# QSentinel 1.5 — Autonomous Guardian

QSentinel 1.5 is designed to run continuously after Windows sign-in and make conservative decisions without requiring the user to press Optimize.

## Autonomous Guardian
- Learns sustained pressure instead of reacting to one spike.
- Four resident states: Learning, Vigilance, Prevention, Active Optimization and Intense Protection.
- Escalates only after repeated pressure samples and backs off after calm.
- Combines CPU/RAM/I-O pressure, commit pressure, network health and hardware symptoms.

## CPU / thermal intelligence
- Reads current and maximum processor clock through Windows hardware instrumentation.
- Correlates high CPU load with substantial clock collapse as a possible throttling symptom.
- Does not claim a temperature when firmware does not expose one.
- Never overrides BIOS/EC fan control, PROCHOT or thermal protection.

## Storage intelligence
- Detects HDD/SSD when Windows Storage exposes media type.
- Reads physical-disk health and temperature when supported.
- Keeps HDD/SSD policy observational and avoids blind defrag/TRIM commands.
- Existing per-process I/O pressure remains part of bottleneck scoring.

## Memory
- Retains physical available memory, commit pressure, kernel paged/nonpaged pool monitoring.
- Memory priority is changed only for safe background candidates under real pressure.
- Does not purge standby memory, empty working sets, or disable the page file.

## Network Intelligence 3.0
- Throughput, gateway latency, packet loss, interface errors/discards and health score.
- Windows TCP auto-tuning stays at supported Normal behavior.
- After sustained degradation, a cooldown-limited DNS resolver cache refresh can run automatically.
- Does not reset the adapter, force public DNS, force MTU, disable IPv6, or apply undocumented registry tweaks.

## Background operation
The installer registers QSentinel in the current user's Windows Run key with --background. Closing the main window hides it to the notification area unless Exit is explicitly chosen. The resident timer continues monitoring and optimizing.

## Safety principle
Automatic does not mean aggressive. QSentinel observes, requires persistence, acts on safe background targets, and restores reversible process settings when pressure clears.
