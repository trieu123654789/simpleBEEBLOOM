using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DemoBuild
{
    [MenuItem("Let It Bee Lab/Create demo scene")]
    public static void CreateScene()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var demo = new GameObject("Let It Bee GAMA demo").AddComponent<LetItBeeDemo>();
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/DemoMaterial.mat");
        if (material == null) {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material,"Assets/DemoMaterial.mat");
        }
        demo.demoMaterial = material;
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/LetItBeeGamaDemo.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/LetItBeeGamaDemo.unity",true)};
        // Only affects this standalone experimental project.
        PlayerSettings.runInBackground=true;
        PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=800;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.companyName="LetItBeeLab"; PlayerSettings.productName="Let It Bee - GAMA Lab";
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        AssetDatabase.SaveAssets();
    }
    public static void Build()
    {
        CreateScene();
        string output=Path.GetFullPath("../Build/LetItBeeGamaDemo.exe");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/Scenes/LetItBeeGamaDemo.unity"}, locationPathName=output,
            target=BuildTarget.StandaloneWindows64, options=BuildOptions.Development
        });
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Demo build failed: "+report.summary.result);
        Debug.Log("DEMO_BUILD_OK "+output);
    }
}
