# Main menu

The scene `SCN_LetItBee` contains `LetItBee_MainMenu`, an instance of
`Assets/Prefabs/LetItBee/UI/PF_MainMenu.prefab`, plus one XR EventSystem.
The existing camera, player rig and game tuning are retained.

## Flow

- Story waits for `BeeHoneyPilot.Live` and a confirmed state, then requires both
  triggers and grips to be released before unblocking gameplay.
- A connection timeout offers Retry and Back. The pilot keeps its existing
  reconnection loop; the menu never resets session IDs or checkpoints.
- Study is an explicit placeholder. The artwork describes the intended mode,
  not completed chapters or a measured play duration.
- Settings adjusts master volume and shows controller instructions. Volume is
  stored in device PlayerPrefs under `LetItBee.MasterVolume`.
- This is a startup menu, not an in-game pause system. GAMA continues simulating
  while the menu is visible; Unity timeScale is not changed.

## Inspector

Select `LetItBee_MainMenu` and edit `Bee Main Menu`:

- Viewing Distance: physical viewing distance, default 1.5 m.
- Panel Width: apparent physical width, default 1.5 m.
- Connection Timeout: seconds before offering Retry, default 25.
- Left Ray / Right Ray: the existing controller XR Ray Interactors.

Placement accounts for the camera's miniature rig scale. It is set once at
startup and again on the first valid headset tracking pose; it does not follow
head rotation every frame. The panel itself does not bob. Decorative bees do.

The prefab contains the view and page controls. Controller references belong to
its scene instance. Runtime UI trigger readers are separate for each hand and
are restored when gameplay begins. `BeeMenuInput` prevents movement and honey
transactions during menu interaction. Two-pointer hover remains active until
both rays leave; repeated clicks are debounced.

## Validation

`Let It Bee > Checks > Main Menu` runs isolated prefab checks for pointer hover,
click debounce, settings, unavailable Study, connection failure/retry/back,
server-ready release gating and connection loss during entry. It does not
connect to GAMA or modify checkpoints.

Quest acceptance: start with the host offline, exercise Settings and Back with
both hands, then start SIMPLE/GAMA and Retry Story. Confirm tracking stays live,
no movement or honey transaction leaks through the menu, and both triggers must
be released before playing. Compare progress before and after entry.
## Menu music

The menu loops LetItBee_MenuTheme (2:47, stereo). Select LetItBee_MainMenu > Audio Source > Volume to tune its level (default 0.35). Settings master volume also affects it. Music stops when gameplay begins or the menu is disabled. The clip uses Vorbis compression and streaming for Quest. Original supplied M4A was converted to WAV for import.

