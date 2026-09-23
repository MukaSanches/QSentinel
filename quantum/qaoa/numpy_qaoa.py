from __future__ import annotations

import math
import numpy as np


def _bit_matrix(n: int) -> np.ndarray:
    states = np.arange(1 << n, dtype=np.uint64)
    return np.array(
        [[(int(s) >> q) & 1 for q in range(n)] for s in states],
        dtype=float,
    )


def _costs(Q: np.ndarray, bits: np.ndarray) -> np.ndarray:
    return np.einsum("bi,ij,bj->b", bits, Q, bits)


def _apply_mixer(state: np.ndarray, beta: float, n: int) -> np.ndarray:
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


def run_qaoa(Q: np.ndarray, gamma_steps: int = 17, beta_steps: int = 13):
    """
    Educational p=1 statevector QAOA simulator.

    It executes the actual QAOA mathematical circuit locally.
    Intended for small QUBOs before hardware submission.
    """

    n = Q.shape[0]

    if n == 0:
        return {
            "bits": [],
            "cost": 0.0,
            "expected_cost": 0.0,
            "probability": 1.0,
            "gamma": 0.0,
            "beta": 0.0,
        }

    if n > 10:
        raise ValueError("Local QAOA simulator limited to <=10 qubits.")

    bits = _bit_matrix(n)
    costs = _costs(Q, bits)

    size = 1 << n
    initial = np.ones(size, dtype=np.complex128) / math.sqrt(size)

    best = None

    gammas = np.linspace(0.0, 2.0 * math.pi, gamma_steps, endpoint=False)
    betas = np.linspace(0.0, math.pi / 2.0, beta_steps)

    for gamma in gammas:
        phased = initial * np.exp(-1j * gamma * costs)

        for beta in betas:
            state = _apply_mixer(phased, float(beta), n)
            probabilities = np.abs(state) ** 2
            expected = float(probabilities @ costs)

            if best is None or expected < best["expected_cost"]:
                index = int(np.argmax(probabilities))

                best = {
                    "bits": bits[index].astype(int).tolist(),
                    "cost": float(costs[index]),
                    "expected_cost": expected,
                    "probability": float(probabilities[index]),
                    "gamma": float(gamma),
                    "beta": float(beta),
                }

    return best
