using System;
using System.Linq;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

// Editor-only test session. Clone XR settings in memory so connection tests do
// not start Quest Link or modify the user's XR assets/scene.
[InitializeOnLoad]
public static class BeeSimpleCheckSession
{
    const string ActiveKey = "BeeBloom.Simple.NoHeadsetChecks";
    const string BackgroundKey = "BeeBloom.Simple.CheckBackground";
    const string XrCloneName = "BeeBloom isolated XR checks (temporary)";
    const string SkyCloneName = "BeeBloom isolated sky checks (temporary)";
    const string SkyOriginalKey = "BeeBloom.Simple.CheckSkyOriginal";
    static XRGeneralSettings original, temporary;
    static Material originalSky, temporarySky;
    public static bool Active => SessionState.GetBool(ActiveKey, false);

    static BeeSimpleCheckSession()
    {
        EditorApplication.playModeStateChanged += Changed;
        if (Active) UseTemporaryXrSettings();
    }

    [MenuItem("Bee Bloom/Check SIMPLE without headset")]
    public static void Begin()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Keep the latest saved scene in Edit Mode before isolated checks.");
        SessionState.SetBool(BackgroundKey, Application.runInBackground);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool("BeeBloom.Simple.IsolatedChecks", true);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnEnterPlayMode]
    static void BeforePlay(EnterPlayModeOptions options)
    {
        if (Active) UseTemporaryXrSettings();
    }

    static void UseTemporaryXrSettings()
    {
        if (temporary != null) return;
        original = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (original != null) {
            temporary = FindTemporary<XRGeneralSettings>(XrCloneName);
            if (temporary == null) temporary = UnityEngine.Object.Instantiate(original);
            temporary.name = XrCloneName;
            temporary.hideFlags = HideFlags.HideAndDontSave;
            temporary.InitManagerOnStart = false;
            XRGeneralSettings.Instance = temporary;
        }
        Application.runInBackground = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ProtectSkyBeforeUpdates()
    {
        if (!Active || temporarySky != null) return;
        originalSky = RenderSettings.skybox;
        if (originalSky == null) return;
        temporarySky = UnityEngine.Object.Instantiate(originalSky);
        temporarySky.name = SkyCloneName;
        temporarySky.hideFlags = HideFlags.HideAndDontSave;
        SessionState.SetString(SkyOriginalKey, AssetDatabase.GetAssetPath(originalSky));
        RenderSettings.skybox = temporarySky;
    }

    static void Changed(PlayModeStateChange state)
    {
        if (!Active) return;
        if (state == PlayModeStateChange.EnteredPlayMode) {
            Application.runInBackground = true;
            ProtectSkyBeforeUpdates();
            BeeSimpleChecks.Begin();
        }
        if (state == PlayModeStateChange.EnteredEditMode) {
            XRGeneralSettings.Instance = original;
            originalSky = AssetDatabase.LoadAssetAtPath<Material>(SessionState.GetString(SkyOriginalKey, ""));
            temporarySky = FindTemporary<Material>(SkyCloneName);
            if (RenderSettings.skybox == temporarySky) RenderSettings.skybox = originalSky;
            if (temporarySky != null) UnityEngine.Object.DestroyImmediate(temporarySky);
            if (temporary != null) UnityEngine.Object.DestroyImmediate(temporary);
            temporary = null; temporarySky = null;
            Application.runInBackground = SessionState.GetBool(BackgroundKey, false);
            SessionState.EraseBool(ActiveKey);
            SessionState.EraseBool(BackgroundKey);
            SessionState.EraseBool("BeeBloom.Simple.IsolatedChecks");
            SessionState.EraseString(SkyOriginalKey);
            SessionState.EraseString("BeeBloom.Simple.CheckEndpoint");
        }
    }

    static T FindTemporary<T>(string name) where T : UnityEngine.Object
        => Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(value => value.name == name && value.hideFlags == HideFlags.HideAndDontSave);

    public static void Finish()
    {
        if (Active) EditorApplication.isPlaying = false;
    }
}
