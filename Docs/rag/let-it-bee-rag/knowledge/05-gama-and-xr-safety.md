# Let It Bee integration guardrails
Tags: letitbee, gama, simple, xr, quest, safety

The game uses Unity 6 + XR Interaction Toolkit + OpenXR, and exchanges simulation information with GAMA/SIMPLE. Before editing, inspect the actual repository to confirm protocol, message schemas, scheduling and ownership of simulation state.

Never assume GAMA is connected in real time. Don't change message formats, salinity/flower state semantics, simulation timing, coordinate mapping or network threading without integration tests. Move parsing/expensive work off hot loops only if Unity API remains on main thread and synchronization is correct.

Do not create duplicate XR Origins, cameras, AudioListeners, input actions or tracking rigs. Protect bee flight comfort, eye height, controller pose, recenter behavior, tutorial and NPC dialogue.
