# Let It Bee

A VR bee game for Meta Quest, built with Unity and connected to GAMA through the SIMPLE toolchain.

Players fly between flowers, transfer pollen, collect nectar chunks and carry them back to a hive. The current prototype focuses on making this loop work reliably in VR. Its environment and connection infrastructure build on the original SIMPLE/VU2 project.

## Current gameplay

- Flight controlled by two controller rays, with position hold, collision handling, surface feedback and wing audio.
- Two flowers with perching, pollen transfer and nectar collection.
- One carried nectar chunk at a time and seven fillable hive cells.
- GAMA validates collection and deposits, replenishes nectar and saves progress to JSON checkpoints.
- Connection settings outside the APK allow the PC address to change without rebuilding the game.

The seven-cell prototype is the current implementation. The earlier five-chunk objective is a design proposal, not the current completion rule. NPCs, cutscenes and the salinity scenario are planned work. The ecological model is not yet calibrated against field data.

## Requirements

- Unity **6000.6.2f1** with Android build support for Quest builds.
- A Meta Quest with tracked controllers for device testing.
- GAMA and the official [SIMPLE WebPlatform](https://github.com/project-SIMPLE/simple.toolchain), configured on the host PC.
- PC and Quest on a network that permits communication between them. USB debugging is required for the setup script to update the headset.

Unity package versions are recorded in `Packages/manifest.json` and `Packages/packages-lock.json`.

## Getting started

1. Open the project in Unity and load `Assets/Scenes/VU2/SCN_LetItBee.unity`.
2. Follow the [PC setup guide](Docs/LetItBee.md#4-chạy-simple-và-cấu-hình-chuyển-máy). On a new machine, copy `machine.example.json` to `machine.local.json` in the experiment folder and set the local paths.
3. From the project root, start SIMPLE and headless GAMA:

   ```powershell
   cd Experiments/LetItBee-HoneyPilot
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Start-Simple.ps1 -Headless
   ```

4. With one authorized Quest connected by USB, update its endpoint:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Connect-Quest.ps1 -UpdateFirewall -EnableStreaming
   ```

   Firewall changes require an elevated PowerShell window. Install the desired APK separately or use the script's `-InstallApk` option.
5. Open Let It Bee on the headset. The SIMPLE dashboard runs at `http://localhost:8000`; its screen-streaming page is `/streamPlayerScreen`.

`Start-Simple.ps1` leaves an already running service in place. After changing a GAMA model, restart the experiment so it loads the new code. See the [troubleshooting guide](Docs/LetItBee.md#lỗi-thường-gặp-và-chế-độ-cũ) in the main document's **Lỗi thường gặp và chế độ cũ** section.

## Controls

| Input | Action |
| --- | --- |
| Controller rays across the flight bar | Generate lift |
| Left trigger | Hold position; perch when close to a flower |
| Right trigger | Collect nectar after flower contact is confirmed |
| Right grip | Pick up and carry a chunk; release near an empty cell to deposit |

Pollen transfer and nectar collection are separate steps. A collection effect is not a confirmed reward until GAMA accepts the transaction.

## Project structure

| Location | Responsibility |
| --- | --- |
| `Assets/Scripts/VU2/Bee/` | Flight, input, presentation and existing bee systems |
| `Assets/Scripts/VU2/HoneyPilot/` | Flower–nectar–hive loop and GAMA integration |
| `Assets/Editor/VU2/Checks/` | Editor regression checks |
| `Assets/Editor/VU2/Tools/` | Scene setup tools, inspectors and diagnostics |
| `Experiments/LetItBee-HoneyPilot/` | GAMA models, launch scripts and integration checks |
| `Docs/` | Design notes, operating guide and development status |

Read [Architecture and code organization](Docs/Architecture.md) for folder responsibilities and naming rules. The existing VU2 and SIMPLE code is retained alongside the game-specific code.

## Validation and known limits

Editor checks are available under the **Let It Bee** menu. GAMA integration checks use separate test sessions; they must not reset a player's active experiment. See the [validation notes](Docs/LetItBee.md#9-kiểm-chứng-và-giới-hạn) for setup and results.

The latest collection safeguards still need a fresh APK test on Quest. A connected SIMPLE dashboard alone does not confirm that the gameplay experiment is ready. Local machine configuration, runtime checkpoints, logs and APK builds are kept out of Git.

## Development notes

- Treat the latest saved Unity scene and Inspector values as the source of truth.
- Move Unity assets with their `.meta` files; preserve GUIDs and serialized field names.
- Keep protocol keys and session IDs stable so existing checkpoints remain readable.
- Do not run scene setup tools over an authored scene without reviewing their changes.

The [main project guide](Docs/LetItBee.md) contains configuration details, gameplay plans and the backlog. Project infrastructure is based on [project-SIMPLE](https://github.com/project-SIMPLE/simple.toolchain), with development in the CTU VR project context.
