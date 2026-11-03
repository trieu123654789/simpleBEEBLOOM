# Gameplay, C#, CPU and physics
Source IDs: UNITY-EBOOK, META-SKILL, META-CHECK
Tags: csharp, update, physics, gc, npc, interaction

Profile Update/LateUpdate/FixedUpdate costs before refactoring. Look for per-frame Find/GetComponent, allocations, LINQ in hot paths, string formatting, repeated event subscriptions, Instantiate/Destroy bursts, excessive raycasts and unnecessary Animator updates.

For many flowers, evaluate proximity/spatial partitioning and update scheduling; use pooling for frequently spawned pollen/VFX only if profiling justifies it. Reduce physics contacts and layers where safe. Avoid pooling everything blindly; account for memory retention.

VR comfort and interaction correctness outrank micro-optimizations: preserve head/controller tracking, bee locomotion, grab interactions, collision fidelity and stable visual feedback.
