# Honey presentation authoring

The honey visuals now use reusable prefabs. Gameplay state, SIMPLE/GAMA messages, controller bindings and saved pilot tuning remain in their existing systems. Original HLSL shaders remain active; Shader Graph trials are still a separate comparison.

## Preview without Play

1. Keep the **Scene** tab open.
2. Select the object with **Bee Honey Pilot** in the current scene.
3. At the top of its Inspector, use **Form Nectar**, **Lift Nectar** or **Deposit Nectar**.
4. Click **Frame Preview** to locate the active effect. It only moves the Scene View when you ask.
5. Orbit with **Alt + left mouse**, pan with the middle mouse button, and zoom with the scroll wheel.
6. Use **Pause / Resume** and **Reset Preview**. A sequence stops after 12 seconds; click again to replay.

The scene pilot preview samples the runtime flower idle pose on a temporary art copy, offset from the original flower. Click Frame Preview to locate it. The original flower is never sampled or repositioned.

No headset, Play Mode or GAMA session is required. Preview is visual and silent: sound playback and haptics remain runtime features. The preview simulates confirmations locally without calling gameplay transactions.

To examine an individual prefab, double-click it in Project, select its root, then use the same buttons on **Bee Nectar View** or **Bee Honey Effect Rig**. In isolated Prefab Mode, flower and hive anchor positions are simulated; no complete environment is copied in. Deposit uses a temporary copy of the authored hive when previewing from the scene pilot. The selected cell retains its saved geometry for fitting, while its static honey renderer is hidden only on the copy. This prevents overlapping surfaces after insertion; the original hive is unchanged.

Temporary preview objects are removed on Reset, entering Play, script reload, scene save or loss of the selected scene. No extra scene asset is created. Scene View orientation is free throughout playback.

## Assets

| Asset | Purpose |
| --- | --- |
| `Assets/Prefabs/LetItBee/HoneyPilot/PRB_NectarChunk.prefab` | Existing nectar art, with Bee Nectar View and a Grip Anchor child |
| `Assets/Prefabs/LetItBee/HoneyPilot/Effects/PRB_FlowerNectarEffects.prefab` | Flower rings, nectar streams, charge arc and droplets |
| `Assets/Prefabs/LetItBee/HoneyPilot/Effects/PRB_NectarCarryEffects.prefab` | Pickup thread, grab cue, motes and spatial audio sources |
| `Assets/Prefabs/LetItBee/HoneyPilot/Effects/PRB_HiveSocketEffects.prefab` | Socket rim, confirmation wave and wax sealing lines |
| `Assets/Resources/BeePresentation/PresentationLibrary.asset` | Shared references and presentation settings |

The existing nectar prefab's mesh, material and transform were preserved. An explicit mesh or material already assigned on Bee Honey Pilot takes priority on its runtime clone. Changing this library does not overwrite those scene assignments. Grip Anchor is authoring metadata at the current zero-offset contact; it does not introduce a new grip mechanic.

## Tuning

Select **PresentationLibrary** in Project:

- **Nectar Color**, **Pollen Color**, **Highlight Color**: effect colours.
- **Alignment Duration**, **Insertion Duration**: seconds for aligning and sliding nectar into a cell.
- **Line Width Scale**, **Particle Size Scale**: visual sizing multipliers.
- **Audio Profile**: existing interaction audio settings; an explicit pilot audio profile takes priority.

Positions, line shapes and nectar scale animate through the existing feedback code. Editing a Line Renderer preview pose alone does not replace that animation. Prefabs supply reusable objects and references; this is not a conversion into an Animator timeline or Shader Graph.

After changing timing or colours, reset and restart the preview. Do not rename rig children or remove references without updating their bindings.

## Maintenance and validation

**Let It Bee > Authoring > Create Honey Effect Prefabs** creates missing presentation assets. It does not reset existing colours, durations, original mesh/material or scene tuning. Normal editing does not require rerunning it.

**Let It Bee > Check Honey Authoring** checks preview isolation, resource ownership and scene/camera preservation. **Let It Bee > Check Honey Presentation** runs the existing animation and socket-fit checks.

Editor checks do not replace an APK test on Quest. This change does not claim a new live SIMPLE/GAMA or headset validation.
