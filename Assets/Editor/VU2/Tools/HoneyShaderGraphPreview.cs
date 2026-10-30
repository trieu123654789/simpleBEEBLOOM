using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LetItBee.HoneyPilot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only A/B review. Both sides use private copies and the existing presentation code.
public sealed class HoneyShaderGraphPreview : EditorWindow
{
    public const string Folder = "Assets/Art/Shaders/LetItBee/ShaderGraphTrials/";
    const int Width = 640, Height = 480;
    PreviewSide original, graph;
    double previousTime;
    float elapsed;
    bool running = true;
    string problem;

    [MenuItem("Let It Bee/Preview/Honey Shader Graph Comparison")]
    public static void Open()
    {
        var window = GetWindow<HoneyShaderGraphPreview>("Honey Shader A/B");
        window.minSize = new Vector2(850, 440);
        window.Show();
        if (window.original == null) window.Initialize();
    }

    void OnEnable()
    {
        previousTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += Advance;
        AssemblyReloadEvents.beforeAssemblyReload += Release;
    }

    void OnDisable()
    {
        EditorApplication.update -= Advance;
        AssemblyReloadEvents.beforeAssemblyReload -= Release;
        Release();
    }

    void Release()
    {
        original?.Dispose(); graph?.Dispose();
        original = graph = null;
    }

    void Initialize()
    {
        Release(); problem = null; elapsed = 0f;
        if (Application.isPlaying) { problem = "Stop Play Mode before opening the comparison."; return; }
        var source = FindObjectsByType<BeeHoneyPilot>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(p => p.gameObject.scene.name == "SCN_LetItBee");
        if (source == null) { problem = "Keep SCN_LetItBee open for the flower and hive art references."; return; }
        try {
            var nectar = AssetDatabase.LoadAssetAtPath<Shader>(Folder + "HoneyNectarGraph_Trial.shadergraph");
            var feedback = AssetDatabase.LoadAssetAtPath<Shader>(Folder + "HoneyFeedbackGraph_Trial.shadergraph");
            if (nectar == null || feedback == null || ShaderUtil.ShaderHasError(nectar) || ShaderUtil.ShaderHasError(feedback))
                throw new InvalidOperationException("The trial graphs must import without shader errors.");
            original = new PreviewSide(source, null, null);
            graph = new PreviewSide(source, nectar, feedback);
            original.Step(0.001f, 0f); graph.Step(0.001f, 0f);
        } catch (Exception ex) { Release(); problem = ex.Message; }
        previousTime = EditorApplication.timeSinceStartup;
    }

    void Advance()
    {
        double now = EditorApplication.timeSinceStartup;
        if (now - previousTime < 1.0 / 30.0) return;
        float dt = Mathf.Min((float)(now - previousTime), 0.05f); previousTime = now;
        if (!running || original == null || graph == null) return;
        if (Application.isPlaying) { Release(); problem = "Preview stopped because Play Mode started."; Repaint(); return; }
        elapsed += dt;
        original.Step(dt, elapsed); graph.Step(dt, elapsed);
        Repaint();
    }

