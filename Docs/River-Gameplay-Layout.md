# River gameplay layout

Scene: `Assets/Scenes/VU2/SCN_LetItBee_Gameplay.unity`.

The environment authored by Nha is retained. Gameplay lives under `GAMEPLAY - River Route`:

- `Flowers A - Hive Garden`: three daisies near the starting point and hive.
- `Flowers B - Riverbank`: three daisies along the lower bank.
- `Flowers C - Palm Clearing`: three daisies on the opposite side of the palm clearing.
- `Hive - Seven Cells`: existing honeycomb geometry mounted on the visible coconut trunk, facing the clearing.

Move a complete `Daisy NN` root to reposition a flower. Its head, landing anchors, walking surface and nectar feedback move together. Keep flower roots on dry ground and leave room above the petals for the tracked view. Use the complete hive root to move the seven cells together. Do not move individual inserts to reposition the hive.

The pilot retains the A/B references for compatibility and has seven Additional Flowers entries. Each flower has its own ID and plant ID. Unity sends all nine flower positions and all seven hive cells to GAMA. Existing flight, HUD, audio and interaction tuning values were retained. Original gameplay art is kept inactive for reference.

This layout uses a separate checkpoint session, preserving previous sessions. Changing authored resource positions after starting a saved session requires an explicit new session or migration; the server rejects silent geometry changes. Environment transforms and materials are independent of the gameplay groups.

## Validation

- Unity compiled without C# errors; no missing scripts in the scene.
- All nine flowers have a baked walking surface and 13 sampled landing points supported by their mesh.
- Feedback construction and nectar positions checked for all nine flowers.
- Real SIMPLE -> LinkToUnity -> GAMA: 46 checks passed, including world sync, nine-flower pollination, seven deposits, retry idempotency and reconnect.
- Additional flowers 3–9 also tested directly against real GAMA.
- Quest headset comfort, tracked controller reach and performance still need a device test. No updated APK is claimed by these checks.

Re-run the transport test after exporting the current scene configuration:

```powershell
python Experiments/LetItBee-HoneyPilot/validate_river_route.py Library/river-route-configuration.json
```

The test uses disposable sessions and checkpoints; run with gameplay stopped and no other connected player.

## Flight and petal tuning — 4 November 2026

The gameplay scene now uses these values on `BeeController`:

| Inspector field | Value |
| --- | ---: |
| Move Speed | 11 |
| Reverse Speed | 6 |
| Strafe Speed | 6 |
| Move Acceleration | 16 |
| Move Deceleration | 18 |
| Max Flight Altitude | 50 |

The altitude limit is absolute world Y. The seven sockets currently sit at Y=40.95–41.58;
the previous limit of 25 made them unreachable. Socket gizmos now use the mounted
position and warn when it exceeds the flight ceiling. Hive transforms and checkpoint
geometry have not changed. Gravity, ascent speed, steering and XR tracking retain their saved values.

`BeeHoneyPilot > Perch Camera Comfort` controls the felt vertical petal movement:
Camera Motion Influence = 0.35, Max Perch Camera Motion = 0.025 world metres,
Perch Camera Smooth Time = 0.16 seconds. Set influence to zero for a steady view.
Each `BeePerchPoint` retains its existing Petal Bend Angle, Spring Frequency and
Damping Ratio; Perch Sway Amplitude adds 0.15 degrees of resting sway while loaded.
Feedback bends petals along their length, with bloom capped at one degree at the root.
Landing pressure is handled by the contacted petal spring rather than all root joints.
No mesh, skin weights, rig hierarchy or artist animation clip was edited.

The material used by 54 Nipa renderers has its `_Shadow_Color` restored from Nha's
original `SM-Duanuoc/Material.001.mat`. The SIMPLE shader and original texture are retained.

Checks for this tuning pass:

- Unity compilation and Console: no errors; scene diff contains only the six flight
  settings and new perch presentation fields.
- All seven sockets have clear final approach sweeps from 3 m to 0.8 m in front,
  using the player's collision radius and collision mask in Edit Mode.
- Spring simulation at 72, 90 and 120 Hz: about 2.17 degrees peak, 2 degrees settled,
  returning to zero after unloading. Feedback applied 720 times does not accumulate rotation.
- Isolated flower previews checked at idle and loaded/bloom poses. Existing gaps
  between petal tips remain part of the artist asset; they were not welded or remodeled.
- Current hive configuration matches the existing GAMA checkpoint exactly. No reset or migration.
- SIMPLE/GAMA services were not listening during this pass. The earlier transport
  results above were not rerun. Quest flight comfort, walking, collection and delivery
  still require a new APK and an end-to-end device test.


## 2026-11-04 — Story flight and layout integration

Scope: the flower-to-hive prototype only. NPCs, dialogue, cutscenes and narrative progression remain pending artist assets.

