using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Deterministic checks in an isolated preview scene. No headset or live scene edits required.
public static class BeeFlightV2Checks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int _checks;

    [MenuItem("Bee Bloom/Check Flight V2")]
    public static void RunMenu() => Debug.Log(Run());

    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before running flight checks.");
        _checks = 0;
        CheckEnvelope();
        CheckPairing();
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            BeeController bee = CreateBee(scene);
            CheckLiftAndGlide(bee);
            CheckHoldAndReset(bee);
            CheckContact(bee, scene);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
        return $"Flight V2: {_checks} checks passed (envelope, pairing, stamina, glide, brake, contact, reset).";
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Flight V2: " + message);
        _checks++;
    }

    private static void Near(float actual, float expected, float tolerance, string message)
        => Require(Mathf.Abs(actual - expected) <= tolerance, $"{message}: {actual} vs {expected}");

    private static object Call(object target, string name, params object[] args)
        => target.GetType().GetMethod(name, Private).Invoke(target, args);

    private static void Set(object target, string name, object value)
        => target.GetType().GetField(name, Private).SetValue(target, value);

    private static object Get(object target, string name)
        => target.GetType().GetField(name, Private).GetValue(target);

    private static void CheckEnvelope()
    {
        foreach (int fps in new[] { 30, 72, 90, 120 })
        {
            var envelope = new BeeLiftEnvelope();
            Require(envelope.Add(1.53f, 0.22f), "enqueue stroke");
            float total = 0f;
            for (int frame = 0; frame < fps; frame++) total += envelope.Step(1f / fps);
            Near(total, 1.53f, 0.0001f, "frame-rate independent total lift");
            Require(!envelope.Active, "pulse finishes");
        }
        var delayed = new BeeLiftEnvelope();
        delayed.Add(2f, 0.22f);
        Near(delayed.Step(1f), 2f, 0.0001f, "long frame consumes pulse once");
        Near(delayed.Step(1f), 0f, 0f, "completed pulse cannot repeat");
        delayed.Add(2f, 0.22f);
        Require(delayed.Step(1f / 90f) < 0.05f, "first frame is not an instant impulse");
        delayed.Clear();
        Near(delayed.Step(1f), 0f, 0f, "clear removes pending lift");
    }

    private static void CheckPairing()
    {
        var pair = new BeeStrokePairWindow();
        Require(!pair.Register(true, 1f, 0.18f), "single stroke is not a pair");
        Require(pair.Register(false, 1.17f, 0.18f), "near-simultaneous pair accepted");
        Require(!pair.Register(false, 1.18f, 0.18f), "paired event cannot be reused");
        pair.Clear();
        Require(!pair.Register(true, 2f, 0.18f), "first left stroke");
        Require(!pair.Register(true, 2.1f, 0.18f), "same hand cannot pair with itself");
        Require(!pair.Register(false, 2.3f, 0.18f), "late opposite stroke is not a pair");
        pair.Clear();
        Require(!pair.Register(false, 3f, 0.18f) && pair.Register(true, 3f, 0.18f), "same-frame pair");
    }

    private static BeeController CreateBee(Scene scene)
    {
        var root = new GameObject("FlightV2_TestBee");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.SetActive(false); // Prevent Awake / input actions touching the user's running rig.
        var bee = root.AddComponent<BeeController>();
        bee.groundCollision = false;
        bee.enableKeyboardInput = false;
        bee.enableDirectXrInput = bee.enableDirectXrMovement = bee.enableDirectXrFlap = false;
        var cameraObject = new GameObject("TestCamera");
        cameraObject.transform.SetParent(root.transform, false);
        bee.vrCamera = cameraObject.AddComponent<Camera>();
        bee.vrCamera.enabled = false;
        Reset(bee);
        return bee;
    }

    private static void Reset(BeeController bee)
    {
        bee.ExternalMovementLocked = false;
        bee.Teleport(new Vector3(0f, 5f, 0f), 0f);
    }

    private static void Step(BeeController bee, float dt, bool continuous = false)
    {
        float velocity = (float)Call(bee, "StepRhythmVertical", dt, continuous);
        Set(bee, "_verticalVelocity", velocity);
    }

    private static float SimulateStroke(BeeController bee, int fps, bool paired)
    {
        Reset(bee);
        Require(bee.TryQueueRhythmStroke(0.85f), "accepted stroke");
        if (paired)
        {
            Require(bee.TryQueueRhythmStroke(0.85f), "accepted opposite stroke");
            bee.QueueRhythmPairBonus(0.85f);
        }
        for (int i = 0; i < fps / 2; i++) Step(bee, 1f / fps);
        return bee.VerticalVelocity;
    }

    private static void CheckLiftAndGlide(BeeController bee)
    {
        float single = SimulateStroke(bee, 90, false);
        Require(single > 0f, "single stroke gives upward motion");
        float paired = SimulateStroke(bee, 90, true);
        Require(paired > single && paired <= bee.maxAscendSpeed, "pair lifts more without exceeding ascent limit");
        Near(SimulateStroke(bee, 30, false), single, 0.1f, "30 vs 90 fps flight result");
        Near(SimulateStroke(bee, 120, false), single, 0.1f, "120 vs 90 fps flight result");
        Require(bee.IsRestGliding, "stroke transitions into rest glide");
        for (int i = 0; i < 360; i++) Step(bee, 1f / 90f);
        Require(!bee.IsRestGliding && bee.VerticalVelocity < -1f, "glide expires and natural descent resumes");

        Reset(bee);
        float stamina = bee.StaminaNormalized;
        bee.TryQueueRhythmStroke(0.85f);
        Near(bee.StaminaNormalized, stamina - bee.strokeStaminaCost / bee.maxStamina, 0.0001f, "stamina paid once per stroke");
        float paid = bee.StaminaNormalized;
        bee.QueueRhythmPairBonus(0.85f);
        Near(bee.StaminaNormalized, paid, 0f, "bonus does not charge a third hand");
        Set(bee, "_stamina", 0f);
        Require(!bee.TryQueueRhythmStroke(0.85f), "empty stamina rejects new stroke");
        Reset(bee);
        bee.ExternalMovementLocked = true;
        Require(!bee.TryQueueRhythmStroke(0.85f), "movement lock rejects stroke");
        Reset(bee);
    }

    private static void CheckHoldAndReset(BeeController bee)
    {
        Reset(bee);
        bee.TryQueueRhythmStroke(0.85f);
        Set(bee, "_lastFlightVelocity", new Vector3(4f, -1f, 0f));
        Call(bee, "StepPositionHold", true, 0.1f);
        Require(bee.IsPositionBraking && bee.Speed > 0f && bee.Speed < 4f, "hold starts with braking, not abrupt stop");
        Require(!bee.TryQueueRhythmStroke(0.85f), "braking rejects stroke");
        Require(!((BeeLiftEnvelope)Get(bee, "_strokeLift")).Active, "hold clears pending lift");
        for (int i = 0; i < 3; i++) Call(bee, "StepPositionHold", true, 0.1f);
        Near(bee.transform.position.x, 4f * bee.holdBrakeDuration * 0.5f, 0.0001f, "braking distance");
        Require(bee.IsPositionHolding && !bee.IsPositionBraking, "brake ends in hold");
        Vector3 stopped = bee.transform.position;
        for (int i = 0; i < 180; i++) Call(bee, "StepPositionHold", true, 1f / 90f);
        Require(bee.transform.position == stopped, "hold bob never moves camera anchor");
        Call(bee, "StepPositionHold", false, 1f / 90f);
        Near(bee.Speed, 0f, 0f, "release does not restore old horizontal speed");
        Near(bee.VerticalVelocity, 0f, 0f, "release does not restore old falling speed");

        Reset(bee);
        Set(bee, "_lastFlightVelocity", Vector3.forward * 4f);
        Call(bee, "StepPositionHold", true, 0.1f);
        float residual = bee.Speed;
        Call(bee, "StepPositionHold", false, 0f);
        Near(bee.Speed, residual, 0f, "early release keeps only remaining brake momentum");
        Require(residual < 4f, "early release cannot restore entry momentum");
        bee.TryQueueRhythmStroke(0.85f);
        bee.Teleport(Vector3.up * 8f, 90f);
        Require(!bee.IsPositionHolding && !bee.IsRestGliding && !((BeeLiftEnvelope)Get(bee, "_strokeLift")).Active,
            "teleport clears flight transients");
        Reset(bee);
        bee.TryQueueRhythmStroke(0.85f);
        bee.ExternalMovementLocked = bee.ExternalSteeringLocked = true;
        bee.enableLeftTriggerHold = false;
        Call(bee, "Update");
        Require(!((BeeLiftEnvelope)Get(bee, "_strokeLift")).Active, "movement lock clears pending lift");
        bee.ExternalSteeringLocked = false;
        bee.enableLeftTriggerHold = true;
        Reset(bee);
        bee.TryQueueRhythmStroke(0.85f);
        Call(bee, "OnDisable");
        Require(!((BeeLiftEnvelope)Get(bee, "_strokeLift")).Active, "controller disable clears pending lift");
        Reset(bee);
    }

    private static void CheckContact(BeeController bee, Scene scene)
    {
        var swipe = bee.gameObject.AddComponent<BeeSwipeFlap>();
        swipe.bee = bee;
        swipe.trackedCamera = bee.vrCamera;
        swipe.xrOrigin = bee.gameObject.AddComponent<Unity.XR.CoreUtils.XROrigin>();
        swipe.autoSkinControllers = swipe.autoFixCeilingControllers = swipe.enableErgonomicHands = false;
        swipe.enableHaptics = swipe.debugLog = false;
        var left = new GameObject("TestLeft");
        var right = new GameObject("TestRight");
        SceneManager.MoveGameObjectToScene(left, scene);
        SceneManager.MoveGameObjectToScene(right, scene);
        swipe.leftControllerAnchor = left.transform;
        swipe.rightControllerAnchor = right.transform;
        Call(swipe, "TryBuild");
        Call(swipe, "FitCanvas");
        var canvas = (RectTransform)Get(swipe, "_canvasRect");
        // Ray origin has a built-in 1 cm vertical tip offset.
        left.transform.position = canvas.position + new Vector3(-0.15f, -0.01f, -0.5f);
        right.transform.position = canvas.position + new Vector3(0.15f, 0.2f, -0.5f);
        Call(swipe, "LateUpdate");
        Require(swipe.FlapCount == 1, "one hand contact produces one stroke");
        right.transform.position = canvas.position + new Vector3(0.15f, -0.01f, -0.5f);
        Call(swipe, "LateUpdate");
        Require(swipe.FlapCount == 2 && swipe.PairedFlapCount == 1, "second hand receives pair bonus");
        for (int i = 0; i < 20; i++) Call(swipe, "LateUpdate");
        Require(swipe.FlapCount == 2 && swipe.PairedFlapCount == 1, "stationary rays cannot farm strokes or pairs");
        Call(swipe, "OnDisable");
        Require(!((BeeLiftEnvelope)Get(bee, "_strokeLift")).Active, "disabling swipe clears pending lift");
    }
}
