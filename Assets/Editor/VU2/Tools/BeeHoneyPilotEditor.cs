using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit local commands only. Never saves/reloads the user's open scene.
public static class BeeHoneyPilotEditor
{
    const string DirectoryPath = "Library/HoneyPilot";
    static double nextCheck;
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextCheck || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        nextCheck = EditorApplication.timeSinceStartup + 1;
        string file = DirectoryPath + "/request.json";
        if (!File.Exists(file)) return;
        JObject request;
        try { request = JObject.Parse(File.ReadAllText(file)); File.Delete(file); }
        catch { return; }
        try {
            if ((string)request["command"] != "inspect") throw new InvalidOperationException("Unsupported command");
            var scenes = new JArray();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                var objects = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true));
                var relevant = objects.Where(t => t.name.Contains("HoaCuc") || t.name.Contains("CayDua") || t.name == "Head"
                    || t.name.Contains("Hive") || t.name.Contains("Cylinder") || t.name == "Object_4" || t.name.Contains("Controller")
                    || t.GetComponent<Camera>() != null).ToArray();
                scenes.Add(new JObject { ["path"] = scene.path, ["dirty"] = scene.isDirty,
                    ["objects"] = new JArray(relevant.Select(t => {
                        var renderer = t.GetComponent<Renderer>();
                        return new JObject { ["path"] = PathOf(t), ["id"] = t.GetEntityId().ToString(),
                            ["position"] = Vec(t.position), ["scale"] = Vec(t.lossyScale),
                            ["active"] = t.gameObject.activeInHierarchy,
                            ["asset"] = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject),
                            ["components"] = new JArray(t.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name)),
                            ["boundsCenter"] = renderer != null ? Vec(renderer.bounds.center) : null,
                            ["boundsSize"] = renderer != null ? Vec(renderer.bounds.size) : null };
                    })) });
            }
            Write(new JObject { ["ok"] = true, ["playing"] = EditorApplication.isPlaying, ["scenes"] = scenes });
        } catch (Exception ex) { Write(new JObject { ["ok"] = false, ["error"] = ex.ToString() }); }
    }
    static JArray Vec(Vector3 v) => new JArray(v.x, v.y, v.z);
    public static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    static void Write(JObject value) { System.IO.Directory.CreateDirectory(DirectoryPath); File.WriteAllText(DirectoryPath + "/response.json", value.ToString()); }
}
