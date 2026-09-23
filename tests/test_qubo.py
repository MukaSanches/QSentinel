import numpy as np

from quantum.qubo.model import Process, build_qubo, exact_solve, greedy_solve
from quantum.qaoa.numpy_qaoa import run_qaoa


def test_protected_process_never_enters_qubo():
    processes = [
        Process("WindowsCritical", 100, 8000, 0, True),
        Process("SafeCandidate", 20, 1000, 10, False),
    ]

    problem = build_qubo(processes, 16000)

    assert "WindowsCritical" not in problem.names
    assert "SafeCandidate" in problem.names


def test_high_importance_process_excluded():
    processes = [
        Process("Foreground", 100, 8000, 100, False),
        Process("Background", 10, 500, 10, False),
    ]

    problem = build_qubo(processes, 16000)

    assert "Foreground" not in problem.names


def test_qubo_is_symmetric():
    processes = [
        Process("A", 10, 1000, 10),
        Process("B", 15, 1200, 20),
        Process("C", 5, 600, 30),
    ]

    problem = build_qubo(processes, 16000)

    assert np.allclose(problem.Q, problem.Q.T)


def test_greedy_never_beats_exact_illegally():
    processes = [
        Process("A", 20, 1000, 10),
        Process("B", 15, 1200, 20),
        Process("C", 10, 600, 30),
    ]

    problem = build_qubo(processes, 16000)

    _, exact = exact_solve(problem)
    _, greedy = greedy_solve(problem)

    assert greedy >= exact - 1e-12


def test_qaoa_probability_valid():
    Q = np.array([
        [-1.0, 0.2],
        [0.2, -0.8],
    ])

    result = run_qaoa(Q, gamma_steps=5, beta_steps=5)

    assert len(result["bits"]) == 2
    assert 0.0 <= result["probability"] <= 1.0
