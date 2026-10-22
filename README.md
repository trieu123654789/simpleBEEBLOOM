# Let It Bee

**Experience the world from a bee’s perspective.**

Let It Bee is a VR game for Meta Quest about pollination, nectar collection and returning to the hive. Players fly between flowers, land on their petals and carry nectar chunks home to fill the honeycomb. The project explores the relationship between bees and their environment through hands-on interaction and a connected simulation.

Built with **Unity 6**, **OpenXR** and **GAMA**, connected through the **SIMPLE toolchain**.

> **Development status:** playable prototype. The flower-to-hive loop is implemented; flight comfort and interaction reliability are being refined on Quest. NPCs, cutscenes and the salinity scenario remain planned work.

## Gameplay

The current prototype follows a single loop:

**Fly → land → transfer pollen → collect nectar → carry it home → fill the hive.**

- **Rhythmic flight.** Sweep either controller ray across the flight bar to gain lift. Coordinated swipes provide extra lift; pauses transition into a bounded gliding descent.
- **Petal landing and walking.** Press the left trigger near a daisy to land, then use the left stick to move across its animated surface. Press again to take off.
- **Flower interaction.** Transfer pollen between flowers and collect available nectar, with visual, sound and haptic feedback for each interaction stage.
- **Honeycomb assembly.** Carry one nectar chunk at a time and guide it into an empty cell. Accepted deposits align and slide into the authored hive geometry.
- **Connected resource state.** GAMA validates interactions, tracks nectar availability and occupied cells, replenishes nectar and records progress in JSON checkpoints.

The current scene contains **two interactive daisies and seven fillable hive cells**. Resource balancing and the wider ecological scenario are still under development; the model has not been calibrated against field data.

## System overview

```text
Meta Quest / Unity  ←→  SIMPLE WebPlatform  ←→  GAMA
  VR interaction         message routing       resource state
  flight and feedback    session management    validation and checkpoints
```

Unity handles tracked head and controller poses, movement, collisions, animation, audio and haptics. SIMPLE carries messages between the game and GAMA. GAMA confirms gameplay transactions and maintains the simulation state. A collection animation alone does not award nectar: the corresponding transaction must be accepted.

Connection settings are stored outside the APK, so the PC address can change without rebuilding the game. Progress uses JSON checkpoints; the project does not require a database service.

## Run the project

### Requirements

- **Unity 6000.6.2f1** with Android Build Support for Quest builds.
- **Meta Quest** with tracked controllers and USB debugging enabled for device setup.
- **GAMA** and **SIMPLE WebPlatform** installed on the host PC.
- PC and headset connected to a network that permits communication between them.

Unity dependencies are recorded in [manifest.json](Packages/manifest.json) and [packages-lock.json](Packages/packages-lock.json). Machine-specific paths are configured separately from the repository.

### Setup and connection

1. Open the project in Unity and load `Assets/Scenes/VU2/SCN_LetItBee.unity`.
2. Follow the [host setup guide](Docs/LetItBee.md#4-chạy-simple-và-cấu-hình-chuyển-máy). For a new machine, copy `machine.example.json` to `machine.local.json` inside `Experiments/LetItBee-HoneyPilot/` and set the installed paths.
3. Install the Quest APK, either separately or with the connection script’s `-InstallApk` option. See [VR build notes](Docs/VR-Embodiment.md) for the latest local build.
4. Start SIMPLE and headless GAMA from the project root:

   ```powershell
   cd Experiments/LetItBee-HoneyPilot
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Start-Simple.ps1 -Headless
   ```

5. Connect one authorized Quest by USB, then upload the connection profile. Run PowerShell as Administrator when updating the firewall:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Connect-Quest.ps1 -UpdateFirewall -EnableStreaming
   ```

6. Open Let It Bee on the headset. The SIMPLE dashboard is available at `http://localhost:8000`; headset streaming uses `/streamPlayerScreen`.

The normal gameplay experiment is **LetItBeeVR**, launched through the SIMPLE integration. A connected dashboard alone does not confirm that GAMA is ready. See the [operating guide](Docs/LetItBee.md) for reconnecting, restarting services and recovering from setup errors.

### Controls

| Input | Action |
| --- | --- |
| Controller rays across the flight bar | Add lift; use both hands for coordinated strokes |
| Left stick | Move in flight or walk while landed |
| Right stick | Turn |
| Left trigger | Land near a flower; press again to take off |
| Right trigger | Hold to collect nectar when the flower is ready |
| Right grip | Hold to carry nectar; release at an empty hive cell to deposit |

## Repository guide

| Location | Responsibility |
| --- | --- |
| `Assets/Scenes/VU2/` | Game scenes, including `SCN_LetItBee` |
| `Assets/Scripts/VU2/Bee/` | Flight, XR input, camera rig, HUD and existing bee systems |
| `Assets/Scripts/VU2/HoneyPilot/` | Flower-to-hive gameplay, feedback and simulation clients |
| `Assets/Editor/VU2/Checks/` | Editor validation and regression checks |
| `Assets/Editor/VU2/Tools/` | Authoring tools, inspectors and diagnostics |
| `Experiments/LetItBee-HoneyPilot/` | GAMA models, launchers, connection scripts and integration checks |
| `Docs/` | Architecture, gameplay plans, setup and tuning documentation |

### Documentation

- [Project guide and development roadmap](Docs/LetItBee.md)
- [Architecture and code organization](Docs/Architecture.md)
- [VR controls, flight tuning and build validation](Docs/VR-Embodiment.md)
- [SIMPLE/GAMA integration package](Experiments/LetItBee-HoneyPilot/README.md)

## Validation and development

The latest flight iteration passed **672 automated Editor checks** and an Android ARM64 build. These checks cover lift and descent, petal walking, collision handling, nectar interactions, hive deposits, audio and connection profiles. They do not replace physical controller, performance or comfort testing on Quest. Build details and remaining device checks are recorded in [VR build notes](Docs/VR-Embodiment.md).

Editor checks are available under the **Let It Bee** menu. GAMA integration checks use isolated sessions to preserve active player progress. Local machine settings, checkpoints, logs and APK builds are excluded from version control.

When contributing:

- Treat the latest saved Unity scene and Inspector values as authored data.
- Keep Unity assets with their `.meta` files and preserve GUIDs.
- Review serialized-field and protocol changes for compatibility with existing scenes and checkpoints.
- Keep ecological assumptions distinct from verified simulation behavior.

## Foundation

Let It Bee builds on the [project-SIMPLE toolchain](https://github.com/project-SIMPLE/simple.toolchain) and the existing CTU VU2 Unity project. SIMPLE infrastructure and third-party assets remain under their respective upstream terms. Game-specific systems and integration responsibilities are described in the architecture guide.
