#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LetItBee.HoneyPilot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BeeVietnamIntegrationChecks
{
    public const string ScenePath = "Assets/Scenes/VU2/SCN_LetItBee_Vietnam.unity";

    [MenuItem("Let It Bee/Checks/Vietnam Integration")]
    public static void Menu() => Debug.Log(Run());

    public static string Run()
    {
        var target = SceneManager.GetActiveScene();
        if (Application.isPlaying || target.path != ScenePath)
            throw new InvalidOperationException("Open the Vietnam integration scene in Edit Mode first.");
        var checks = new List<string>();
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks.Add(label); }
        var roots = target.GetRootGameObjects();
        T[] All<T>() where T : Component => roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        var pilot = All<BeeHoneyPilot>().Single();
        var source = EditorSceneManager.OpenPreviewScene("Assets/Scenes/VU2/SCN_LetItBee.unity");
        try {
            foreach (var root in source.GetRootGameObjects()) {
                if (root.name == "ENVIRONMENT" || root.name == "Scenery" || root.name == "VFX") continue;
                var copy = roots.FirstOrDefault(r => r.name == root.name);
                // Legacy ray roots have duplicate names. Match their components through their common root name.
                if (roots.Count(r => r.name == root.name) > 1) copy = roots.First(r => r.name == root.name);
                Check(copy != null, "Retained root: " + root.name);
                foreach (var component in root.GetComponentsInChildren<Component>(true)) {
                    if (component == null || component is Transform) continue;
                    string path = AnimationUtility.CalculateTransformPath(component.transform, root.transform);
                    var node = path.Length == 0 ? copy.transform : copy.transform.Find(path);
                    Check(node != null, "Retained object: " + root.name + "/" + path);
                    var peers = component.GetComponents(component.GetType());
                    int index = Array.IndexOf(peers, component);
                    var matches = node.GetComponents(component.GetType());
                    Check(index < matches.Length, "Retained component: " + component.GetType().Name);
                    var other = matches[index];
                    // Populate TMP render caches in both scenes before comparing saved settings.
                    if (component is TMPro.TMP_Text originalText && other is TMPro.TMP_Text copiedText) {
                        originalText.ForceMeshUpdate(true, true); copiedText.ForceMeshUpdate(true, true);
                    }
                    Check(Normalize(component) == Normalize(other), "Saved settings unchanged: " + component.name + " / " + component.GetType().Name);
                    var a = References(component); var b = References(other);
                    Check(a.SequenceEqual(b), "References retained: " + component.name + " / " + component.GetType().Name);
                }
            }
            var originalPilot = source.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BeeHoneyPilot>(true)).Single();
            Check(pilot.sessionId != originalPilot.sessionId && Guid.TryParse(pilot.sessionId, out _), "Separate valid checkpoint session");
        } finally { EditorSceneManager.ClosePreviewScene(source); }
        Check(All<Camera>().Count(c => c.enabled && c.gameObject.activeInHierarchy) == 1, "One active VR camera");
        Check(All<AudioListener>().Count(c => c.enabled && c.gameObject.activeInHierarchy) == 1, "One active audio listener");
        Check(All<UnityEngine.EventSystems.EventSystem>().Count(c => c.isActiveAndEnabled) == 1, "One EventSystem");
        Check(All<Transform>().Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)) == 0, "No missing scripts");
        Check(All<BeeEnvironmentSurface>().Count(s => s.kind == BeeEnvironmentSurface.SurfaceKind.Water) == 4, "Four classified water surfaces");
        foreach (var marker in All<BeeEnvironmentSurface>()) {
            var colliders = marker.GetComponentsInChildren<Collider>();
            Check(colliders.Length > 0, "Surface has collider: " + marker.name);
            foreach (var collider in colliders)
                Check(marker.kind == BeeEnvironmentSurface.SurfaceKind.Water ? BeeMapSurfaces.IsWater(collider) : BeeMapSurfaces.IsGround(collider), "Explicit surface classification: " + collider.name);
        }
        Check(BeeMapSurfaces.IsWater("SF_Water_Sea (1)") && BeeMapSurfaces.IsGround("dike"), "Legacy surface classification retained");
        using (var hive = new BeeHiveLayout(pilot.slotDecoration, pilot.slotMount, pilot.slotMountLocalPoint, pilot.slot, pilot.hiveCells)) {
            Check(hive.Count == 7, "Seven hive cells");
            Check(hive.Configuration().Select(c => (string)c["id"]).Distinct().Count() == 7, "Unique hive IDs");
        }
        var world = new BeeWorldSync().Capture(pilot);
        Check(world.Count == 4, "Player, two flowers and hive in one scene registry");
        Check((BeeHoneyPilot.FromGama(BeeHoneyPilot.ToGama(pilot.flowerAHead.position)) - pilot.flowerAHead.position).magnitude < 0.001f, "Unity/GAMA coordinate round trip");
        Physics.SyncTransforms();
        var ground = All<BeeEnvironmentSurface>().Single(s => s.kind == BeeEnvironmentSurface.SurfaceKind.Ground && s.name == "land2").GetComponentInChildren<Collider>();
        foreach (var t in new[] {pilot.beeRoot, pilot.flowerAAnimator.transform, pilot.flowerBAnimator.transform}) {
            Check(ground.Raycast(new Ray(new Vector3(t.position.x, 50, t.position.z), Vector3.down), out var hit, 100), "Ground under " + t.name);
            Check(t.position.y >= hit.point.y, "Above ground: " + t.name);
        }
        return "Vietnam integration: " + checks.Count + " checks passed. Hardware and live server tests are separate.";
    }

    static string Normalize(Component c)
    {
        string json = EditorJsonUtility.ToJson(c);
        json = Regex.Replace(json, "\"instanceID\":-?\\d+", "\"instanceID\":0");
        if (c is BeeHoneyPilot) json = Regex.Replace(json, "\"sessionId\":\"[^\"]*\"", "\"sessionId\":\"isolated\"");
        return json;
    }

    static string[] References(Component c)
    {
        var list = new List<string>();
        var it = new SerializedObject(c).GetIterator();
        while (it.Next(true)) {
            if (it.propertyType != SerializedPropertyType.ObjectReference || it.propertyPath.StartsWith("m_Prefab")) continue;
            var value = it.objectReferenceValue;
            string id = "null";
            if (value != null) {
                string path = AssetDatabase.GetAssetPath(value);
                var component = value as Component;
                var go = value as GameObject;
                if (component != null) go = component.gameObject;
                id = path.Length > 0 ? path + ":" + value.name : go != null ? AnimationUtility.CalculateTransformPath(go.transform, null) + ":" + value.GetType().Name : value.name;
            }
            list.Add(it.propertyPath + "=" + id);
        }
        return list.ToArray();
    }
}
#endif