### Flight controls
- Hold left trigger in flight to brake and hover; release to resume gliding. Tap (up to 0.25 s) near a valid petal to land; tap again to leave. Long holds do not issue landing requests.
- `BeeEmbodiment.useWorldSpaceFlightSpeeds` separates flight tuning from the 0.15 XR body scale. Forward speed is now 3.3 world units/s (previous effective speed 1.65); reverse/strafe 2; ascent cap 2.4 (previous effective 0.9). Acceleration 5.5, braking 7, glide drag 0.8.
- Descent acceleration 1.8 and terminal descent 1.5; exponential approach avoids hitting a hard fall-speed plateau. Hover braking 0.45 s, bob 0.85 Hz. Head tracking and player scale unchanged.
- `BeeFlightFoliage` targets 162 existing frond bones; only the nearest eight receive flyby input. Max bend 0.8 degrees. Existing mesh, bind poses and SIMPLE shaders are unchanged. Flower flyby pressure reuses each petal spring and yields to perched interaction.

### Environment
- Coconut reduced uniformly by 20 percent around its visual ground base. Hive moved with it from approximately Y41.5 to Y33.6; socket interaction scale unchanged.
- Nine existing flower identities retained. Roots placed on dry land: (145,224), (153,230), (168,229), (119,207), (124,245), (130,268), (164,195), (190,198), (218,200), expressed as Unity X/Z. Root Y is sampled from the artist land collider plus 0.02.
- The original WaterBasicDaytime (2) mesh/material now supplies continuous coverage at Y0.5. Three overlapping old renderers and their colliders are disabled, not deleted. WaterShaderController and SHD_SimpleLit_Water salinity properties retained.
- Survey: 1,726 low-ground points inside X45–310 / Z20–320, sampled every five units, all covered after adjustment; 753 had been uncovered. This is a sampled coverage check, not a claim about every pixel outside that region.

### Story GAMA contract
- `allow_layout_update` permits relocation only for matching ordered flower/plant/species and cell IDs within the same session and interaction radii. Before layout migration, GAMA writes a revision-specific backup. Nectar, pollen, chunks, occupied cells and transaction receipts are preserved. Identity changes remain rejected.
- `autoRefillNectar` defaults off for this finite Story route. Existing flower resources are not reset. Refill, when explicitly enabled, now iterates all flowers.
- World snapshots include all hive cells and a UTC observation timestamp. Coordinate conversion remains Unity (X,Y,Z) to GAMA (X,Z,Y).
- GAMA exports a separate `<checkpoint>.observations.json` mirror at most once per second. The read-only HoneyPilot workspace view reads this beside the selected progress file. It shows the last observation timestamp and marks unchanged data stale after roughly five seconds of viewer refreshes. It does not become gameplay authority.
- Moving the bee no longer drives map framing. Inspectable display agents expose `world_position` separately from their projected display location.
- Unity scene changes still require a new APK; no automatic Editor-to-installed-APK layout deployment is implied. Restart/reload the GAMA experiment to load changed model code.

### Checks and limits
- Isolated GAMA test passed: seven-cell deposit, position migration, progress retention, idempotent reconnect and rejection of changed identities. Run `python Experiments/LetItBee-HoneyPilot/validate_story_layout.py` against test server port 6873.
- Two isolated GAMA experiments verified observation mirror updates from one received flower position to another.
- Editor logic checks at 72/90/120 Hz: tap vs hold passed; descent after three seconds was -1.4590 at all three rates. Nine flower overhead approaches were clear in collider probes.
- Quest comfort, complete hand interaction route, on-device performance and final APK are still to be verified. No narrative or Study Mode generation was added.
- Backups and survey images are in Temp/StoryFlightBackup and Temp/StoryFlightReview. Do not commit those transient files.


### Story observation sync — 2026-11-04

- The player observation uses the tracked gameplay camera position and forward vector, mapped from Unity X/Y/Z to GAMA X/Z/Y. `perched_flower_id` is sent only after landing settles, and cleared on takeoff.
- Observations run through the same awaited RPC loop as gameplay, including while an interaction is pending. Failed acknowledgements trigger reconnect; transaction IDs and confirmed progress are preserved. Direct GAMA clients read the acknowledgement from a snapshot when `ask` returns no body.
- GAMA timestamps receipt using its own machine clock. The observation sidecar is exported at most every 200 ms and the read-only viewer polls every two cycles. A three-second receipt age marks the bee stale/gray and suppresses the perch ring. Older files without receipt time are treated as unverified/stale.
- Flower fill: pink = not pollinated, green = pollinated. Cyan ring = currently perched. Small orange/white nectar marker = available/collected. Pollination and collection remain independent server-confirmed transactions.
- To activate after updating: rebuild/install the Unity APK, restart the SIMPLE/GAMA experiment when no player is using it, then reopen the read-only HoneyPilot viewer with the same progress file. Do not delete the checkpoint. GAMA may cache compiled models by path; restart its server if an old model persists.
- Regression script: `python Experiments/LetItBee-HoneyPilot/validate_observations.py` (isolated GAMA server on 6873). It uses unique model snapshots and test checkpoints, checks live file rereading, clock rollback, stale detection, nine flowers/seven slots, separate pollen/nectar state and reconnect preservation.
