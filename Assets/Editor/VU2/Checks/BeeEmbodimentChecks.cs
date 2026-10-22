using System;
using System.Collections.Generic;
using System.Reflection;
using LetItBee.HoneyPilot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BeeEmbodimentChecks
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    [MenuItem("Let It Bee/Check Embodiment")]
    public static void Menu() => Debug.Log(Run());
    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        var current = SceneManager.GetActiveScene(); bool dirty = current.isDirty;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var checks = new List<string>();
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks.Add(label); }
        GameObject Obj(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go; }
        object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Private).Invoke(obj, args);
        BeeEmbodiment embodiment = null;
        try {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(floor, scene);
            floor.transform.position = new Vector3(22000, -0.1f, 22000); floor.transform.localScale = new Vector3(20, 0.2f, 20);
            var root = Obj("Embodiment bee"); root.SetActive(false); root.transform.position = new Vector3(22000, 1f, 22000);
            var bee = root.AddComponent<BeeController>(); bee.groundCollision = false;
            var rig = root.AddComponent<BeeCameraRig>(); rig.bee = bee;
            rig.playerRig = Obj("XR origin").transform; rig.trackedCamera = Obj("Tracked camera").AddComponent<Camera>();
            rig.trackedCamera.transform.SetParent(rig.playerRig, false); rig.trackedCamera.transform.localPosition = Vector3.up * 1.6f;
            Vector3 tracked = rig.trackedCamera.transform.localPosition;
            float speed = bee.moveSpeed, radius = bee.bodyRadius, ceiling = bee.maxFlightAltitude;
            embodiment = root.AddComponent<BeeEmbodiment>(); embodiment.playerScale = 0.15f;
            Call(embodiment, "Start"); Physics.SyncTransforms();
            Check(Mathf.Abs(rig.playerRig.localScale.x - 0.15f) < 0.001f, "runtime miniature scale applied to the tracked rig");
            Check(Mathf.Abs(bee.moveSpeed - speed * 0.15f) < 0.001f && bee.maxFlightAltitude == ceiling,
                "perceived movement scales while the authored hive remains reachable");
            embodiment.AlignTrackedFloor(tracked); rig.SnapToBee();
            Check(Mathf.Abs(rig.playerRig.position.y) < 0.005f, "tracked floor aligns with the measured virtual surface: rig=" + rig.playerRig.position
                + " bee=" + root.transform.position + " mask=" + bee.groundMask.value + " offset=" + rig.eyeOffset);
            Check(rig.trackedCamera.transform.localPosition == tracked, "floor alignment never writes the tracked head pose");
            Call(bee, "StepPositionHold", true, 0.8f);
            Check(Mathf.Abs(embodiment.HoverOffset) <= bee.holdBobAmplitude * embodiment.Scale * embodiment.hoverViewStrength + 0.00001f,
                "hover view displacement stays within the scaled comfort envelope");
            embodiment.reducedMotion = true; Check(embodiment.HoverOffset == 0f, "reduced motion removes artificial hover translation");
            Call(embodiment, "OnDisable");
            Check(bee.moveSpeed == speed && bee.bodyRadius == radius && rig.playerRig.localScale == Vector3.one,
                "authored movement and rig scale restored when embodiment stops");
            rig.SnapToBee();
            var head = Obj("Flower").transform; head.position = new Vector3(22000, 2, 22000);
            var branch = Obj("Authored branch").transform; branch.position = head.position + new Vector3(0, -0.2f, -0.65f);
            var point = branch.gameObject.AddComponent<BeePerchPoint>(); point.viewOffset = new Vector3(0, 0.3f, -0.15f);
            var pilot = root.AddComponent<BeeHoneyPilot>(); pilot.beeRoot = root.transform; pilot.rightHand = rig.trackedCamera.transform;
            pilot.trackedCamera = rig.trackedCamera; pilot.flowerAHead = pilot.flowerBHead = head; pilot.flowerAPerch = branch;
            typeof(BeeHoneyPilot).GetField("beeController", Private).SetValue(pilot, bee);
            typeof(BeeHoneyPilot).GetField("contact", Private).SetValue(pilot, 0);
            Vector3 target = point.ViewPosition;
            root.transform.position += target + Vector3.right * 0.3f - rig.trackedCamera.transform.position;
            rig.SnapToBee();
            Call(pilot, "StepFlowerPerching", 0.02f, true);
            Check(pilot.PerchedFlower == 0, "authored branch begins landing without hidden contact dwell");
            bool bounded = true;
            for (int i = 0; i < 220; i++) {
                Vector3 before = root.transform.position;
                Call(bee, "StepPositionHold", true, 0.02f); Call(pilot, "StepFlowerPerching", 0.02f, true); Call(rig, "UpdateRigPose");
                bounded &= Vector3.Distance(before, root.transform.position) <= pilot.perchApproachSpeed * 0.02f + 0.0021f;
            }
            Check(bounded, "approach remains bounded across the entire transition");
            Check(pilot.IsFlowerSettled, "authored branch approach settles");
            Vector3 settled = root.transform.position; branch.position += Vector3.right * 0.2f;
            Call(pilot, "StepFlowerPerching", 0.02f, true);
            Check(Vector3.Distance(root.transform.position, settled) < 0.001f, "moving branch never drags the tracked view");
            Call(pilot, "StepFlowerPerching", 0.02f, false);
            Check(bee.IsFlowerPerched, "release begins a transition instead of abruptly resuming gravity");
            for (int i = 0; i < 30; i++) Call(pilot, "StepFlowerPerching", 0.02f, false);
            Check(!bee.IsFlowerPerched && Vector3.Distance(root.transform.position, settled) <= 0.061f, "takeoff completes without a large position jump");
            Check(rig.trackedCamera.transform.localPosition == tracked, "landing and takeoff preserve physical head translation");
            var audio = Resources.Load<BeeInteractionAudio>("BeeAudio/InteractionAudio");
            Check(audio != null && audio.pollenPop != null && audio.nectarSip != null && audio.hiveDeposit != null,
                "interaction profile references imported audio assets");
            var feedback = new BeeHoneyFeedback(root.transform, rig.trackedCamera, head, head, branch);
            var shared = audio.hiveDeposit; feedback.Dispose();
            Check(shared != null && audio.hiveDeposit == shared, "disposing feedback cannot destroy a shared audio asset");
        } finally {
            if (embodiment != null) Call(embodiment, "OnDisable");
            EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(current);
        }
        Check(current.isDirty == dirty, "authored scene dirty state preserved");
        return "Embodiment checks passed: " + checks.Count;
    }
}