    void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
            if (GUILayout.Button(running ? "Pause" : "Play", EditorStyles.toolbarButton, GUILayout.Width(55))) running = !running;
            if (GUILayout.Button("Replay", EditorStyles.toolbarButton, GUILayout.Width(55))) Initialize();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open Nectar Nodes", EditorStyles.toolbarButton)) OpenGraph("HoneyNectarGraph_Trial.shadergraph");
            if (GUILayout.Button("Open Feedback Nodes", EditorStyles.toolbarButton)) OpenGraph("HoneyFeedbackGraph_Trial.shadergraph");
        }
        EditorGUILayout.HelpBox("REVIEW ONLY — original game shaders and scene settings are unchanged. Both sides use the same C# animation; no GAMA transactions are sent.", MessageType.Info);
        if (problem != null) {
            EditorGUILayout.HelpBox(problem, MessageType.Warning);
            if (GUILayout.Button("Load comparison")) Initialize();
            return;
        }
        if (original == null) { if (GUILayout.Button("Load comparison")) Initialize(); return; }
        float half = (position.width - 24f) / 2f;
        float imageHeight = Mathf.Min(half * Height / Width, position.height - 135f);
        using (new EditorGUILayout.HorizontalScope()) {
            GUILayout.Space(6);
            DrawSide(original, "ORIGINAL · HLSL", half, imageHeight);
            GUILayout.Space(6);
            DrawSide(graph, "TRIAL · SHADER GRAPH", half, imageHeight);
        }
        GUILayout.Label(original.PhaseName + "  ·  12-second loop", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Label("Open a graph to inspect the numbered node groups. Close this window to remove both preview scenes.", EditorStyles.wordWrappedMiniLabel);
    }

    static void DrawSide(PreviewSide side, string title, float width, float height)
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(width))) {
            GUILayout.Label(title, EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            if (Event.current.type == EventType.Repaint) { side.Render(); GUI.DrawTexture(rect, side.Image, ScaleMode.ScaleToFit, false); }
        }
    }

    static void OpenGraph(string filename) => AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<Shader>(Folder + filename));

    // Capture fixed phases for visual comparison using the same cameras, geometry and animation time.
    public static string CaptureValidation()
    {
        var window = GetWindow<HoneyShaderGraphPreview>();
        window.Initialize();
        if (window.original == null) throw new InvalidOperationException(window.problem);
        bool wasRunning = window.running; window.running = false;
        Directory.CreateDirectory("Library/HoneyShaderGraph/Comparison");
        var metrics = new System.Collections.Generic.List<object>();
        float time = 0f;
        try {
            foreach (float target in new[] { 1.5f, 3f, 4.55f, 6f, 7.5f, 8.2f, 8.65f, 9.3f }) {
                while (time < target - .0001f) {
                    float dt = Mathf.Min(1f / 60f, target - time); time += dt;
                    window.original.Step(dt, time); window.graph.Step(dt, time);
                }
                window.original.Render(); window.graph.Render();
                var a = window.original.Pixels(); var b = window.graph.Pixels();
                try {
                    string label = target.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    File.WriteAllBytes("Library/HoneyShaderGraph/Comparison/" + label + "-original.png", a.EncodeToPNG());
                    File.WriteAllBytes("Library/HoneyShaderGraph/Comparison/" + label + "-graph.png", b.EncodeToPNG());
                    var pa = a.GetPixels32(); var pb = b.GetPixels32();
                    double error = 0; int changed = 0;
                    for (int i = 0; i < pa.Length; i++) {
                        int d = Math.Abs(pa[i].r - pb[i].r) + Math.Abs(pa[i].g - pb[i].g) + Math.Abs(pa[i].b - pb[i].b);
                        error += d; if (d > 15) changed++;
                    }
                    metrics.Add(new { seconds = target, meanChannelDifference = error / (pa.Length * 3),
                        changedPixelsPercent = 100.0 * changed / pa.Length, phase = window.original.PhaseName });
                } finally { DestroyImmediate(a); DestroyImmediate(b); }
            }
            File.WriteAllText("Library/HoneyShaderGraph/comparison.json", Newtonsoft.Json.JsonConvert.SerializeObject(metrics, Newtonsoft.Json.Formatting.Indented));
            return Newtonsoft.Json.JsonConvert.SerializeObject(metrics);
        } finally { window.Initialize(); window.running = wasRunning; }
    }

    sealed class PreviewSide : IDisposable
    {
        readonly Scene scene;
        readonly GameObject owner, flower, hive;
        readonly Camera camera;
        readonly Transform head, second, slot, hand, chunk;
        readonly BeeHoneyFeedback feedback;
        int previousPhase = -1, cycle;
        string state = "none";
        public RenderTexture Image { get; }
        public string PhaseName { get; private set; }

        GameObject Obj(string name)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(go, scene); return go;
        }

        public PreviewSide(BeeHoneyPilot source, Shader nectarShader, Shader feedbackShader)
        {
            scene = EditorSceneManager.NewPreviewScene();
            owner = Obj("Honey shader review");
            camera = Obj("Preview camera").AddComponent<Camera>(); camera.enabled = false;
            camera.scene = scene; camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.nearClipPlane = .01f; camera.farClipPlane = 10f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .055f, .065f);
            Image = new RenderTexture(Width, Height, 24) { hideFlags = HideFlags.HideAndDontSave };
            Image.Create(); camera.targetTexture = Image;
            var light = Obj("Preview directional light").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.1f; light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            flower = Instantiate(source.flowerAAnimator.gameObject);
            SceneManager.MoveGameObjectToScene(flower, scene); flower.hideFlags = HideFlags.HideAndDontSave;
            flower.transform.localScale = source.flowerAAnimator.transform.lossyScale;
            foreach (var anim in flower.GetComponentsInChildren<Animator>(true)) anim.enabled = false;
            foreach (var script in flower.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            source.pilotFlowerIdle.animationClips[0].SampleAnimation(flower, .45f);
            head = flower.GetComponentsInChildren<Transform>(true).First(x => x.name == source.flowerAHead.name);
            flower.transform.position -= head.position;
            second = Obj("Unused second flower").transform; second.position = Vector3.left * 3f;
            slot = Obj("Review socket").transform; slot.position = new Vector3(.85f, .35f, -.05f);
            slot.rotation = Quaternion.LookRotation(new Vector3(.2f, .3f, -1f));
            var pivot = source.slotMount.parent;
            hive = Instantiate(pivot.gameObject); SceneManager.MoveGameObjectToScene(hive, scene);
            hive.hideFlags = HideFlags.HideAndDontSave; hive.transform.localScale = pivot.lossyScale;
            hive.transform.rotation = slot.rotation * Quaternion.Inverse(Quaternion.LookRotation(source.slotMount.forward, Vector3.up)) * pivot.rotation;
            hive.transform.position = slot.position - hive.transform.TransformVector(pivot.InverseTransformPoint(source.slotMount.TransformPoint(source.slotMountLocalPoint)));
            foreach (var script in hive.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            var art = hive.GetComponentsInChildren<Renderer>(true).FirstOrDefault(x => x.name == source.slotDecoration.name);
            if (art != null) art.enabled = false;
            hand = Obj("Simulated hand").transform;
            chunk = Obj("Review nectar").transform;
            chunk.gameObject.AddComponent<MeshFilter>().sharedMesh = source.honeyMesh;
            var renderer = chunk.gameObject.AddComponent<MeshRenderer>();
            feedback = new BeeHoneyFeedback(owner.transform, camera, head, second, slot, source.honeyMaterial, art, source.honeyMesh);
            feedback.Configure(1f, 0f, 0f);
            if (nectarShader != null) feedback.NectarMaterial.shader = nectarShader;
            if (feedbackShader != null) {
                foreach (string name in new[] { "dotsMaterial", "ribbonMaterial" }) {
                    var material = (Material)typeof(BeeHoneyFeedback).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(feedback);
                    material.shader = feedbackShader;
                    material.SetFloat("_Ribbon", name == "ribbonMaterial" ? 1f : 0f);
                }
            }
            renderer.sharedMaterial = feedback.NectarMaterial;
        }

        public void Step(float dt, float elapsed)
        {
            float t = elapsed % 12f;
            int phase = t < 2.5f ? 0 : t < 4.3f ? 1 : t < 6.5f ? 2 : t < 8f ? 3 : 4;
            if (phase != previousPhase) {
                if (phase == 0) {
                    cycle++; state = "none"; flower.SetActive(true); hive.SetActive(false); chunk.gameObject.SetActive(false);
                    hand.position = new Vector3(.17f, .12f, -.1f); hand.rotation = Quaternion.identity;
                    chunk.position = feedback.NectarRestPosition(0); chunk.rotation = Quaternion.identity;
                    camera.transform.position = new Vector3(.08f, .65f, -.42f);
                    camera.transform.LookAt(new Vector3(.055f, .12f, 0f)); camera.fieldOfView = 58f;
                    PhaseName = "1. Nectar gathering";
                } else if (phase == 1) {
                    feedback.Confirm("review-collect-" + cycle, "collect", 0, false, chunk.position, chunk.rotation);
                    state = "ready"; chunk.gameObject.SetActive(true); PhaseName = "2. Nectar formation";
                } else if (phase == 2) {
                    feedback.Confirm("review-pick-" + cycle, "pick", -1, false, chunk.position, chunk.rotation);
                    state = "held"; PhaseName = "3. Pickup and viscous thread";
                } else if (phase == 3) {
                    flower.SetActive(false); hive.SetActive(true);
                    hand.position = slot.position + slot.forward * .19f + slot.right * .06f;
                    camera.transform.position = slot.position + slot.forward * .72f + slot.up * .15f + slot.right * .14f;
                    camera.transform.LookAt(slot.position + slot.right * .1f - slot.up * .08f); camera.fieldOfView = 52f;
                    PhaseName = "4. Align at hive cell";
                } else {
                    feedback.Confirm("review-deposit-" + cycle, "deposit", -1, false, chunk.position, chunk.rotation);
                    state = "stored"; PhaseName = "5. Insert, seat and seal";
                }
                previousPhase = phase;
            }
            if (phase == 2) hand.position = new Vector3(.17f, .12f, -.1f) + new Vector3(.08f, .04f, -.04f) * Mathf.Clamp01((t - 4.3f) / 1.3f);
            Vector3 pos; Quaternion rot;
            if (state == "held") {
                if (feedback.PickupAnimating) feedback.PickupPose(hand, out pos, out rot);
                else BeeHoneyFeedback.FollowHandPose(chunk.position, chunk.rotation, hand, dt, out pos, out rot);
                chunk.SetPositionAndRotation(pos, rot);
            } else if (state == "stored") { feedback.DepositPose(out pos, out rot); chunk.SetPositionAndRotation(pos, rot); }
            else chunk.position = feedback.NectarRestPosition(0);
            float charge = phase == 0 ? Mathf.Clamp01(t / 2.3f) : 0f;
            chunk.localScale = feedback.NectarScale(state);
            feedback.Step(dt, true, phase < 3 ? 0 : -1, charge, false, false, phase < 3 ? 0 : -1,
                phase < 3, 0, 0, state, chunk.position, hand, .35f);
        }

        public void Render() => camera.Render();
        public Texture2D Pixels()
        {
            var previous = RenderTexture.active; RenderTexture.active = Image;
            var pixels = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try { pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); pixels.Apply(); }
            finally { RenderTexture.active = previous; }
            return pixels;
        }

        public void Dispose()
        {
            feedback?.Dispose();
            if (camera != null) camera.targetTexture = null;
            if (Image != null) { Image.Release(); DestroyImmediate(Image); }
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
