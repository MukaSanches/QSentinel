# QSentinel 1.4 — Adaptive System Intelligence

Version 1.4 evolves the resident engine into observe, classify, act and restore.

## Added
- Physical available-memory and commit-pressure telemetry through Windows performance information.
- Kernel paged/nonpaged pool visibility.
- EWMA trends for CPU, memory and I/O.
- Stable dominant-bottleneck classifier with confidence/hysteresis.
- Bottleneck-aware process scoring.
- Conservative memory intervention tied to physical and commit pressure.
- Network Intelligence 2.0: throughput, gateway latency, packet-loss sampling, IPv4 interface errors/discards, adaptive health score and congestion detection.
- TCP receive-window auto-tuning remains in the Windows-supported Normal mode.
- Network recovery is cooldown-limited and conservative.

## Safety
Foreground applications, Windows/security components and QSentinel remain protected. EcoQoS and memory-priority changes remain reversible. QSentinel does not empty working sets or standby lists, disable the page file, disable SysMain, change fan firmware, force DNS providers/MTU, disable IPv6, reset adapters, or blindly modify NIC advanced properties.

## Evidence model
Network and system tuning depends on workload and hardware. Version 1.4 measures conditions and selects conservative actions instead of applying one universal tweak to every PC.
