using System;
using System.Linq;
using LetItBee.HoneyPilot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Temporary presentation only. Never invokes pilot input, network or inventory.
[InitializeOnLoad]
public static class HoneyInspectorPreview
{
    static GameObject root;
    static Component target;
    static Transform head, second, slot, hand, chunk;
    static BeeHoneyFeedback feedback;
    static float elapsed;
    static double previous;
    static int phase = -1;
    static string state;
    static bool paused;
    public static string LastError { get; private set; }
    public static bool Active => root != null;

    static HoneyInspectorPreview()
    {
        AssemblyReloadEvents.beforeAssemblyReload += Reset;
        EditorApplication.playModeStateChanged += _ => Reset();
        EditorSceneManager.sceneSaving += (scene, path) => Reset();
        EditorApplication.quitting += Reset;
    }

    public static void Draw(Component selected)
    {
        if (Application.isPlaying) return;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Honey Effect Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Edit Mode preview only. Orbit and move freely in Scene View. Reset removes temporary effects; gameplay and GAMA are untouched.", MessageType.Info);
        if (EditorUtility.IsPersistent(selected)) {
            if (GUILayout.Button("Open Prefab to Preview"))
                AssetDatabase.OpenAsset(selected.gameObject);
            return;
        }
        using (new EditorGUILayout.HorizontalScope()) {
            if (GUILayout.Button("Form Nectar")) Start(selected, 0f);
            if (GUILayout.Button("Lift Nectar")) Start(selected, 4.3f);
            if (GUILayout.Button("Deposit Nectar")) Start(selected, 8f);
        }
        using (new EditorGUILayout.HorizontalScope()) {
            if (GUILayout.Button("Play Sequence")) Start(selected, 0f);
            using (new EditorGUI.DisabledScope(!Active)) {
                if (GUILayout.Button(paused ? "Resume" : "Pause")) paused = !paused;
                if (GUILayout.Button("Frame Preview")) Frame();
                if (GUILayout.Button("Reset Preview")) Reset();
            }
        }
        if (!string.IsNullOrEmpty(LastError)) EditorGUILayout.HelpBox(LastError, MessageType.Error);
    }

