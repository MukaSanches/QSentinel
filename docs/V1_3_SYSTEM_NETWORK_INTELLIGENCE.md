# QSentinel 1.3 — System & Network Intelligence

Version 1.3 extends the adaptive 1.2 engine with evidence-based network intelligence.

## Network layer
- Measures aggregate receive/send throughput from active Windows adapters.
- Samples default-gateway latency at a deliberately low frequency and smooths it with EWMA.
- Restores Windows TCP receive-window auto-tuning to Microsoft's documented Normal adaptive mode.
- Runs continuously with the resident engine.
- Does not force DNS servers, MTU, congestion providers, NIC advanced properties, Wi-Fi roaming parameters, or unsafe registry "gaming tweaks".
- Does not enable RSS on Wi-Fi; Microsoft documents that wireless adapters do not support RSS/VMQ in this context.

## System layer retained from 1.2
- EWMA system-pressure controller and hysteresis.
- CPU/resource-aware candidate scoring.
- EcoQoS for eligible background work.
- Memory-priority reduction only under real memory pressure.
- Foreground/system/security process protections.
- Reversible restoration of scheduler and QoS state.

## Engineering rationale
Windows networking is adaptive. Maximum throughput and minimum latency are different objectives. QSentinel therefore observes first, preserves supported Windows defaults, and avoids universal tweaks that can trade throughput for latency, break VPN/security software, or disconnect adapters.

The diagnostic model is designed to evolve toward ETW/PktMon evidence for packet loss, retransmission and driver/DPC attribution rather than guessing from raw link speed.
