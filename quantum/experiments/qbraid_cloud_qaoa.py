from __future__ import annotations

import json
import math
import time
from pathlib import Path

import numpy as np
from scipy.optimize import minimize

from qbraid import QbraidProvider

from quantum.qubo.model import (
    Process,
    build_qubo,
    exact_solve,
)


DEVICE_ID = "qbraid:qbraid:sim:qir-sv"
SHOTS = 2000
TOTAL_RAM_MB = 16384


def bit_matrix(n: int) -> np.ndarray:
    states = np.arange(1 << n, dtype=np.uint64)

    return np.array(
        [
            [(int(state) >> q) & 1 for q in range(n)]
            for state in states
        ],
        dtype=float,
    )


def apply_mixer(
    state: np.ndarray,
    beta: float,
    n: int,
) -> np.ndarray:

    c = math.cos(beta)
    s = -1j * math.sin(beta)

    out = state.copy()

    for q in range(n):
        step = 1 << q

        for base in range(0, len(out), step * 2):
            for offset in range(step):
                i = base + offset
                j = i + step

                a = out[i]
                b = out[j]

                out[i] = c * a + s * b
                out[j] = s * a + c * b

    return out


def qaoa_state(
    costs: np.ndarray,
    n: int,
    gammas: np.ndarray,
    betas: np.ndarray,
) -> np.ndarray:

    state = np.ones(
        1 << n,
        dtype=np.complex128,
    ) / math.sqrt(1 << n)

    for gamma, beta in zip(gammas, betas):

        state = state * np.exp(
            -1j * gamma * costs
        )

        state = apply_mixer(
            state,
            float(beta),
            n,
        )

    return state


def expectation(
    params: np.ndarray,
    costs: np.ndarray,
    n: int,
    depth: int,
) -> float:

    gammas = params[:depth]
    betas = params[depth:]

    state = qaoa_state(
        costs,
        n,
        gammas,
        betas,
    )

    probs = np.abs(state) ** 2

    return float(
        probs @ costs
    )


def optimize_qaoa(
    costs: np.ndarray,
    n: int,
    depth: int,
    starts: int = 12,
):

    rng = np.random.default_rng(
        20260923 + depth
    )

    best = None

    bounds = (
        [(0.0, 2.0 * math.pi)] * depth
        +
        [(0.0, math.pi / 2.0)] * depth
    )

    for _ in range(starts):

        initial = np.concatenate(
            [
                rng.uniform(
                    0,
                    2.0 * math.pi,
                    depth,
                ),
                rng.uniform(
                    0,
                    math.pi / 2.0,
                    depth,
                ),
            ]
        )

        result = minimize(
            expectation,
            initial,
            args=(costs, n, depth),
            method="Powell",
            bounds=bounds,
            options={
                "maxiter": 350,
                "xtol": 1e-6,
                "ftol": 1e-8,
            },
        )

        if (
            best is None
            or result.fun < best.fun
        ):
            best = result

    params = best.x

    gammas = params[:depth]
    betas = params[depth:]

    state = qaoa_state(
        costs,
        n,
        gammas,
        betas,
    )

    probs = np.abs(state) ** 2

    return {
        "depth": depth,
        "expectation": float(
            best.fun
        ),
        "gammas": [
            float(x)
            for x in gammas
        ],
        "betas": [
            float(x)
            for x in betas
        ],
        "probabilities": probs,
    }


def qubo_to_ising(Q: np.ndarray):

    n = len(Q)

    h = np.zeros(n)

    J = {}

    constant = 0.0

    for i in range(n):

        constant += (
            Q[i, i] / 2.0
        )

        h[i] -= (
            Q[i, i] / 2.0
        )

    for i in range(n):

        for j in range(i + 1, n):

            # Q é simétrica.
            # x^T Q x conta Q_ij duas vezes.
            q = Q[i, j]

            constant += q / 2.0

            h[i] -= q / 2.0
            h[j] -= q / 2.0

            J[(i, j)] = (
                q / 2.0
            )

    return constant, h, J


