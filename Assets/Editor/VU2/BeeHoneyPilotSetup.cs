using System;
using System.IO;
using System.Linq;
using BeeBloom.HoneyPilot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BeeHoneyPilotSetup
{
    const string RootName = "BeeBloom Honey Pilot";
    const string ScenePath = "Assets/Scenes/VU2/SCN_BeeBloom.unity";
    const string AssetFolder = "Assets/Prefabs/BeeBloom/HoneyPilot";
    const string BaselineKey = "BeeBloom.HoneyPilot.SceneBaseline";
    static JObject CaptureOriginals()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var data = new JObject();
        foreach (var root in scene.GetRootGameObjects()) {
            if (root.name == RootName) continue;
            foreach (var c in root.GetComponentsInChildren<Component>(true)) {
                if (c != null) data[c.GetEntityId().ToString()] = new JObject {
                    ["name"] = c.name, ["type"] = c.GetType().FullName, ["json"] = EditorJsonUtility.ToJson(c)
                };
            }
        }
        return data;
    }

    [MenuItem("VU2/Bee/Honey Pilot/Install two flowers and one slot")]
    public static void InstallMenu() { Debug.Log(Install()); }

    public static string Install()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != ScenePath || EditorApplication.isPlaying) throw new InvalidOperationException("Open SCN_BeeBloom in Edit Mode.");
        if (scene.isDirty) throw new InvalidOperationException("Scene has unsaved edits. Save your scene before installing the pilot.");
        if (GameObject.Find(RootName) != null) return "Pilot already installed; no settings changed.";
        var original = GameObject.Find("HoaCuc_Final");
        var tree = GameObject.Find("CayDua_Quad_Rig");
        var bee = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BeeController>(true)).Single();
        if (original == null || tree == null) throw new InvalidOperationException("Expected flower/coconut not found.");
        var hand = bee.GetComponent<BeeSwipeFlap>().rightControllerAnchor;
        if (hand == null) throw new InvalidOperationException("Saved right controller reference is missing.");
        var firstHead = original.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Head");
        var sourcePiece = tree.GetComponentsInChildren<MeshFilter>(true).Single(m => m.name == "Cylinder.001");
        var meshBounds = sourcePiece.sharedMesh.bounds;
        var slotPosition = sourcePiece.transform.TransformPoint(meshBounds.center) + sourcePiece.transform.forward * 0.12f;
        if (slotPosition.y > bee.maxFlightAltitude - 1f) throw new InvalidOperationException("Slot is above the saved flight envelope.");
        SessionState.SetString(BaselineKey, CaptureOriginals().ToString(Formatting.None));
        Directory.CreateDirectory(AssetFolder);
        AssetDatabase.Refresh();
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetFolder + "/NectarChunk.asset");
        if (mesh == null) {
            mesh = UnityEngine.Object.Instantiate(sourcePiece.sharedMesh); mesh.name = "NectarChunk";
            var vertices = mesh.vertices; var b = mesh.bounds;
            for (int i = 0; i < vertices.Length; i++) vertices[i] = Vector3.Scale(vertices[i] - b.center,
                new Vector3(0.16f / b.size.x, 0.16f / b.size.y, 0.07f / b.size.z));
            mesh.vertices = vertices; mesh.RecalculateBounds(); mesh.RecalculateNormals();
            AssetDatabase.CreateAsset(mesh, AssetFolder + "/NectarChunk.asset");
        }
        var honey = MaterialAsset("Nectar", new Color(1f, 0.72f, 0.1f), false);
        if (honey.GetTexture("_BaseMap") == null) {
            honey.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Enviroment/coconutandhive/matong.png"));
            EditorUtility.SetDirty(honey);
        }
        var ghost = MaterialAsset("SlotGhost", new Color(1f, 0.75f, 0.15f, 0.3f), true);
        var cue = MaterialAsset("PollenCue", new Color(1f, 0.85f, 0.12f), false);
        var idle = CreateIdle();
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Bee Bloom: add honey and pollination pilot");
        var root = new GameObject(RootName); Undo.RegisterCreatedObjectUndo(root, "Add Bee Bloom honey pilot"); root.layer = 2;
        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(original);
        var flowerB = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), scene);
        Undo.RegisterCreatedObjectUndo(flowerB, "Add second pilot flower");
        flowerB.name = "Pilot Flower B"; flowerB.transform.SetParent(root.transform, true);
        flowerB.transform.position = original.transform.position + new Vector3(2f, 0f, 0f);
        flowerB.transform.rotation = original.transform.rotation; flowerB.transform.localScale = original.transform.lossyScale;
        float aGround = GroundAt(original.transform.position), bGround = GroundAt(flowerB.transform.position);
        flowerB.transform.position += Vector3.up * (bGround - aGround);
        var animB = flowerB.GetComponent<Animator>(); if (animB == null) animB = flowerB.AddComponent<Animator>();
        animB.runtimeAnimatorController = idle;
        var slot = new GameObject("Pilot Slot A"); slot.layer = 2; slot.transform.SetParent(root.transform, false);
        slot.transform.position = slotPosition; slot.transform.rotation = Quaternion.LookRotation(sourcePiece.transform.forward, Vector3.up);
        var pilot = root.AddComponent<BeeHoneyPilot>();
        pilot.beeRoot = bee.transform; pilot.rightHand = hand;
        pilot.flowerAHead = firstHead; pilot.flowerBHead = flowerB.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Head");
        pilot.flowerAAnimator = original.GetComponent<Animator>(); pilot.flowerBAnimator = animB;
        pilot.pilotFlowerIdle = idle; pilot.slot = slot.transform; pilot.slotDecoration = sourcePiece.GetComponent<Renderer>();
        pilot.slotMount = sourcePiece.transform; pilot.slotMountLocalPoint = sourcePiece.transform.InverseTransformPoint(slotPosition);
        pilot.honeyMesh = mesh; pilot.honeyMaterial = honey; pilot.ghostMaterial = ghost; pilot.cueMaterial = cue;
        pilot.trackedCamera = bee.GetComponent<BeeCameraRig>().trackedCamera;
        pilot.labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        string repo = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        pilot.modelOnServer = Path.Combine(repo, "Experiments/BeeBloom-HoneyPilot/GAMA/BeeBloomHoneyPilot.gaml").Replace('\\', '/');
        pilot.checkpointDirectoryOnServer = Path.Combine(repo, "Experiments/BeeBloom-HoneyPilot/Runtime/checkpoints").Replace('\\', '/');
        Directory.CreateDirectory(pilot.checkpointDirectoryOnServer);
        pilot.sessionId = Guid.NewGuid().ToString("N");
        EditorUtility.SetDirty(pilot); EditorSceneManager.MarkSceneDirty(scene); Undo.CollapseUndoOperations(group);
        AssetDatabase.SaveAssets();
        return VerifyAndSave();
    }

    public static string ConfigureMovingSlot()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath || scene.isDirty) throw new InvalidOperationException("Saved scene in Edit Mode required.");
        SessionState.SetString(BaselineKey, CaptureOriginals().ToString(Formatting.None));
        var pilot = GameObject.Find(RootName).GetComponent<BeeHoneyPilot>();
        Undo.RecordObject(pilot, "Follow existing hive animation");
        pilot.slotMount = pilot.slotDecoration.transform; pilot.slotMountLocalPoint = pilot.slotMount.InverseTransformPoint(pilot.slot.position);
        EditorUtility.SetDirty(pilot); EditorSceneManager.MarkSceneDirty(scene); return VerifyAndSave();
    }

    public static string VerifyAndSave()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath) throw new InvalidOperationException("Edit Mode required.");
        var saved = SessionState.GetString(BaselineKey, "");
        if (saved == "") throw new InvalidOperationException("No current baseline; refusing to save.");
        var baseline = JObject.Parse(saved); var current = CaptureOriginals();
        var differences = baseline.Properties().Where(p => !JToken.DeepEquals(p.Value, current[p.Name])).Select(p => p.Name).ToArray();
        if (differences.Length > 0 || current.Count != baseline.Count) throw new InvalidOperationException("Original scene objects changed; refusing to save: " + string.Join(",", differences));
        var pilot = GameObject.Find(RootName).GetComponent<BeeHoneyPilot>();
        bool savedScene = EditorSceneManager.SaveScene(scene);
        return JsonConvert.SerializeObject(new { saved = savedScene, originalComponentsUnchanged = baseline.Count,
            pilot = RootName, flowerB = pilot.flowerBHead.position.ToString(), slot = pilot.slot.position.ToString(), session = pilot.sessionId });
    }

    static Material MaterialAsset(string name, Color color, bool transparent)
    {
        string path = AssetFolder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path); if (mat != null) return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.name = name; mat.SetColor("_BaseColor", color);
        if (transparent) {
            mat.SetFloat("_Surface", 1); mat.SetFloat("_SrcBlend", 5); mat.SetFloat("_DstBlend", 10); mat.SetFloat("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.SetOverrideTag("RenderType", "Transparent"); mat.renderQueue = 3000;
        }
        mat.SetFloat("_Cull", 0); AssetDatabase.CreateAsset(mat, path); return mat;
    }
    static RuntimeAnimatorController CreateIdle()
    {
        string path = AssetFolder + "/FlowerPilotIdle.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path); if (controller != null) return controller;
        var clip = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Enviroment/daisy/FlowerRig_Sway_Gentle.anim"));
        clip.name = "Pilot Gentle Sway";
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, AssetFolder + "/FlowerPilotIdle.anim");
        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var state = controller.layers[0].stateMachine.AddState("Gentle Sway"); state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state; return controller;
    }
    static float GroundAt(Vector3 p)
    {
        float top = float.NegativeInfinity;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) {
            if (!mf.gameObject.activeInHierarchy || mf.sharedMesh == null || !BeeMapSurfaces.IsGround(mf.name)) continue;
            var rr = mf.GetComponent<Renderer>(); if (rr == null) continue; var bounds = rr.bounds;
            if (p.x < bounds.min.x || p.x > bounds.max.x || p.z < bounds.min.z || p.z > bounds.max.z) continue;
            var vs = mf.sharedMesh.vertices; var ts = mf.sharedMesh.triangles;
            for (int k = 0; k < ts.Length; k += 3) {
                var a = mf.transform.TransformPoint(vs[ts[k]]); var b = mf.transform.TransformPoint(vs[ts[k + 1]]); var c = mf.transform.TransformPoint(vs[ts[k + 2]]);
                float den = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z); if (Mathf.Abs(den) < 0.0000001f) continue;
                float u = ((b.z - c.z) * (p.x - c.x) + (c.x - b.x) * (p.z - c.z)) / den;
                float v = ((c.z - a.z) * (p.x - c.x) + (a.x - c.x) * (p.z - c.z)) / den;
                if (u >= -0.0001f && v >= -0.0001f && u + v <= 1.0001f) top = Mathf.Max(top, u * a.y + v * b.y + (1 - u - v) * c.y);
            }
        }
        if (float.IsNegativeInfinity(top)) throw new InvalidOperationException("No ground at pilot flower position " + p);
        return top;
    }

    [MenuItem("VU2/Bee/Honey Pilot/New session (keep scene tuning)")]
    public static void NewSession()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var pilot = GameObject.Find(RootName)?.GetComponent<BeeHoneyPilot>(); if (pilot == null) return;
        Undo.RecordObject(pilot, "New honey pilot session"); pilot.sessionId = Guid.NewGuid().ToString("N");
        EditorUtility.SetDirty(pilot); EditorSceneManager.MarkSceneDirty(pilot.gameObject.scene);
    }
}
