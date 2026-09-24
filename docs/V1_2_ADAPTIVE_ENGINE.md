# QSentinel 1.2 — Adaptive Performance Engine

QSentinel 1.2 replaces fixed, spike-driven optimization with a conservative adaptive control loop.

## Engine

- Exponentially weighted moving pressure (EWMA) filters short CPU/RAM/I/O spikes.
- Hysteresis requires sustained calm before restoring managed processes, reducing priority/QoS churn.
- Resource-aware scoring changes candidate weights according to the active bottleneck.
- CPU priority is lowered only when CPU pressure is material.
- Memory priority is lowered only under high memory pressure.
- Background candidates use Windows ProcessPowerThrottling / EcoQoS.
- Restore returns QoS control to Windows heuristics with ControlMask=0.
- Foreground, Windows-critical, security, shell and QSentinel processes remain protected.
- User block-list behavior remains separate from adaptive optimization.

## Safety principles

QSentinel does not blindly disable Windows services, SysMain, the page file, security services, scheduled tasks, or system components. It does not empty standby memory merely to increase the displayed amount of free RAM.

Optimization is reversible and limited to eligible background processes.

## Control loop

Capture -> calculate pressure -> smooth -> identify bottleneck -> rank safe background candidates -> apply minimal intervention -> observe -> restore after sustained calm.

## Validation target

Windows x64, .NET 8 self-contained single-file build. GitHub Actions builds both the portable executable and the Inno Setup installer.