    public static void Start(Component selected, float startTime)
    {
        Reset(); LastError = null;
        try {
            if (Application.isPlaying || selected == null || EditorUtility.IsPersistent(selected))
                throw new InvalidOperationException("Open a prefab or select a scene object in Edit Mode.");
            var view = SceneView.lastActiveSceneView;
            if (view == null || view.camera == null) throw new InvalidOperationException("Open the Scene tab first.");
            target = selected;
            root = new GameObject("Honey Effect Preview (temporary)");
            SceneManager.MoveGameObjectToScene(root, selected.gameObject.scene);
            root.hideFlags = HideFlags.HideAndDontSave;
            var pilot = selected as BeeHoneyPilot;
            Vector3 origin = selected.transform.position;
            var library = BeeHoneyPresentationLibrary.Load();
            if (library == null) throw new InvalidOperationException("PresentationLibrary is missing.");
            head = pilot != null && pilot.flowerAHead != null ? pilot.flowerAHead : Anchor("Flower anchor", origin - Vector3.up * .25f);
            // Sample the runtime idle pose on a disposable art copy, never on the user's flower.
            if (pilot != null && pilot.flowerAAnimator != null && pilot.pilotFlowerIdle != null) {
                var flower = UnityEngine.Object.Instantiate(pilot.flowerAAnimator.gameObject, root.transform);
                flower.name = "Preview flower art";
                flower.transform.localScale = pilot.flowerAAnimator.transform.lossyScale;
                flower.transform.rotation = pilot.flowerAAnimator.transform.rotation;
                foreach (var behaviour in flower.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
                foreach (var collider in flower.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                var clips = pilot.pilotFlowerIdle.animationClips;
                if (clips.Length > 0) clips[0].SampleAnimation(flower,.45f);
                head = flower.GetComponentsInChildren<Transform>(true).First(x=>x.name==pilot.flowerAHead.name);
                flower.transform.position = pilot.flowerAAnimator.transform.position + Vector3.right * 6f;
            }
            second = Anchor("Inactive flower", head.position + Vector3.left * 3f);
            slot = Anchor("Preview socket", origin + Vector3.right * .6f);
            Renderer socketArt = null;
            if (pilot != null && pilot.slotMount != null) {
                var sourceHive = pilot.slotMount.parent != null ? pilot.slotMount.parent : pilot.slotMount;
                var hive = UnityEngine.Object.Instantiate(sourceHive.gameObject,root.transform);
                hive.name = "Preview hive art";
                hive.transform.localScale = sourceHive.lossyScale;
                hive.transform.SetPositionAndRotation(sourceHive.position + Vector3.right * 6f,sourceHive.rotation);
                foreach (var behaviour in hive.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
                foreach (var collider in hive.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                var mountPath = AnimationUtility.CalculateTransformPath(pilot.slotMount,sourceHive);
                var mount = string.IsNullOrEmpty(mountPath) ? hive.transform : hive.transform.Find(mountPath);
                slot.position = mount.TransformPoint(pilot.slotMountLocalPoint);
                slot.rotation = mount.rotation;
                if (pilot.slotDecoration != null) {
                    var artPath = AnimationUtility.CalculateTransformPath(pilot.slotDecoration.transform,sourceHive);
                    var art = string.IsNullOrEmpty(artPath) ? hive.transform : hive.transform.Find(artPath);
                    socketArt = art != null ? art.GetComponent<Renderer>() : null;
                    // Use its saved shape for fitting, but draw only the animated nectar.
                    if (socketArt != null) socketArt.enabled = false;
                }
            }
            hand = Anchor("Preview hand", head.position + new Vector3(.17f,.37f,-.1f));
            Mesh mesh = pilot != null ? pilot.honeyMesh : library.nectarPrefab.GetComponent<MeshFilter>().sharedMesh;
            Material material = pilot != null ? pilot.honeyMaterial : library.nectarPrefab.GetComponent<Renderer>().sharedMaterial;
            chunk = BeeHoneyPresentationLibrary.CreateNectar(root.transform, mesh, material, "Preview nectar");
            feedback = new BeeHoneyFeedback(root.transform, view.camera, head, second, slot, material, socketArt, mesh);
            feedback.Configure(1f,0f,0f);
            chunk.GetComponent<Renderer>().sharedMaterial = feedback.NectarMaterial;
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                tr.gameObject.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(root, selected.gameObject.scene);
            elapsed = 0f; phase = -1; state = "none"; paused = false;
            Step(.001f);
            while (elapsed < startTime) { float dt = Mathf.Min(1f/60f,startTime-elapsed); elapsed += dt; Step(dt); }
            previous = EditorApplication.timeSinceStartup;
            EditorApplication.update += Update;
            SceneView.RepaintAll();
        } catch (Exception ex) { Reset(); LastError = ex.Message; Debug.LogException(ex); }
    }

    static Transform Anchor(string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform,false);
        go.transform.position = position;
        return go.transform;
    }

    static void Update()
    {
        if (root == null || target == null || !target.gameObject.scene.isLoaded) { Reset(); return; }
        double now = EditorApplication.timeSinceStartup;
        if (now-previous < 1.0/30.0) return;
        float dt = Mathf.Min(.05f,(float)(now-previous)); previous = now;
        if (paused) return;
        try {
            elapsed += dt;
            if (elapsed >= 12f) { paused = true; return; }
            Step(dt); SceneView.RepaintAll();
        } catch (Exception ex) { Reset(); LastError = ex.Message; Debug.LogException(ex); }
    }

    static void Step(float dt)
    {
        int next = elapsed < 2.5f ? 0 : elapsed < 4.3f ? 1 : elapsed < 6.5f ? 2 : elapsed < 8f ? 3 : 4;
        if (next != phase) {
            if (next == 0) { state = "none"; chunk.gameObject.SetActive(false); }
            if (next == 1) {
                chunk.position = feedback.NectarRestPosition(0);
                feedback.Confirm("preview-collect","collect",0,false,chunk.position,chunk.rotation);
                state = "ready"; chunk.gameObject.SetActive(true);
            }
            if (next == 2) {
                feedback.Confirm("preview-pick","pick",-1,false,chunk.position,chunk.rotation); state = "held";
            }
            if (next == 3) hand.position = slot.position + slot.forward*.19f + slot.right*.06f;
            if (next == 4) {
                feedback.Confirm("preview-deposit","deposit",-1,false,chunk.position,chunk.rotation); state = "stored";
            }
            phase = next;
        }
        if (phase == 2) hand.position = head.position + new Vector3(.17f,.37f,-.1f)
            + new Vector3(.08f,.04f,-.04f)*Mathf.Clamp01((elapsed-4.3f)/1.3f);
        Vector3 pos; Quaternion rot;
        if (state == "held") {
            if (feedback.PickupAnimating) feedback.PickupPose(hand,out pos,out rot);
            else BeeHoneyFeedback.FollowHandPose(chunk.position,chunk.rotation,hand,dt,out pos,out rot);
            chunk.SetPositionAndRotation(pos,rot);
        } else if (state == "stored") {
            feedback.DepositPose(out pos,out rot); chunk.SetPositionAndRotation(pos,rot);
        } else chunk.position = feedback.NectarRestPosition(0);
        chunk.localScale = feedback.NectarScale(state);
        feedback.Step(dt,true,phase<3 ? 0:-1,phase==0 ? Mathf.Clamp01(elapsed/2.3f):0f,
            false,false,phase<3 ? 0:-1,phase<3,0,0,state,chunk.position,hand,.35f);
    }

    public static void Frame()
    {
        if (root == null || SceneView.lastActiveSceneView == null) return;
        Vector3 centre = phase >= 3 ? slot.position : feedback.NectarRestPosition(0);
        SceneView.lastActiveSceneView.Frame(new Bounds(centre,Vector3.one*.7f),true);
    }

    public static void Reset()
    {
        EditorApplication.update -= Update;
        feedback?.Dispose(); feedback = null;
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
        root = null; target = null; paused = false;
        SceneView.RepaintAll();
    }
}

[CustomEditor(typeof(BeeNectarView))]
public sealed class BeeNectarViewInspector : Editor
{
    public override void OnInspectorGUI() { HoneyInspectorPreview.Draw((Component)target); DrawDefaultInspector(); }
}
[CustomEditor(typeof(BeeHoneyEffectRig))]
public sealed class BeeHoneyEffectRigInspector : Editor
{
    public override void OnInspectorGUI() { HoneyInspectorPreview.Draw((Component)target); DrawDefaultInspector(); }
}
