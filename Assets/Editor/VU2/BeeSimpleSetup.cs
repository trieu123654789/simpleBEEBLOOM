using System;
using System.IO;
using System.Linq;
using BeeBloom.HoneyPilot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BeeSimpleSetup
{
    static JObject Capture()
    {
        var data = new JObject();
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var c in root.GetComponentsInChildren<Component>(true)) {
                if (c == null) continue;
                var json = JObject.Parse(EditorJsonUtility.ToJson(c));
                if (c is BeeHoneyPilot) {
                    var fields = (JObject)(json["MonoBehaviour"] ?? json);
                    fields.Remove("useSimpleToolchain"); fields.Remove("simpleEndpoint");
                }
                data[c.GetEntityId().ToString()] = json;
            }
        return data;
    }
    public static string Configure(string pcIp)
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.isDirty || scene.path!="Assets/Scenes/VU2/SCN_BeeBloom.unity")
            throw new InvalidOperationException("Save the latest SCN_BeeBloom in Edit Mode before configuring SIMPLE.");
        var before=Capture();
        Directory.CreateDirectory("Library/HoneyPilot");
        File.WriteAllText("Library/HoneyPilot/simple-component-baseline.json",before.ToString(Formatting.Indented));
        var pilot=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<BeeHoneyPilot>(true)).Single();
        Undo.RecordObject(pilot,"Connect Bee Bloom through SIMPLE");
        pilot.useSimpleToolchain=true;
        pilot.simpleEndpoint="ws://"+pcIp+":8080";
        EditorUtility.SetDirty(pilot);
        var after=Capture();
        if (!JToken.DeepEquals(before,after)) throw new InvalidOperationException("Existing scene component settings changed; scene was not saved.");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Saved only the new SIMPLE connection fields; all "+before.Count+" existing components unchanged.";
    }

    public static string SaveVerifiedConnection()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path!="Assets/Scenes/VU2/SCN_BeeBloom.unity")
            throw new InvalidOperationException("Use SCN_BeeBloom in Edit Mode.");
        var baseline=JObject.Parse(File.ReadAllText("Library/HoneyPilot/simple-component-baseline.json"));
        foreach (var item in baseline.Properties()) {
            var fields=item.Value["MonoBehaviour"] as JObject;
            if ((string)fields?["m_EditorClassIdentifier"]=="Assembly-CSharp::BeeBloom.HoneyPilot.BeeHoneyPilot") {
                fields.Remove("useSimpleToolchain");fields.Remove("simpleEndpoint");
            }
        }
        if (!JToken.DeepEquals(baseline,Capture())) throw new InvalidOperationException("Scene changed since the connection update; preserve the user's edits and inspect again.");
        EditorSceneManager.SaveScene(scene);
        return "Saved SIMPLE fields. Verified all "+baseline.Count+" existing components unchanged.";
    }

    public static string BuildApk()
    {
        if (EditorApplication.isPlaying || EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)
            throw new InvalidOperationException("Use Edit Mode with Android already selected.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Save the scene before building.");
        EditorApplication.delayCall += () => {
            bool internet=PlayerSettings.Android.forceInternetPermission;
            try {
                PlayerSettings.Android.forceInternetPermission=true;
                Directory.CreateDirectory("Experiments/BeeBloom-HoneyPilot/Build");
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes=new[]{"Assets/Scenes/VU2/SCN_BeeBloom.unity"},target=BuildTarget.Android,
                    locationPathName="Experiments/BeeBloom-HoneyPilot/Build/BeeBloom-SIMPLE.apk",
                    options=BuildOptions.Development|BuildOptions.CompressWithLz4
                });
                File.WriteAllText("Library/HoneyPilot/simple-apk-build.json",JsonConvert.SerializeObject(new {
                    result=report.summary.result.ToString(),errors=report.summary.totalErrors,
                    warnings=report.summary.totalWarnings,size=report.summary.totalSize,
                    path=report.summary.outputPath,finishedUtc=DateTime.UtcNow.ToString("O")
                },Formatting.Indented));
            } catch(Exception e) {
                File.WriteAllText("Library/HoneyPilot/simple-apk-build.json",JsonConvert.SerializeObject(new {result="Exception",error=e.ToString()}));
            } finally { PlayerSettings.Android.forceInternetPermission=internet;AssetDatabase.SaveAssets(); }
        };
        return "APK build scheduled. Existing Android settings will be restored.";
    }
}
