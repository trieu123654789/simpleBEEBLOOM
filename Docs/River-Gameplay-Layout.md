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
