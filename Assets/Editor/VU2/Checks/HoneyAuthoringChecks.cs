using System;
using System.Linq;
using LetItBee.HoneyPilot;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Preview safety and prefab ownership, independent of GAMA and headset input.
public static class HoneyAuthoringChecks
{
    [MenuItem("Let It Bee/Check Honey Authoring")]
    public static void Menu() => Debug.Log(Run());

    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play first.");
        var pilot = UnityEngine.Object.FindFirstObjectByType<BeeHoneyPilot>();
        if (pilot == null || SceneView.lastActiveSceneView == null)
            throw new InvalidOperationException("Open the game scene and Scene View.");
        HoneyInspectorPreview.Reset();
        var library = BeeHoneyPresentationLibrary.Load();
        var before = EditorJsonUtility.ToJson(pilot);
        var view = SceneView.lastActiveSceneView;
        var pivot = view.pivot; var rotation = view.rotation;
        int scenes = SceneManager.sceneCount;
        bool dirty = pilot.gameObject.scene.isDirty;
        int checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        try {
            Check(library != null && library.HasEffectPrefabs, "All three effect prefabs must be assigned.");
            string prefabBefore = EditorJsonUtility.ToJson(library.nectarPrefab);
            string libraryBefore = EditorJsonUtility.ToJson(library);
            foreach (float time in new[] { 1.5f, 4.6f, 8.2f }) {
                HoneyInspectorPreview.Start(pilot,time);
                Check(HoneyInspectorPreview.Active && HoneyInspectorPreview.LastError == null, "Each phase starts in Edit Mode.");
                var root = Resources.FindObjectsOfTypeAll<GameObject>().First(x=>x.name=="Honey Effect Preview (temporary)");
                Check(root.scene == pilot.gameObject.scene, "Preview renders in the selected object's scene.");
                Check(root.GetComponentsInChildren<BeeHoneyEffectRig>(true).Length == 3, "Runtime must instantiate all three prefab rigs.");
                Check(root.GetComponentsInChildren<Collider>(true).Length == 0, "Preview must not affect physical interaction.");
                var art = root.transform.Find("Preview hive art");
                Check(art != null, "Deposit preview uses a disposable hive copy.");
                var copy = art.GetComponentsInChildren<Renderer>(true).First(x=>x.name==pilot.slotDecoration.name);
                Check(!copy.enabled, "Static honey must not overlap animated nectar in the preview cell.");
                Check(Vector3.Distance(copy.bounds.center,pilot.slotDecoration.bounds.center)>1f,
                    "Preview hive must not overlap the authored hive.");
                Check(root.GetComponentsInChildren<MeshFilter>(true).Any(x=>x.sharedMesh==library.dropletMesh), "Baked droplet mesh is reused.");
                Check(SceneManager.sceneCount == scenes && view.pivot==pivot && view.rotation==rotation, "No extra scenes or camera movement.");
                HoneyInspectorPreview.Reset();
                Check(!Resources.FindObjectsOfTypeAll<GameObject>().Any(x=>x.name=="Honey Effect Preview (temporary)"), "Reset removes all temporary preview objects.");
            }
            Check(library.dropletMesh != null, "Disposing preview must not destroy shared mesh assets.");
            Check(EditorJsonUtility.ToJson(library)==libraryBefore && EditorJsonUtility.ToJson(library.nectarPrefab)==prefabBefore,
                "Preview must not mutate source assets.");
            Check(EditorJsonUtility.ToJson(pilot)==before && pilot.gameObject.scene.isDirty==dirty, "Preview preserves saved fields and scene dirty state.");
            return "Honey authoring: " + checks + " checks passed.";
        } finally { HoneyInspectorPreview.Reset(); }
    }
}
