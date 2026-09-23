"""QSentinel V1 laboratory smoke test.

This does NOT terminate or modify any process.
"""

import platform
import sys
from dataclasses import dataclass


@dataclass(frozen=True)
class ProcessScenario:
    name: str
    cpu: float
    ram_mb: int
    importance: int
    protected: bool


def classify(p: ProcessScenario) -> str:
    if p.protected:
        return "PROTECTED"

    if p.importance >= 80:
        return "KEEP"

    if p.cpu >= 20 and p.importance <= 25:
        return "OPTIMIZATION_CANDIDATE"

    if p.ram_mb >= 1500 and p.importance <= 50:
        return "ECO_CANDIDATE"

    return "NORMAL"


def main():
    scenarios = [
        ProcessScenario("WindowsCritical", 4, 300, 100, True),
        ProcessScenario("ForegroundGame", 55, 5200, 100, False),
        ProcessScenario("BrowserBackground", 12, 2200, 45, False),
        ProcessScenario("Updater", 25, 650, 10, False),
        ProcessScenario("Chat", 4, 850, 55, False),
    ]

    print("=" * 64)
    print("QSENTINEL 1.0 - LABORATORY SMOKE TEST")
    print("=" * 64)
    print(f"Python:   {sys.version.split()[0]}")
    print(f"Platform: {platform.platform()}")
    print()
    print("SIMULATED DECISIONS - NO REAL PROCESS IS MODIFIED")
    print("-" * 64)

    for p in scenarios:
        print(
            f"{p.name:20} "
            f"CPU={p.cpu:5.1f}% "
            f"RAM={p.ram_mb:5}MB "
            f"=> {classify(p)}"
        )

    assert classify(scenarios[0]) == "PROTECTED"
    assert classify(scenarios[1]) == "KEEP"
    assert classify(scenarios[2]) == "ECO_CANDIDATE"
    assert classify(scenarios[3]) == "OPTIMIZATION_CANDIDATE"

    print("-" * 64)
    print("SAFETY CHECKS: PASS")
    print("REAL PROCESSES MODIFIED: 0")
    print("QSentinel laboratory initialized successfully.")


if __name__ == "__main__":
    main()
