using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Exercise the saved scene tuning after its real miniature-scale setup, without entering XR Play Mode.
public static class BeeBalancedFlightChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Private).Invoke(obj, args);
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Private).SetValue(obj, value);
    static T Get<T>(object obj, string name) => (T)obj.GetType().GetField(name, Private).GetValue(obj);

    [MenuItem("Let It Bee/Check Balanced Flight")]
    public static void Menu() => Debug.Log(Run());
    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        var original = SceneManager.GetActiveScene(); bool dirty = original.isDirty;
        var source = UnityEngine.Object.FindAnyObjectByType<BeeController>();
        var sourceScale = source.GetComponent<BeeEmbodiment>();
        var swipe = UnityEngine.Object.FindAnyObjectByType<BeeSwipeFlap>();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        int checks = 0;
        var traces = new List<object>();
        BeeEmbodiment embodiment = null;
        void Check(bool valid, string message) { if (!valid) throw new Exception(message); checks++; }
        GameObject Obj(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go; }
        try {
            var root = Obj("Balanced flight test"); root.SetActive(false);
            var bee = root.AddComponent<BeeController>(); EditorUtility.CopySerialized(source, bee);
            bee.steerAction = default; bee.turnAction = default; bee.flapAction = default;
            bee.groundCollision = false; bee.enableKeyboardInput = bee.enableDirectXrInput = false;
            var rig = root.AddComponent<BeeCameraRig>(); rig.bee = bee; rig.playerRig = Obj("Test XR").transform;
            rig.trackedCamera = Obj("Test head").AddComponent<Camera>(); rig.trackedCamera.enabled = false;
            rig.trackedCamera.transform.SetParent(rig.playerRig, false);
            rig.trackedCamera.transform.localPosition = Vector3.up * 1.6f; bee.vrCamera = rig.trackedCamera;
            embodiment = root.AddComponent<BeeEmbodiment>(); EditorUtility.CopySerialized(sourceScale, embodiment);
            Call(embodiment, "Start");
            Check(bee.useWorldGravity && bee.enableRhythmFlight, "saved scene enables balanced stroke flight");
            Check(Mathf.Abs(bee.moveSpeed - source.moveSpeed * embodiment.Scale) < 0.0001f,
                "saved horizontal top speed still follows miniature scale");
            Check(bee.gravityAcceleration == source.gravityAcceleration && bee.terminalFallSpeed == source.terminalFallSpeed,
                "descent is not rescaled a second time by embodiment");
            float strength = swipe.assistLiftImpulse;
            void Reset(float velocity = 0f) { bee.Teleport(Vector3.zero, 0f); Set(bee, "_verticalVelocity", velocity); }
            float Step(float dt, bool button = false) {
                Call(bee, "StepStamina", dt, button, button);
                float v = (float)Call(bee, "StepRhythmVertical", dt, button && bee.IsFlapping);
                Set(bee, "_verticalVelocity", v); return v;
            }
            Vector3 Horizontal(Vector2 stick, float yaw, float dt) =>
                (Vector3)Call(bee, "StepGlideHorizontal", stick, Quaternion.Euler(0f, yaw, 0f), dt);
            float referenceDrop = 0f;
            foreach (int fps in new[] { 30, 72, 90, 120 }) {
                float dt = 1f / fps; Reset(); float drop = 0f, v = 0f;
                for (int i = 0; i < fps; i++) { v = Step(dt); drop -= v * dt; }
                Check(Mathf.Abs(v + .9f) < .001f && drop < .7f, "controlled one-second descent at " + fps + " FPS");
                if (fps == 30) referenceDrop = drop;
                else Check(Mathf.Abs(drop - referenceDrop) < .025f, "descent displacement agrees across frame rates");
                traces.Add(new { fps, second = 1, descentSpeed = -v, drop });

                Reset(-bee.terminalFallSpeed);
                Check(bee.TryQueueRhythmStroke(strength), "recovery stroke accepted");
                float initial = Get<float>(bee, "_verticalVelocity"), peakStep = 0f;
                Check(initial == -bee.terminalFallSpeed, "gesture does not instantly replace falling velocity");
                for (int i = 0; i < Mathf.CeilToInt(bee.strokeLiftDuration * fps); i++) {
                    float next = Step(dt); peakStep = Mathf.Max(peakStep, Mathf.Abs(next - v)); v = next;
                }
                float oneStroke = v;
                Check(oneStroke > -.3f && oneStroke < .15f, "one relaxed swipe arrests most descent");
                Check(bee.TryQueueRhythmStroke(strength), "second recovery stroke accepted");
                for (int i = 0; i < Mathf.CeilToInt(bee.strokeLiftDuration * fps); i++) v = Step(dt);
                Check(v > .3f, "second swipe restores climb");
                traces.Add(new { fps, oneStroke, twoStrokes = v, peakStep });

                Reset(-bee.terminalFallSpeed); bee.TryQueueRhythmStroke(strength); bee.TryQueueRhythmStroke(strength);
                bee.QueueRhythmPairBonus(swipe.dualHandBonusImpulse); v = -bee.terminalFallSpeed; peakStep = 0f;
                for (int i = 0; i < Mathf.CeilToInt(bee.strokeLiftDuration * fps); i++) {
                    float next = Step(dt); peakStep = Mathf.Max(peakStep, Mathf.Abs(next - v)); v = next;
                }
                Check(v > oneStroke && v <= bee.maxAscendSpeed && peakStep < .4f, "paired lift is stronger and spread over time");
                Check(!Get<BeeLiftEnvelope>(bee, "_strokeLift").Active, "pulse ends without stored burst");
                float end = v; v = Step(dt);
                Check(v < end && end - v < .06f, "release starts descent continuously");
                for (int i = 0; i < 4 * fps; i++) v = Step(dt);
                Check(Mathf.Abs(v + bee.terminalFallSpeed) < .001f, "release eventually reaches bounded descent");

                Reset(); float height = 0f; int accepted = 0, rejected = 0;
                int cadence = Mathf.RoundToInt(.6f * fps);
                for (int i = 0; i < 30 * fps; i++) {
                    if (i % cadence == 0) { if (bee.TryQueueRhythmStroke(strength)) accepted++; else rejected++; }
                    height += Step(dt) * dt;
                }
                Check(height > 12f && rejected == 0, "relaxed single-hand cadence reaches the canopy without exhaustion");
                traces.Add(new { fps, climbSeconds = 30, height, accepted, rejected, stamina = bee.StaminaNormalized * bee.maxStamina });
            }

            // Compare raw pulse budgets without gravity or speed clipping.
            Reset(); float savedGravity = bee.gravityAcceleration, savedDrag = bee.fallAirDrag, savedCap = bee.maxAscendSpeed;
            bee.gravityAcceleration = bee.fallAirDrag = 0f; bee.maxAscendSpeed = 20f;
            bee.TryQueueRhythmStroke(strength); bee.TryQueueRhythmStroke(strength);
            for (int i = 0; i < 90; i++) Step(1f / 90f);
            float unpaired = Get<float>(bee, "_verticalVelocity"); Reset();
            bee.TryQueueRhythmStroke(strength); bee.TryQueueRhythmStroke(strength); bee.QueueRhythmPairBonus(strength);
            for (int i = 0; i < 90; i++) Step(1f / 90f);
            Check(Mathf.Abs(Get<float>(bee, "_verticalVelocity") / unpaired - 1.25f) < .001f, "two-hand bonus is exactly 25 percent of two strokes");
            bee.gravityAcceleration = savedGravity; bee.fallAirDrag = savedDrag; bee.maxAscendSpeed = savedCap;

            Reset(); Vector3 forward = Vector3.zero;
            for (int i = 0; i < 180; i++) forward = Horizontal(Vector2.up, 0f, 1f / 90f);
            Check(Mathf.Abs(forward.z - bee.moveSpeed) < .001f, "authored forward speed is reachable");
            Vector3 coast = Horizontal(Vector2.zero, 90f, .1f);
            Check(Mathf.Abs(coast.x) < .0001f && coast.z > 0f, "looking sideways while coasting does not redirect momentum");
            Vector3 turn = Horizontal(Vector2.up, 90f, 1f / 90f);
            Check(turn.x > 0f && turn.z > 0f && (turn - coast).magnitude <= bee.moveAcceleration / 90f + .001f,
                "active joystick steers gradually");
            Set(bee, "worldHorizontalVelocity", forward);
            Vector3 brake = Horizontal(Vector2.down, 0f, .1f);
            Check(brake.z >= 0f && brake.z < forward.z, "reverse input brakes before travelling backward");
            for (int i = 0; i < 270; i++) brake = Horizontal(Vector2.down, 0f, 1f / 90f);
            Check(Mathf.Abs(brake.z + bee.reverseSpeed) < .001f, "authored reverse speed is reachable");
            for (int i = 0; i < 270; i++) turn = Horizontal(Vector2.right, 0f, 1f / 90f);
            Check(Mathf.Abs(turn.x - bee.strafeSpeed) < .001f, "authored strafe speed is reachable");
            for (int i = 0; i < 900; i++) coast = Horizontal(Vector2.zero, 180f, 1f / 90f);
            Check(coast.sqrMagnitude < .00001f, "glide drag eventually stops horizontal drift");

            Reset(); Set(bee, "_lastFlightVelocity", forward + Vector3.down * .9f);
            Call(bee, "StepPositionHold", true, bee.holdBrakeDuration);
            Check(Get<Vector3>(bee, "worldHorizontalVelocity").sqrMagnitude < .00001f, "hold removes momentum after braking");
            Call(bee, "StepPositionHold", false, .02f);
            Check(!bee.TryQueueRhythmStroke(0f), "empty gesture adds no lift");
            bee.SetFlowerPerched(true); bee.BeginPetalTakeoff();
            Check(!Get<BeeLiftEnvelope>(bee, "_strokeLift").Active, "takeoff cannot accumulate while still latched");
            Check(!bee.TryQueueRhythmStroke(strength), "petal owns movement while walking");
            bee.SetFlowerPerched(false); bee.BeginPetalTakeoff();
            Check(Get<float>(bee, "_verticalVelocity") == 0f, "petal release does not snap velocity");
            float takeoffHeight = 0f;
            for (int i = 0; i < 36; i++) takeoffHeight += Step(1f / 90f) / 90f;
            Check(takeoffHeight > .03f && takeoffHeight < .15f, "petal exit gets a small smooth lift");
            traces.Add(new { takeoffSeconds = .4f, takeoffHeight });
            bee.StopFlying();
            Check(!Get<BeeLiftEnvelope>(bee, "_strokeLift").Active && Get<Vector3>(bee, "worldHorizontalVelocity") == Vector3.zero,
                "stop clears lift and world momentum");
            bee.TryQueueRhythmStroke(strength); Set(bee, "worldHorizontalVelocity", forward); bee.Teleport(Vector3.one, 30f);
            Check(!Get<BeeLiftEnvelope>(bee, "_strokeLift").Active && Get<Vector3>(bee, "worldHorizontalVelocity") == Vector3.zero,
                "teleport cannot replay old momentum");
            bee.ExternalMovementLocked = true; Check(!bee.TryQueueRhythmStroke(strength), "external lock rejects strokes");
            bee.ExternalMovementLocked = false; Call(bee, "OnDisable");
            Check(Get<Vector3>(bee, "worldHorizontalVelocity") == Vector3.zero, "disable clears world momentum");
            Check(rig.trackedCamera.transform.localPosition == Vector3.up * 1.6f, "flight never rewrites tracked head position");
        } finally {
            if (embodiment != null) Call(embodiment, "OnDisable");
            EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(original);
        }
        Check(original.isDirty == dirty, "saved scene state preserved");
        System.IO.Directory.CreateDirectory("Library/FlightBalance");
        System.IO.File.WriteAllText("Library/FlightBalance/traces.json", Newtonsoft.Json.JsonConvert.SerializeObject(traces, Newtonsoft.Json.Formatting.Indented));
        return "Balanced flight: " + checks + " checks passed.";
    }
}
