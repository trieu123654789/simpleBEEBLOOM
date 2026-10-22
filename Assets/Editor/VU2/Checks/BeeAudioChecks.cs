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
            check(flap.flapClip.length > 1f && flap.flapClip.length <= 4f, "Wing loop must remain short enough for immediate loading");
            var samples = new float[flap.flapClip.samples * flap.flapClip.channels];
            flap.flapClip.GetData(samples, 0);
            check(Mathf.Abs(samples[0]) < 0.01f && Mathf.Abs(samples[samples.Length - 1]) < 0.01f, "Wing loop boundary must not click");
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
            var profile = Resources.Load<LetItBee.HoneyPilot.BeeInteractionAudio>("BeeAudio/InteractionAudio");
            check(profile != null && profile.landingClip != null && profile.landingClip.name == "PetalLanding", "Petal landing must use the supplied contact clip");
            check(profile.honeyFormation.name == "NectarFormation" && profile.honeyPickup.name == "NectarPickup"
                && profile.honeyFormation != profile.honeyPickup, "Formation and pickup need separate supplied clips");
            check(profile.formationImpactSeconds <= profile.honeyFormation.length && profile.pickupImpactSeconds <= profile.honeyPickup.length,
                "Animation accents must lie within their audio clips");
            var flight = Resources.Load<AudioClip>("BeeAudio/FlightFlutterLoop");
            check(flight != null && flight.channels == 1 && flight.length > 1f && flight.length < 2f, "Prepared flight phrase must be a short mono loop");
            var loop = new float[flight.samples]; flight.GetData(loop, 0);
            check(Mathf.Abs(loop[0] - loop[loop.Length - 1]) < 0.015f, "Flight phrase loop seam must not click");
            var authored = UnityEngine.Object.FindObjectsByType<BeeFlapping>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            check(Array.Exists(authored, f => f.gameObject.scene.name == "SCN_LetItBee" && f.flapClip == flight),
                "Current game scene must use the requested flutter clip rather than the legacy default");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        return "Wing audio: " + count + " checks passed.";
    }
}
