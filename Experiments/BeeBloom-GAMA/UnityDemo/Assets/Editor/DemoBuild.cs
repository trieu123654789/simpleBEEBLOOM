using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DemoBuild
{
    [MenuItem("Bee Bloom Lab/Create demo scene")]
    public static void CreateScene()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var demo = new GameObject("Bee Bloom GAMA demo").AddComponent<BeeBloomDemo>();
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/DemoMaterial.mat");
        if (material == null) {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material,"Assets/DemoMaterial.mat");
        }
        demo.demoMaterial = material;
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/BeeBloomGamaDemo.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/BeeBloomGamaDemo.unity",true)};
        // Only affects this standalone experimental project.
        PlayerSettings.runInBackground=true;
        PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=800;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.companyName="BeeBloomLab"; PlayerSettings.productName="Bee Bloom - GAMA Lab";
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        AssetDatabase.SaveAssets();
    }
    public static void Build()
    {
        CreateScene();
        string output=Path.GetFullPath("../Build/BeeBloomGamaDemo.exe");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/Scenes/BeeBloomGamaDemo.unity"}, locationPathName=output,
            target=BuildTarget.StandaloneWindows64, options=BuildOptions.Development
        });
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Demo build failed: "+report.summary.result);
        Debug.Log("DEMO_BUILD_OK "+output);
    }
}
