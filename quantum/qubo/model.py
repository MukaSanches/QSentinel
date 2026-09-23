from __future__ import annotations

from dataclasses import dataclass
import numpy as np


@dataclass(frozen=True)
class Process:
    name: str
    cpu: float
    ram_mb: float
    importance: float
    protected: bool = False


@dataclass
class QuboProblem:
    names: list[str]
    Q: np.ndarray
    gains: np.ndarray
    risks: np.ndarray
    target: float

    def cost(self, bits) -> float:
        x = np.asarray(bits, dtype=float)
        return float(x @ self.Q @ x)

    def recovered(self, bits) -> float:
        x = np.asarray(bits, dtype=float)
        return float(self.gains @ x)


def build_qubo(
    processes: list[Process],
    total_ram_mb: float,
    target: float = 0.30,
    penalty: float = 8.0,
    risk_weight: float = 2.5,
    gain_weight: float = 1.5,
) -> QuboProblem:
    """
    Builds a QUBO for deciding which NON-PROTECTED background processes
    are candidates for intervention.

    x_i = 1 -> candidate for optimization
    x_i = 0 -> leave untouched

    Protected/high-importance processes NEVER enter the QUBO.
    """

    candidates = [
        p for p in processes
        if not p.protected and p.importance < 80
    ]

    names = [p.name for p in candidates]

    if not candidates:
        return QuboProblem(
            names=[],
            Q=np.zeros((0, 0)),
            gains=np.zeros(0),
            risks=np.zeros(0),
            target=target,
        )

    gains = []
    risks = []

    for p in candidates:
        cpu_gain = min(max(p.cpu / 100.0, 0.0), 1.0)
        ram_gain = min(max(p.ram_mb / max(total_ram_mb, 1.0), 0.0), 1.0)

        idle_factor = max(0.0, 1.0 - p.importance / 100.0)

        gain = (0.60 * cpu_gain + 0.40 * ram_gain) * idle_factor
        risk = p.importance / 100.0

        gains.append(gain)
        risks.append(risk)

    g = np.asarray(gains, dtype=float)
    r = np.asarray(risks, dtype=float)
    n = len(g)

    Q = np.zeros((n, n), dtype=float)

    # cost =
    # risk - benefit + penalty * (target - recovered)^2
    for i in range(n):
        Q[i, i] = (
            risk_weight * r[i]
            - gain_weight * g[i]
            - 2.0 * penalty * target * g[i]
            + penalty * g[i] * g[i]
        )

    for i in range(n):
        for j in range(i + 1, n):
            value = penalty * g[i] * g[j]
            Q[i, j] = value
            Q[j, i] = value

    return QuboProblem(
        names=names,
        Q=Q,
        gains=g,
        risks=r,
        target=target,
    )


def exact_solve(problem: QuboProblem):
    n = len(problem.names)

    if n == 0:
        return [], 0.0

    if n > 22:
        raise ValueError("Exact solver intentionally limited to <=22 variables.")

    best_bits = None
    best_cost = float("inf")

    for value in range(1 << n):
        bits = [(value >> i) & 1 for i in range(n)]
        cost = problem.cost(bits)

        if cost < best_cost:
            best_cost = cost
            best_bits = bits

    return best_bits, float(best_cost)


def greedy_solve(problem: QuboProblem):
    n = len(problem.names)

    if n == 0:
        return [], 0.0

    bits = [0] * n
    current = problem.cost(bits)

    while True:
        best_change = None
        best_cost = current

        for i in range(n):
            trial = bits.copy()
            trial[i] ^= 1
            cost = problem.cost(trial)

            if cost < best_cost:
                best_cost = cost
                best_change = i

        if best_change is None:
            break

        bits[best_change] ^= 1
        current = best_cost

    return bits, float(current)
