# Nha land import

Source: land.blend supplied by Nha on 2 November 2026. The original file was read with Blender background mode and was not modified.

- Models/NhaLand.fbx: exported ground mesh only; camera/light were excluded.
- Textures/T_NhaLand_BaseColor.png: recovered from the packed image, because the active Blender image node referenced an unavailable external PNG.
- Materials/M_NhaLand_SIMPLE.mat: scene-specific material using the supplied SHD_SimpleLit_2Sided shader.
- Materials/M_River_*: separate SIMPLE materials for environment and base hive geometry; original authored materials are retained.

The packed image has the same pixels and 1024 x 1024 resolution as the previous ground texture. This import does not add texture detail. Unity imports normals from the exported FBX rather than recalculating them. The FBX was exported from the supplied Blender file; exact per-vertex equivalence with the source has not been verified. The imported land matches the existing footprint and sampled flower/spawn heights within 0.00001 world units.

Scene object: LAND - Nha Latest. Previous land2 is retained inactive. The new MeshCollider is explicitly classified as Ground. Gameplay positions, pilot tuning, session ID, menu and music were retained.

River water uses the supplied SHD_SimpleLit_Water graph and SIMPLE caustic texture. Salinity uniforms remain available; a live salinity model connection is not claimed by the visual setup.

A separate RiverVR pipeline adds MSAA 4x without changing the provided shader graphs. Quest frame time must be measured on a device before treating the quality preset as final.

Scene backup before replacement: Library/LandImport/before-new-land.unity (local only).
