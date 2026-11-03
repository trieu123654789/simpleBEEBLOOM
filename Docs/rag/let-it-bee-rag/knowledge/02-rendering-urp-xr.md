# URP / OpenXR / stereo rendering
Source IDs: META-OPENXR, META-BEST, META-CHECK, META-SAMPLE
Tags: unity6, urp, xr, openxr, multiview, gpu

Audit active URP asset, renderer features, MSAA, HDR, shadows, opaque/depth textures, post-processing, shader variants and stereo mode. Verify whether Multiview and any Quest-specific Vulkan options are supported by installed packages and runtime.

Assess draw calls, SetPass calls, shader cost, transparent overdraw, vegetation/flower alpha, water shaders, particles, texture bandwidth and eye buffer resolution. SRP Batcher and GPU instancing solve different bottlenecks; don't assume they combine freely or improve every material. Test each candidate with Frame Debugger and headset GPU timings.

Fixed Foveated Rendering and resolution scaling are experiments with visual-quality tradeoffs, not mandatory switches. Verify compatibility and text clarity before enabling. Don't use a generic fixed triangle or draw-call budget as a universal pass/fail threshold.
