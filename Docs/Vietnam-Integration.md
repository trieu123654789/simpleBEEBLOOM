# Vietnam gameplay integration

## Current scene

Open `Assets/Scenes/VU2/SCN_LetItBee_Vietnam.unity`.
The saved gameplay scene supplies XR Origin, BeeRoot, two daisies, petal landing/walking, nectar interactions, seven hive cells, HUD, menu and menu music. The Vietnam scene supplies the environment assets. Both source scenes remain unchanged.

## Layout correction — 1 November 2026

The first integration layout was rejected during visual review. It is superseded.
We first restored all 4,100 environment transforms to match the source scene. This confirmed that land2 was already separated from the village/river in the source. The user then explicitly authorized rearranging the environment into a connected village.

The revised layout has:
- A long western bank using land2 for the flower/hive gameplay.
- A southern village bank and northern rice bank, using additional land2 instances.
- Four water surfaces extended beneath the banks to close shoreline gaps.
- Two houses grouped on the southern bank, props grounded around the village, and a smaller boat on the river.
- Water palms arranged on the banks; duplicated lotus and duplicate house set disabled; extremely displaced decorative mesh fragments hidden.

These are scene-instance changes, not edits to source models/prefabs. This is a reviewable environment layout, not finished level art.

## Preserved gameplay

The gameplay roots were translated together by (-10, 2, 150). Only flower-root heights were subsequently fitted to the sampled ground. Rig scale, movement/flight/landing parameters, HUD layout, menu references and music are retained. XR Origin and BeeRoot remain separate roots.

Imported cameras, lights and Volume remain disabled. The existing VR camera and gameplay lighting are used.

BeeEnvironmentSurface classifies the ground and four water surfaces. Collision floor protection and skim feedback use the classification, with legacy name rules retained for the original scene. Water remains a flight safety surface, not a flower landing target.

## SIMPLE/GAMA and progress

Current integration session: `1e43444fb9e34ff9bca74e3582e61cfe`.
Original gameplay session: `46c5af5b854947f49d64c9c6dacef12f`.

The new scene uses independent progress. Original checkpoints are not reset or migrated. Never copy the old session ID into a scene with different flower/socket coordinates. Transport, external connection profiles, socket IDs and Unity/GAMA XZY mapping are unchanged.

Start the host from the project root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Experiments/LetItBee-HoneyPilot/Start-Simple.ps1 -Headless
```

## Verification and limits

The initial layout passed 893 editor checks, world sync 18/18, full hive 18/18 and menu 22/22. Live protocol checks passed 35/35 through SIMPLE -> LinkToUnity -> GAMA, including seven deposits, duplicate protection and reconnect restoration. The restored-source layout also passed 35/35 using its updated coordinates. These results do not substitute for rechecking the final rearranged layout or headset testing.

Run `Let It Bee > Checks > Vietnam Integration` in Edit Mode. The live test script `Experiments/LetItBee-HoneyPilot/validate_vietnam_scene.py` takes an exported configuration JSON and uses a disposable session, never the player's progress.

**The existing LetItBee-Vietnam-Integration.apk is obsolete:** it was built before the layout correction. It succeeded with 0 errors and 510 warnings, but must be rebuilt before testing this revised layout. Default Build Settings still point to the original game scene.

Quest interactions, comfort, performance and a complete shoreline/collision walkthrough remain unverified. GAMA observes gameplay entities; terrain and river meshes are not automatically replicated into its map.
## Final rearranged layout verification

After reconnecting Unity MCP, the saved final layout passed 897 editor checks and 35/35 live SIMPLE -> LinkToUnity -> GAMA checks using the newly exported coordinates. Ground raycasts succeeded below the player spawn, both flower roots and hive. Source-scene, menu, music and existing-checkpoint SHA256 hashes remain unchanged; Player Settings has no diff. Screenshots are in Library/VietnamIntegration/village-layout-current.png and gameplay-eye-review.png.

These are editor/static and protocol checks. Headset input, frame rate and a full traversal are still unverified. The previous APK remains obsolete and was not rebuilt after this layout correction.
