using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BeeAudioChecks
{
    [MenuItem("Let It Bee/Check Wing Audio")]
    public static void RunMenu() => Debug.Log(Run());

    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play first.");
        var scene = EditorSceneManager.NewPreviewScene();
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        int count = 0;
        Action<bool, string> check = (ok, message) => { if (!ok) throw new Exception(message); count++; };
        try
        {
            var go = new GameObject("Audio check");
            go.SetActive(false);
            SceneManager.MoveGameObjectToScene(go, scene);
            var flap = go.AddComponent<BeeFlapping>();
            typeof(BeeFlapping).GetMethod("SetupAudio", flags).Invoke(flap, null);
            check(flap.flapClip != null && flap.flapClip.name == "ChubbyBumblebeeLoop", "Wrong default clip");
            check(Mathf.Abs(flap.flapClip.length - 2.26f) < .01f, "Wrong loop duration");
            check(flap.flapAudioSource.loop && !flap.flapAudioSource.playOnAwake && flap.flapAudioSource.dopplerLevel == 0f, "Source configuration mismatch");
            flap.SetFlapAudioEnabled(false);
            check(!(bool)typeof(BeeFlapping).GetField("_audioEnabled", flags).GetValue(flap), "Mute not persistent");
            flap.SetFlapAudioEnabled(true);
            check((bool)typeof(BeeFlapping).GetField("_audioEnabled", flags).GetValue(flap), "Unmute failed");
            flap.ExternalFlapBoost = 1f;
            typeof(BeeFlapping).GetMethod("UpdateWingBeat", flags).Invoke(flap, null);
            check((float)typeof(BeeFlapping).GetField("_audibleWingStrength", flags).GetValue(flap) > 0f, "Animation boost not shared with audio");
            flap.ExternalFlapBoost = -1f;
            flap.idleFlap = false;
            typeof(BeeFlapping).GetMethod("UpdateWingBeat", flags).Invoke(flap, null);
            check((float)typeof(BeeFlapping).GetField("_audibleWingStrength", flags).GetValue(flap) == 0f, "Stopped wings still audible");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        return "Wing audio: " + count + " checks passed.";
    }
}
