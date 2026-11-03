# Performance foundations
Source IDs: META-PERF, META-METRICS, UNITY-EBOOK
Tags: unity6, quest3, quest3s, profiling, cpu, gpu

Frame budget: 72 Hz = 13.89 ms; 90 Hz = 11.11 ms; 120 Hz = 8.33 ms. These are total cadence budgets, not a promise that CPU/GPU timing counters can simply be added together. Use actual headset refresh rate and report dropped/stale frames.

Measure on standalone headset, not just editor or Quest Link. Capture scene, build hash, device, refresh rate, thermal state, quality, resolution, runtime and time window. Collect FPS, CPU/GPU frame times, memory, thermal throttling and dropped/stale frames where available.

Bottleneck classification: CPU main thread, render thread, GPU, memory pressure/GC, asset streaming, XR compositor/runtime, thermal throttling. Investigate evidence before choosing a fix. Repeat tests in comparable conditions; change one variable at a time.

Never claim measured gains based solely on code inspection.
