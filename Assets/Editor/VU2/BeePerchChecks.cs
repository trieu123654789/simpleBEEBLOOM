using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BeeBloom.HoneyPilot;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BeePerchChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [MenuItem("Bee Bloom/Check Flower Landing")]
    public static void RunMenu() => Debug.Log(Run());
    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play before isolated landing checks.");
        var scene = EditorSceneManager.NewPreviewScene();
        Scene collisionScene = default;
        var originalScene = SceneManager.GetActiveScene();
        var checks = new List<string>();
        void Check(bool condition, string label) { if (!condition) throw new Exception(label); checks.Add(label); }
        try {
            var root = new GameObject("Landing check bee"); SceneManager.MoveGameObjectToScene(root, scene); root.SetActive(false);
            var bee = root.AddComponent<BeeController>(); bee.groundCollision = false; bee.enableRhythmFlight = false;
            // Empty InputActionProperty values generate new IDs when serialized. Use real actions
            // so the before/after comparison detects tuning changes rather than lazy IDs.
            bee.steerAction = new UnityEngine.InputSystem.InputActionProperty(new UnityEngine.InputSystem.InputAction("Move"));
            bee.turnAction = new UnityEngine.InputSystem.InputActionProperty(new UnityEngine.InputSystem.InputAction("Turn"));
            bee.flapAction = new UnityEngine.InputSystem.InputActionProperty(new UnityEngine.InputSystem.InputAction("Flap"));
            var cameraObject = new GameObject("Tracked camera"); cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0.1f, 0.08f, 0.16f);
            cameraObject.transform.localRotation = Quaternion.Euler(12f, 30f, 2f);
            var head = new GameObject("Flower head"); SceneManager.MoveGameObjectToScene(head, scene); head.transform.position = new Vector3(0f, 2f, 0f);
            var branch = new GameObject("Leaf_2_2_end"); branch.transform.SetParent(head.transform, false);
            branch.transform.localPosition = new Vector3(0f, -0.2f, -0.55f);
            var pilot = root.AddComponent<BeeHoneyPilot>(); pilot.beeRoot = root.transform;
            pilot.rightHand = cameraObject.transform;
            pilot.flowerAHead = pilot.flowerBHead = head.transform;
            typeof(BeeHoneyPilot).GetField("beeController", Private).SetValue(pilot, bee);
            typeof(BeeHoneyPilot).GetField("contact", Private).SetValue(pilot, 0);
            typeof(BeeHoneyPilot).GetField("contactDwell", Private).SetValue(pilot, 1f);
            var step = typeof(BeeHoneyPilot).GetMethod("StepFlowerPerching", Private);
            var holdStep = typeof(BeeController).GetMethod("StepPositionHold", Private);
            void Step(bool hold) => step.Invoke(pilot, new object[]{0.02f, hold});
            Check(pilot.TryGetPerchView(0, out Vector3 support, out Vector3 target), "leaf-tip perch resolves inside collection range");
            Check(support == branch.transform.position && Mathf.Abs(target.y - support.y - pilot.perchHeight) < 0.001f,
                "perch height is measured above the branch rather than the flower centre");
            Check(Vector3.ProjectOnPlane(target - head.transform.position, Vector3.up).magnitude >= 0.74f
                && Vector3.Distance(target, head.transform.position) < pilot.contactRadius * 0.95f,
                "view keeps flower standoff while remaining in GAMA contact range");
            bee.Teleport(target + Vector3.right * 0.2f, 0f);
            string tuning = EditorJsonUtility.ToJson(bee);
            Vector3 cameraLocal = cameraObject.transform.localPosition;
            Quaternion cameraRotation = cameraObject.transform.localRotation;
            Step(false); Check(pilot.PerchedFlower == -1, "proximity alone cannot land the player");
            Step(true); Check(pilot.PerchedFlower == 0 && bee.IsFlowerPerched, "deliberate hold near a flower begins landing");
            float largestStep = 0f;
            for (int i = 0; i < 80; i++) {
                Vector3 before = root.transform.position;
                holdStep.Invoke(bee, new object[]{true, 0.02f}); Step(true);
                largestStep = Mathf.Max(largestStep, Vector3.Distance(before, root.transform.position));
            }
            Check(largestStep <= pilot.perchSettleSpeed * 0.02f + 0.00001f, "landing movement stays within speed limit");
            Check(pilot.IsFlowerSettled && Vector3.Distance(root.transform.position, target) < 0.001f, "bee settles beside the flower over its branch");
            branch.transform.localPosition += Vector3.right * 0.03f;
            Step(true);
            Check(Vector3.Distance(root.transform.position, target) < 0.001f, "animated branch cannot drag the landed view");
            branch.transform.localPosition -= Vector3.right * 0.03f;
            Check(cameraObject.transform.localPosition == cameraLocal && Quaternion.Angle(cameraObject.transform.localRotation, cameraRotation) < 0.001f,
                "landing never writes tracked head pose");
            Check(!bee.TryQueueRhythmStroke(1f), "perched bee cannot accumulate a hidden lift stroke");
            var flapping = root.AddComponent<BeeFlapping>();
            typeof(BeeFlapping).GetField("_bee", Private).SetValue(flapping, bee);
            typeof(BeeFlapping).GetMethod("UpdateWingBeat", Private).Invoke(flapping, null);
            Check((float)typeof(BeeFlapping).GetField("_audibleWingFrequency", Private).GetValue(flapping) == 0f,
                "resting wings and wing audio share a stopped beat");
            Vector3 settledPosition = root.transform.position;
            Step(false); Check(pilot.PerchedFlower == -1 && !bee.IsFlowerPerched && root.transform.position == settledPosition,
                "release leaves the flower without a teleport or launch impulse");
            Check(EditorJsonUtility.ToJson(bee) == tuning, "saved flight tuning is unchanged after landing and release");
            bee.Teleport(target + Vector3.right * 3f, 0f);
            Step(true); Check(pilot.PerchedFlower == -1, "hold cannot pull the bee from outside landing range");
            Step(false); bee.Teleport(target + Vector3.right * 0.2f, 0f);
            bee.ExternalMovementLocked = true;
            Step(true); Check(pilot.PerchedFlower == -1 && bee.ExternalMovementLocked, "landing respects another movement lock");
            bee.ExternalMovementLocked = false;
            Step(true); Check(pilot.PerchedFlower == 0, "landing remains available after an external lock ends");
            bee.Teleport(target + Vector3.forward * 4f, 0f);
            Vector3 teleported = root.transform.position;
            Step(true); Check(pilot.PerchedFlower == -1 && root.transform.position == teleported,
                "teleport cancels the old perch without pulling the bee back");
            Step(false);
            pilot.trackedCamera = cameraObject.AddComponent<Camera>();
            Vector3 trackedOffset = new Vector3(0.1f, 1.5f, 0.16f);
            cameraObject.transform.localPosition = trackedOffset;
            bee.Teleport(target + Vector3.right * 0.15f - trackedOffset, 0f);
            Check(Vector3.Distance(root.transform.position, head.transform.position) > pilot.contactRadius
                && (int)typeof(BeeHoneyPilot).GetMethod("NearestContact", Private).Invoke(pilot, null) >= 0,
                "tracked view near a flower counts even when XR body origin is lower");
            Step(true);
            Check(pilot.PerchedFlower == 0, "tracked-view approach begins landing with an offset XR origin");
            for (int i = 0; i < 80; i++) { holdStep.Invoke(bee, new object[]{true, 0.02f}); Step(true); }
            Check(pilot.IsFlowerSettled && Vector3.Distance(pilot.InteractionPosition, target) < 0.001f,
                "landing brings the tracked viewpoint to the flower rest point");
            Check(cameraObject.transform.localPosition == trackedOffset
                && Quaternion.Angle(cameraObject.transform.localRotation, cameraRotation) < 0.001f,
                "offset-aware landing preserves tracked head local pose");
            Vector3 landedRoot = root.transform.position;
            cameraObject.transform.localPosition += Vector3.right * 0.05f;
            Check(root.transform.position == landedRoot && Mathf.Abs(pilot.InteractionPosition.x - target.x - 0.05f) < 0.001f,
                "physical head translation remains free after landing");
            typeof(BeeHoneyPilot).GetProperty("Live").SetValue(pilot, true);
            typeof(BeeHoneyPilot).GetMethod("Request", Private).Invoke(pilot, new object[]{"visit", 0});
            var request = (Newtonsoft.Json.Linq.JObject)typeof(BeeHoneyPilot).GetField("pendingRequest", Private).GetValue(pilot);
            Check(Vector3.Distance(BeeHoneyPilot.FromGama(request["bee"]), pilot.InteractionPosition) < 0.001f,
                "GAMA transactions use the same tracked interaction point as local contact");
            Step(false);
            // Preview scenes have separate physics. Exercise the runtime's
            // default Physics casts in a disposable additive scene instead.
            collisionScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "SM_Terrain_Island_01_PerchCheck";
            SceneManager.MoveGameObjectToScene(floor, collisionScene);
            floor.transform.position = new Vector3(20000f, -0.1f, 20000f);
            floor.transform.localScale = new Vector3(10f, 0.2f, 10f);
            head.transform.position = new Vector3(20000f, 1.2f, 20000f);
            pilot.TryGetPerchView(0, out support, out target);
            bee.groundCollision = true;
            bee.Teleport(target + Vector3.up * 0.2f - cameraObject.transform.localPosition, 0f);
            Physics.SyncTransforms();
            Step(true);
            for (int i = 0; i < 80; i++) { holdStep.Invoke(bee, new object[]{true, 0.02f}); Step(true); }
            Check(pilot.IsFlowerSettled && root.transform.position.y >= 0.349f
                && Vector3.Distance(pilot.InteractionPosition, head.transform.position) < pilot.contactRadius
                && Vector3.ProjectOnPlane(pilot.InteractionPosition-head.transform.position,Vector3.up).magnitude > 0.5f,
                "ground clearance raises a low flower perch without blocking nearby tracked-view collection: root=" + root.transform.position + ", perched=" + pilot.PerchedFlower);
            Step(false); bee.groundCollision = false;
            pilot.flowerAPerch = branch.transform;
            pilot.perchViewDistance = 3f;
            Check(pilot.TryGetPerchView(0, out support, out target) && support == branch.transform.position
                && Vector3.Distance(target, head.transform.position) <= pilot.contactRadius * 0.901f,
                "manual branch anchor and oversized viewing distance still respect server contact range");
            branch.transform.localPosition = Vector3.up * 3f;
            Check(!pilot.TryGetPerchView(0, out support, out target), "unreachable manual anchor is rejected without moving the camera");
            var result = new {passed=true,count=checks.Count,checks,finishedUtc=DateTime.UtcNow.ToString("O")};
            Directory.CreateDirectory("Library/HoneyPilot");
            File.WriteAllText("Library/HoneyPilot/flower-landing-validation.json", JsonConvert.SerializeObject(result,Formatting.Indented));
            return "Flower landing: " + checks.Count + " checks passed in isolated temporary scenes.";
        } finally {
            if (collisionScene.IsValid()) EditorSceneManager.CloseScene(collisionScene, true);
            if (originalScene.IsValid()) SceneManager.SetActiveScene(originalScene);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