def validate_ising(
    Q: np.ndarray,
    constant: float,
    h: np.ndarray,
    J: dict,
):

    n = len(Q)

    bits = bit_matrix(n)

    qubo = np.einsum(
        "bi,ij,bj->b",
        bits,
        Q,
        bits,
    )

    z = 1.0 - 2.0 * bits

    ising = (
        constant
        +
        z @ h
    )

    for (i, j), value in J.items():

        ising += (
            value
            * z[:, i]
            * z[:, j]
        )

    if not np.allclose(
        qubo,
        ising,
        atol=1e-9,
    ):
        raise RuntimeError(
            "QUBO -> Ising validation failed."
        )


def qasm_for_qaoa(
    n: int,
    h: np.ndarray,
    J: dict,
    gammas: list[float],
    betas: list[float],
):

    lines = [
        "OPENQASM 3.0;",
        'include "stdgates.inc";',
        f"qubit[{n}] q;",
        f"bit[{n}] c;",
        "",
    ]

    # |+>^n
    for q in range(n):
        lines.append(
            f"h q[{q}];"
        )

    for layer, (
        gamma,
        beta,
    ) in enumerate(
        zip(gammas, betas)
    ):

        lines.append("")
        lines.append(
            f"// QAOA layer {layer + 1}"
        )

        # Campos Z
        for i, value in enumerate(h):

            angle = (
                2.0
                * gamma
                * value
            )

            if abs(angle) > 1e-14:
                lines.append(
                    f"rz({angle:.17g}) q[{i}];"
                )

        # Acoplamentos ZZ
        for (
            i,
            j,
        ), value in J.items():

            angle = (
                2.0
                * gamma
                * value
            )

            if abs(angle) <= 1e-14:
                continue

            lines.append(
                f"cx q[{i}], q[{j}];"
            )

            lines.append(
                f"rz({angle:.17g}) q[{j}];"
            )

            lines.append(
                f"cx q[{i}], q[{j}];"
            )

        # Mixer
        for i in range(n):

            angle = (
                2.0
                * beta
            )

            lines.append(
                f"rx({angle:.17g}) q[{i}];"
            )

    lines.append("")
    lines.append(
        "c = measure q;"
    )

    return "\n".join(lines)


def extract_counts(result):

    data = result.data

    if hasattr(
        data,
        "get_counts",
    ):
        return data.get_counts()

    if hasattr(
        data,
        "measurement_counts",
    ):
        return data.measurement_counts

    if isinstance(
        data,
        dict,
    ):

        for key in (
            "measurementCounts",
            "measurement_counts",
            "counts",
        ):
            if key in data:
                return data[key]

    raise RuntimeError(
        "Could not extract measurement counts."
    )


def clean_bitstring(
    key,
    n,
):

    text = "".join(
        c
        for c in str(key)
        if c in "01"
    )

    return text.zfill(n)[-n:]


def job_identifier(job):

    for name in (
        "id",
        "job_id",
        "job_qrn",
    ):

        value = getattr(
            job,
            name,
            None,
        )

        if callable(value):
            try:
                value = value()
            except Exception:
                value = None

        if value:
            return str(value)

    return "unknown"


