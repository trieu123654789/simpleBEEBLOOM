# Assets and mangrove environment
Source IDs: META-CHECK, META-PERF
Tags: vegetation, flowers, assets, texture, memory

Inventory flowers, mangroves, water, hive, bee, NPCs, pollen and ambient effects. Record mesh triangles, material slots, shader, texture dimensions, compression, mipmaps, LOD, animation and instancing eligibility.

Experiment with LODs, culling, texture atlases, ASTC, mesh simplification and baked lighting only after identifying their likely impact. Dense alpha-blended foliage may cost more than its triangle count suggests. Occlusion culling may have limited benefit in open outdoor environments and has its own overhead.

Measure peak and steady-state memory and loading spikes on device. Avoid degrading close-up flower readability, interaction affordances or educational visual cues.
