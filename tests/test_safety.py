from simulator.smoke_test import ProcessScenario, classify


def test_protected_process_always_protected():
    process = ProcessScenario(
        "critical",
        cpu=100,
        ram_mb=10000,
        importance=0,
        protected=True,
    )

    assert classify(process) == "PROTECTED"


def test_important_foreground_process_is_kept():
    process = ProcessScenario(
        "foreground",
        cpu=90,
        ram_mb=8000,
        importance=100,
        protected=False,
    )

    assert classify(process) == "KEEP"