def main():

    print("=" * 76)
    print(
        "QSENTINEL — REAL QBRAID CLOUD QAOA EXPERIMENT"
    )
    print("=" * 76)

    # ----------------------------------------------------
    # QSentinel optimization scenario
    # ----------------------------------------------------

    processes = [
        Process(
            "WindowsCritical",
            5,
            500,
            100,
            True,
        ),
        Process(
            "ForegroundGame",
            55,
            5200,
            100,
        ),
        Process(
            "Updater",
            24,
            600,
            10,
        ),
        Process(
            "BrowserBG",
            12,
            2200,
            35,
        ),
        Process(
            "ChatBG",
            5,
            900,
            45,
        ),
        Process(
            "CloudSync",
            9,
            1300,
            30,
        ),
        Process(
            "Telemetry",
            7,
            500,
            15,
        ),
        Process(
            "Launcher",
            4,
            650,
            20,
        ),
        Process(
            "Helper",
            6,
            800,
            25,
        ),
    ]

    problem = build_qubo(
        processes,
        TOTAL_RAM_MB,
    )

    n = len(
        problem.names
    )

    print(
        f"Optimization variables: {n}"
    )

    print(
        f"Protected processes in QUBO: "
        f"{'WindowsCritical' in problem.names}"
    )

    if "WindowsCritical" in problem.names:
        raise RuntimeError(
            "Safety violation."
        )

    bits_table = bit_matrix(n)

    costs = np.einsum(
        "bi,ij,bj->b",
        bits_table,
        problem.Q,
        bits_table,
    )

    exact_bits, exact_cost = (
        exact_solve(problem)
    )

    print(
        f"Exact optimum: {exact_cost:.10f}"
    )

    # ----------------------------------------------------
    # Compare p=1 and p=2
    # ----------------------------------------------------

    print("")
    print(
        "Optimizing QAOA p=1..."
    )

    p1 = optimize_qaoa(
        costs,
        n,
        depth=1,
        starts=8,
    )

    print(
        f"P=1 expectation: "
        f"{p1['expectation']:.10f}"
    )

    print("")
    print(
        "Optimizing QAOA p=2..."
    )

    p2 = optimize_qaoa(
        costs,
        n,
        depth=2,
        starts=12,
    )

    print(
        f"P=2 expectation: "
        f"{p2['expectation']:.10f}"
    )

    improvement = (
        p1["expectation"]
        -
        p2["expectation"]
    )

    print(
        f"P=2 improvement: "
        f"{improvement:.10f}"
    )

    # ----------------------------------------------------
    # Validate QUBO -> Ising mathematically
    # ----------------------------------------------------

    constant, h, J = (
        qubo_to_ising(
            problem.Q
        )
    )

    validate_ising(
        problem.Q,
        constant,
        h,
        J,
    )

    print("")
    print(
        "QUBO -> Ising: PASS"
    )

    # ----------------------------------------------------
    # Connect to qBraid
    # ----------------------------------------------------

    print("")
    print(
        "Connecting to qBraid Runtime..."
    )

    provider = (
        QbraidProvider()
    )

    device = provider.get_device(
        DEVICE_ID
    )

    print(
        f"Device: {DEVICE_ID}"
    )

    # ----------------------------------------------------
    # Endianness calibration
    # ----------------------------------------------------

    calibration_qasm = """
OPENQASM 3.0;
include "stdgates.inc";

qubit[3] q;
bit[3] c;

x q[0];

c = measure q;
"""

    print("")
    print(
        "Calibrating measurement bit order..."
    )

    calibration_job = device.run(
        calibration_qasm,
        shots=128,
    )

    calibration_result = (
        calibration_job.result()
    )

    calibration_counts = (
        extract_counts(
            calibration_result
        )
    )

    dominant = max(
        calibration_counts,
        key=calibration_counts.get,
    )

    dominant_bits = clean_bitstring(
        dominant,
        3,
    )

    if dominant_bits.endswith("1"):

        reverse_output = True
        orientation = (
            "MSB-left / q0-right"
        )

    elif dominant_bits.startswith("1"):

        reverse_output = False
        orientation = (
            "q0-left"
        )

    else:

        raise RuntimeError(
            "Unable to determine bit order."
        )

    print(
        f"Measurement order: {orientation}"
    )

    # ----------------------------------------------------
    # Build cloud QAOA circuit
    # ----------------------------------------------------

    qasm = qasm_for_qaoa(
        n=n,
        h=h,
        J=J,
        gammas=p2["gammas"],
        betas=p2["betas"],
    )

    Path(
        "quantum/experiments/"
        "qsentinel_qaoa_p2.qasm"
    ).write_text(
        qasm,
        encoding="utf-8",
    )

    print("")
    print(
        f"Submitting p=2 QAOA "
        f"({n} qubits, {SHOTS} shots)..."
    )

    started = time.perf_counter()

    job = device.run(
        qasm,
        shots=SHOTS,
    )

    result = job.result()

    elapsed = (
        time.perf_counter()
        -
        started
    )

    counts = extract_counts(
        result
    )

    # ----------------------------------------------------
    # Analyze cloud result
    # ----------------------------------------------------

    total = sum(
        int(v)
        for v in counts.values()
    )

    weighted_cost = 0.0

    best_observed_cost = (
        float("inf")
    )

    best_observed_bits = None

    optimum_hits = 0

    decoded = {}

    for raw_key, count in (
        counts.items()
    ):

        text = clean_bitstring(
            raw_key,
            n,
        )

        if reverse_output:
            text = text[::-1]

        bits = [
            int(x)
            for x in text
        ]

        cost = problem.cost(
            bits
        )

        weighted_cost += (
            cost
            * int(count)
        )

        if (
            cost
            <
            best_observed_cost
        ):

            best_observed_cost = (
                cost
            )

            best_observed_bits = (
                bits
            )

        if abs(
            cost
            -
            exact_cost
        ) < 1e-8:

            optimum_hits += (
                int(count)
            )

        decoded[
            "".join(
                str(x)
                for x in bits
            )
        ] = int(count)

    cloud_mean_cost = (
        weighted_cost
        /
        max(total, 1)
    )

    optimum_rate = (
        optimum_hits
        /
        max(total, 1)
    )

    selected_names = [
        name
        for name, bit
        in zip(
            problem.names,
            best_observed_bits,
        )
        if bit
    ]

    print("")
    print("=" * 76)
    print(
        "QBRAID CLOUD RESULTS"
    )
    print("=" * 76)

    print(
        f"Job:               "
        f"{job_identifier(job)}"
    )

    print(
        f"Execution time:    "
        f"{elapsed:.2f}s"
    )

    print(
        f"Shots received:    "
        f"{total}"
    )

    print(
        f"Exact optimum:     "
        f"{exact_cost:.10f}"
    )

    print(
        f"Local p=1 mean:    "
        f"{p1['expectation']:.10f}"
    )

    print(
        f"Local p=2 mean:    "
        f"{p2['expectation']:.10f}"
    )

    print(
        f"Cloud p=2 mean:    "
        f"{cloud_mean_cost:.10f}"
    )

    print(
        f"Best cloud sample: "
        f"{best_observed_cost:.10f}"
    )

    print(
        f"Optimum hit rate:  "
        f"{100.0 * optimum_rate:.2f}%"
    )

    print(
        f"Best selection:    "
        f"{selected_names}"
    )

    if (
        best_observed_cost
        <
        exact_cost - 1e-8
    ):
        raise RuntimeError(
            "Cloud result illegally beat "
            "the exact mathematical optimum."
        )

    # ----------------------------------------------------
    # Save report
    # ----------------------------------------------------

    report = {
        "project": "QSentinel",
        "version": "1.0.0-dev",
        "device": DEVICE_ID,
        "qubits": n,
        "shots": total,
        "qaoa_depth": 2,
        "measurement_orientation":
            orientation,
        "exact": {
            "cost":
                float(exact_cost),
            "bits":
                exact_bits,
        },
        "p1": {
            "expectation":
                p1["expectation"],
            "gammas":
                p1["gammas"],
            "betas":
                p1["betas"],
        },
        "p2": {
            "expectation":
                p2["expectation"],
            "gammas":
                p2["gammas"],
            "betas":
                p2["betas"],
            "improvement_over_p1":
                improvement,
        },
        "cloud": {
            "job_id":
                job_identifier(job),
            "execution_seconds":
                elapsed,
            "mean_cost":
                cloud_mean_cost,
            "best_cost":
                best_observed_cost,
            "best_bits":
                best_observed_bits,
            "selected_names":
                selected_names,
            "optimum_hits":
                optimum_hits,
            "optimum_hit_rate":
                optimum_rate,
            "counts":
                {
                    str(k): int(v)
                    for k, v
                    in counts.items()
                },
        },
        "safety": {
            "protected_process_in_qubo":
                False,
            "real_windows_processes_modified":
                0,
        },
    }

    Path(
        "benchmarks/"
        "qbraid_cloud_qaoa_p2.json"
    ).write_text(
        json.dumps(
            report,
            indent=2,
        ),
        encoding="utf-8",
    )

    print("")
    print(
        "QUBO SAFETY:       PASS"
    )

    print(
        "ISING VALIDATION:  PASS"
    )

    print(
        "QBRAID CLOUD:      PASS"
    )

    print(
        "WINDOWS MODIFIED:  0 processes"
    )

    print(
        "REPORT: benchmarks/"
        "qbraid_cloud_qaoa_p2.json"
    )

    print("=" * 76)


if __name__ == "__main__":
    main()
