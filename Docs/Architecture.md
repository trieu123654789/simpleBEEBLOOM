# Architecture and code organization

Let It Bee uses Unity for the VR experience and GAMA for the authoritative resource state. SIMPLE connects the headset to the simulation. This document describes the current prototype, not a proposed framework rewrite.

## Runtime responsibilities

```text
Quest controller input
        |
        v
Unity flight and flower interaction ----> local visuals, audio and haptics
        |
        v
HoneySimpleClient -> SIMPLE -> LinkToUnity -> GAMA
        ^                                     |
        |                                     v
        +---------- confirmed state ------ JSON checkpoint
```

Unity owns tracked head/controller poses, flight, aiming, animation and feedback. `BeeHoneyPilot` coordinates the interaction sequence. `BeeHoneyGrip` tracks grip intent; `BeeHiveLayout` maps the authored hive inserts to runtime slots. The pilot still coordinates both gameplay and presentation: the folders clarify ownership without claiming these are independent assemblies.

GAMA validates visits, nectar collection, pickup and deposits. It owns nectar availability, pollen state, occupied cells and transaction receipts. Repeated transaction IDs must not award resources twice. Checkpoints are JSON files, not a separate database service.

`IHoneyClient` is the transport boundary. `HoneySimpleClient` is the normal Quest route; `HoneyGamaClient` remains a direct diagnostic route. `BeeWorldSync` sends world observations separately from gameplay transactions. The connection profile can change the endpoint outside the APK.

## Folder map

| Folder | Contents |
| --- | --- |
| `Assets/Scripts/VU2/Bee/Flight/` | Movement, altitude hold, collision, camera rig, flight rhythm and surface skim |
| `Assets/Scripts/VU2/Bee/Input/` | XR/desktop input and swipe/shake adapters |
| `Assets/Scripts/VU2/Bee/Presentation/` | HUD, audio and game status screens |
| `Assets/Scripts/VU2/Bee/` | Existing flower, hive, quest and ecosystem systems; some predate the GAMA pilot |
| `Assets/Scripts/VU2/HoneyPilot/Gameplay/` | Interaction coordinator, grip state, flower geometry and hive layout |
| `Assets/Scripts/VU2/HoneyPilot/Presentation/` | Honey, flower and socket feedback |
| `Assets/Scripts/VU2/HoneyPilot/Integration/` | Clients, RPC retries, endpoint configuration and world synchronization |
| `Assets/Scripts/VU2/HoneyPilot/Compatibility/` | Legacy BeeBloom API aliases needed by existing callers |
| `Assets/Editor/VU2/Checks/` | Regression checks, including preview-scene checks |
| `Assets/Editor/VU2/Tools/` | Inspectors, setup tools and diagnostics |
| `Experiments/LetItBee-HoneyPilot/GAMA/` | Simulation source and GAMA workspace project |
| `Experiments/LetItBee-HoneyPilot/` | Stable launcher entry points, package settings and integration tests |
| `Experiments/LetItBee-HoneyPilot/Runtime/` | Ignored machine-local files, checkpoints and logs |

Launchers stay at the experiment root because their relative paths are part of the existing deployment workflow. Models, scenes, prefabs and `Resources` assets keep their current paths. Unrelated SIMPLE/VU2 modules remain in place.

## Naming

- C# classes, methods and properties use `PascalCase`; fields, local variables and parameters use `camelCase`.
- Prefer a domain term over an abbreviation: `visitedFlowerIndex`, `collectionElapsedSeconds`, `CompleteInteractionRequest`.
- Name asynchronous methods with `Async` where their signatures return `Task`.
- Use English for identifiers and Inspector descriptions. Player-facing text may remain Vietnamese.
- Distinguish pollen transfer, nectar collection, a carried chunk and a deposited hive cell. A completed visual effect does not imply an accepted transaction.
- Preserve serialized Inspector field names unless a migration is necessary and tested. Existing concise fields are not renamed just for style.
- GAMA actions and JSON keys retain their existing `snake_case` contract. Do not force C# naming conventions onto the wire format.

## Changes that need compatibility checks

Moving a script must preserve its `.meta` GUID. Renaming a serialized field also requires checking custom inspectors, reflection-based checks, scene/prefab values and any UnityEvent references. Changing a class or namespace requires a separate migration review.

Changing slot IDs, session IDs, model actions or checkpoint keys affects saved progress. New Unity functionality must validate that the connected GAMA model supports it before enabling the interaction. Never repair a mismatch by silently resetting the player's checkpoint.

## Verification

For a code organization change:

1. Compare saved scene, prefab, asset, project setting and checkpoint hashes before and after.
2. Verify moved script GUIDs and all remaining path references.
3. Compile in Unity, then run the relevant Editor checks in isolated preview scenes.
4. Exercise protocol changes against separate GAMA sessions. A folder-only change does not require rewriting or resetting the server.
5. Record any remaining Quest testing separately from Editor and server results.

Use [the project guide](LetItBee.md) for operational commands, tuning and the gameplay backlog.

### Organization pass — 22 October 2026

Moved 60 C# scripts through Unity AssetDatabase, retaining their metadata. Renamed internal collection state and coordinator methods, including the reflection-based callers in the regression checks. Serialized fields, public component types, namespaces and transport keys were retained.

Unity compilation passed. Ten Editor check suites passed (251 checks), covering the full hive, interaction sequence, grip, feedback, perching, flight, audio, surface flight, connection profiles and world synchronization. These are Editor results; this pass does not constitute a new Quest APK or a live headset/server test.
