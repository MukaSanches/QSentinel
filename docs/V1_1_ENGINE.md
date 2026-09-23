# QSentinel 1.1

QSentinel 1.1 is a resident Windows resource orchestrator.

## Resident behavior

- Registers itself in the current user's Windows startup.
- Starts hidden in the notification area after login.
- Closing the main window keeps the optimization engine running.
- The user or a Windows administrator can still stop it.
- Uninstall removes startup registration.

## Adaptive engine

Every cycle QSentinel measures:

- total CPU load
- physical memory pressure
- process I/O throughput
- foreground application
- per-process CPU, memory and I/O

When system pressure rises, QSentinel selects safe background candidates and uses reversible controls:

- Below Normal process priority
- Windows execution-speed power throttling / Eco behavior
- reduced memory priority under high memory pressure

When pressure falls, QSentinel restores the previous process priority.

## Termination policy

QSentinel does not blindly terminate unknown software.

Automatic termination only applies to process names explicitly added by the user to the block list.

## Quantum research

The quantum research code remains isolated from Windows execution authority.
Quantum optimization results are advisory and cannot bypass the Safety Engine.
