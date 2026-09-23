# QSentinel Safety Model

## Non-negotiable principles

QSentinel must fail safely.

Automatic optimization must never blindly terminate unknown processes.

Protected categories include:

- critical Windows components
- security software
- drivers and hardware services
- QSentinel itself
- explicitly protected user applications

Interventions should prefer:

1. Observe
2. Recommend
3. Adjust priority when safe
4. Apply reversible controls
5. Suspend only when validated
6. Terminate only when explicitly permitted and classified safe

The V1 must begin in observation mode.
