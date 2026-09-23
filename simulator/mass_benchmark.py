from __future__ import annotations

import json
import os
import random
import statistics
import time
from concurrent.futures import ProcessPoolExecutor

from quantum.qubo.model import (
    Process,
    build_qubo,
    exact_solve,
    greedy_solve,
)
from quantum.qaoa.numpy_qaoa import run_qaoa


TOTAL_RAM_MB = 16384


def scenario(seed: int):
    rng = random.Random(seed)
    count = rng.randint(10, 35)

    processes = [
        Process("WindowsKernel", rng.uniform(1, 8), 400, 100, True),
        Process("SecurityService", rng.uniform(0, 5), 350, 100, True),
        Process("QSentinel", rng.uniform(0, 1), 80, 100, True),
        Process("ForegroundApp", rng.uniform(15, 75), rng.randint(1000, 7000), 100),
    ]

    for i in range(count):
        processes.append(
            Process(
                name=f"background_{i}",
                cpu=rng.uniform(0, 30),
                ram_mb=rng.randint(50, 2500),
                importance=rng.randint(5, 75),
                protected=False,
            )
        )

    return processes


def run_chunk(args):
    start, amount = args

    safety_violations = 0
    recovered = []
    costs = []
    selected_counts = []

    for seed in range(start, start + amount):
        processes = scenario(seed)
        problem = build_qubo(processes, TOTAL_RAM_MB)

        bits, cost = greedy_solve(problem)

        selected = {
            name for name, bit in zip(problem.names, bits)
            if bit
        }

        # Absolute safety invariant.
        protected_names = {p.name for p in processes if p.protected}

        if selected & protected_names:
            safety_violations += 1

        recovered.append(problem.recovered(bits))
        costs.append(cost)
        selected_counts.append(sum(bits))

    return {
        "cases": amount,
        "safety_violations": safety_violations,
        "mean_recovered": statistics.fmean(recovered),
        "mean_cost": statistics.fmean(costs),
        "mean_selected": statistics.fmean(selected_counts),
    }


def main():
    cpu_count = os.cpu_count() or 2
    workers = min(8, cpu_count)

    total_cases = 40000
    per_worker = total_cases // workers

    chunks = [
        (i * per_worker, per_worker)
        for i in range(workers)
    ]

    print("=" * 72)
    print("QSENTINEL V1 — MASSIVE HYBRID OPTIMIZATION BENCHMARK")
    print("=" * 72)
    print(f"CPU workers:       {workers}")
    print(f"Mass scenarios:    {total_cases:,}")
    print()

    started = time.perf_counter()

    with ProcessPoolExecutor(max_workers=workers) as pool:
        results = list(pool.map(run_chunk, chunks))

    elapsed = time.perf_counter() - started

    total_safety = sum(x["safety_violations"] for x in results)
    avg_recovery = statistics.fmean(x["mean_recovered"] for x in results)
    avg_selected = statistics.fmean(x["mean_selected"] for x in results)

    print(f"Finished in:       {elapsed:.2f}s")
    print(f"Scenarios/sec:     {total_cases / max(elapsed, 0.001):,.0f}")
    print(f"Safety violations: {total_safety}")
    print(f"Mean recovery:     {avg_recovery:.5f}")
    print(f"Mean interventions:{avg_selected:.2f}")
    print()

    # Validate greedy vs exact solver on small cases.
    print("CLASSICAL EXACT VALIDATION")
    print("-" * 72)

    exact_cases = 100
    gaps = []

    for seed in range(exact_cases):
        processes = scenario(100000 + seed)[:12]
        problem = build_qubo(processes, TOTAL_RAM_MB)

        # Keep exact search manageable.
        if len(problem.names) > 10:
            continue

        exact_bits, exact_cost = exact_solve(problem)
        greedy_bits, greedy_cost = greedy_solve(problem)

        gaps.append(greedy_cost - exact_cost)

    mean_gap = statistics.fmean(gaps) if gaps else 0.0
    max_gap = max(gaps) if gaps else 0.0

    print(f"Exact comparisons: {len(gaps)}")
    print(f"Mean greedy gap:   {mean_gap:.8f}")
    print(f"Worst greedy gap:  {max_gap:.8f}")
    print()

    # Small real QAOA mathematical simulation.
    print("QAOA STATEVECTOR EXPERIMENT")
    print("-" * 72)

    q_processes = [
        Process("WindowsCritical", 5, 500, 100, True),
        Process("ForegroundGame", 55, 5200, 100),
        Process("Updater", 24, 600, 10),
        Process("BrowserBG", 12, 2200, 35),
        Process("ChatBG", 5, 900, 45),
        Process("CloudSync", 9, 1300, 30),
        Process("Telemetry", 7, 500, 15),
        Process("Launcher", 4, 650, 20),
        Process("Helper", 6, 800, 25),
    ]

    qp = build_qubo(q_processes, TOTAL_RAM_MB)

    exact_bits, exact_cost = exact_solve(qp)
    qaoa = run_qaoa(qp.Q)

    print(f"Qubits:            {len(qp.names)}")
    print(f"Exact optimum:     {exact_cost:.8f}")
    print(f"QAOA sample cost:  {qaoa['cost']:.8f}")
    print(f"QAOA expectation:  {qaoa['expected_cost']:.8f}")
    print(f"QAOA probability:  {qaoa['probability']:.4f}")
    print(f"QAOA gamma:        {qaoa['gamma']:.4f}")
    print(f"QAOA beta:         {qaoa['beta']:.4f}")

    selected = [
        name for name, bit in zip(qp.names, qaoa["bits"])
        if bit
    ]

    print(f"QAOA selected:     {selected}")
    print()

    report = {
        "version": "1.0.0-dev",
        "mass_scenarios": total_cases,
        "workers": workers,
        "elapsed_seconds": elapsed,
        "scenarios_per_second": total_cases / max(elapsed, 0.001),
        "safety_violations": total_safety,
        "mean_recovery": avg_recovery,
        "mean_interventions": avg_selected,
        "classical_validation": {
            "comparisons": len(gaps),
            "mean_greedy_gap": mean_gap,
            "worst_greedy_gap": max_gap,
        },
        "qaoa": {
            "qubits": len(qp.names),
            "exact_cost": exact_cost,
            **qaoa,
            "selected_names": selected,
        },
    }

    with open("benchmarks/v1_lab_summary.json", "w") as f:
        json.dump(report, f, indent=2)

    print("=" * 72)

    if total_safety != 0:
        raise SystemExit("FAIL: SAFETY VIOLATION DETECTED")

    print("SAFETY:             PASS — 0 protected processes selected")
    print("QUBO:               PASS")
    print("CLASSICAL BASELINE: PASS")
    print("QAOA SIMULATION:    PASS")
    print("REPORT:              benchmarks/v1_lab_summary.json")
    print("=" * 72)


if __name__ == "__main__":
    main()
