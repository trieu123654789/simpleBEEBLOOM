# Honey Shader Graph trial

Status: parallel visual review, pending approval. The game continues to use the original
`Assets/Resources/HoneyNectar.shader` and `HoneyFeedback.shader`.

## Open the comparison

In Unity, keep `SCN_LetItBee` open in Edit Mode, then choose:

**Let It Bee → Preview → Honey Shader Graph Comparison**

The left side uses the original HLSL shaders; the right side uses the trial graphs.
Both sides use private copies of the current flower/hive art, identical cameras,
and the existing `BeeHoneyFeedback` presentation code. The 12-second sequence shows
gathering, formation, pickup, alignment and insertion. Audio and haptics are muted.
No connection to GAMA or gameplay transaction is required.

- **Pause / Play:** inspect a phase or resume the loop.
- **Replay:** recreate both private previews and start from the flower.
- **Open Nectar Nodes:** open the nectar material graph.
- **Open Feedback Nodes:** open the motes, filament and hive-rim graph.
- Close the window to remove its private preview scenes.

The window reads the live scene references, including unsaved Inspector edits.
It never saves the game scene or assigns a trial shader to an authored material.

## Graphs and controls

Both graphs are in `Assets/Art/Shaders/LetItBee/ShaderGraphTrials/`.
They use native Shader Graph nodes, grouped in numbered stages; the shading formula
is not hidden inside a Custom Function node.
The nectar graph has 260 native nodes; the feedback graph has 34. Constant scalars
are stored at node inputs to keep the diagrams smaller. Separate trial `.mat` files
are available in the same folder, with no assignments to the game scene.

| Graph | Editable groups | Preserved property references |
| --- | --- | --- |
| `HoneyNectarGraph_Trial.shadergraph` | Texture/tint, curved normal, lighting, liquid swirl, reflections, six bubble rings, final warmth | `_BaseMap`, `_BaseColor`, `_Warmth`, `_FlowTime` |
| `HoneyFeedbackGraph_Trial.shadergraph` | UV/ribbon shape, halo/core falloff, vertex colour and transparency | `_Ribbon` |

The nectar graph is opaque URP Unlit with front-face rendering. The feedback graph
is transparent URP Unlit, alpha blended and double sided. Numerically authored colour
constants use vector nodes to avoid an extra colour-space conversion. Time and warmth
remain script-driven properties so the original animation can drive either version.

Formation, stretch, pickup, carried movement and insertion remain C# animation.
Opening a Shader Graph exposes the material/shading nodes, not those movement rules.

## Review before replacement

Review the same phases on both sides: amber colour, broad reflections, bubble rings,
thin filaments, soft motes, the socket outline and sealing wave. Source shader,
materials, scenes, gameplay scripts and connection/checkpoint settings remain the
active implementation until a separate replacement is approved.

Editor visual comparison does not verify Quest performance or stereo rendering.
Before adopting the graphs, test a separate APK on Quest for both eyes, transparency,
frame time and memory use. No production migration or APK rebuild is part of this trial.

## Editor verification — 26 October 2026

Both graphs imported and rendered without shader compilation errors in Unity
6000.6.2f1 / URP 17.6. Eight fixed-time captures cover gathering, formation, pickup,
alignment, insertion and seating. Average absolute channel differences over the
complete 640 × 480 images ranged from 0.12 to 0.61 on a 0–255 scale. This is a
whole-image measurement, not a guarantee that every nectar pixel is identical.
The original and graph captures were also inspected side by side.

All 1,637 live game-scene components match the pre-trial snapshot; its existing
unsaved state is preserved. Original resource shaders and runtime code were not
changed. A pre-existing `AwaiiStudio/ASFolliage` duplicate-keyword diagnostic is
separate from the two trial graphs. Quest validation and replacement approval remain
pending.

Temporary captures and measurements are under `Library/HoneyShaderGraph/` and are
not required to open or edit the graph assets. **Replay** recreates the comparison
using graph edits that have been saved in the Shader Graph editor.
