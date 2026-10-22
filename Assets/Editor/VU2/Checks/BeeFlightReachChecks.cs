using System;
using System.Collections.Generic;
using System.Reflection;
using LetItBee.HoneyPilot;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Exercises the miniature rig, rendered bar, actual ray contact and branch body pose together.
public static class BeeFlightReachChecks
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);

    [MenuItem("Let It Bee/Check Flight Reach and Branch Landing")]
    public static void Menu() => Debug.Log(Run());
    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        var original = SceneManager.GetActiveScene(); bool dirty = original.isDirty;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var checks = new List<string>();
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks.Add(label); }
        GameObject Obj(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go; }
        BeeEmbodiment embodiment = null;
        try {
            var root = Obj("Flight reach bee"); root.SetActive(false); root.transform.position = new Vector3(24000f, 1f, 24000f);
            var bee = root.AddComponent<BeeController>(); bee.groundCollision = false;
            bee.staminaDrainPerSecond = 22f; bee.staminaRegenPerSecond = 14f;
            bee.enableKeyboardInput = bee.enableDirectXrInput = bee.enableDirectXrFlap = bee.enableDirectXrMovement = false;
            var rig = root.AddComponent<BeeCameraRig>(); rig.bee = bee;
            rig.playerRig = Obj("Flight reach XR").transform;
            rig.trackedCamera = Obj("Flight reach camera").AddComponent<Camera>(); rig.trackedCamera.enabled = false;
            rig.trackedCamera.transform.SetParent(rig.playerRig, false);
            rig.trackedCamera.transform.localPosition = Vector3.up * 1.6f;
            bee.vrCamera = rig.trackedCamera;
            embodiment = root.AddComponent<BeeEmbodiment>(); embodiment.playerScale = 0.15f;
            Call(embodiment, "Start"); rig.SnapToBee();
            float ClimbUntilExhausted(float seconds) {
                bee.Teleport(new Vector3(24000f, 0.4f, 24000f), 0f);
                float top = root.transform.position.y;
                for (int i = 0; i < seconds * 90f; i++) {
                    Call(bee, "StepStamina", 1f / 90f, true, true);
                    float v = (float)Call(bee, "StepRhythmVertical", 1f / 90f, bee.IsFlapping);
                    Set(bee, "_verticalVelocity", v);
                    root.transform.position += Vector3.up * (v / 90f);
                    top = Mathf.Max(top, root.transform.position.y);
                    if (bee.IsExhausted) break;
                }
                return top;
            }
            float previousTop = ClimbUntilExhausted(15f);
            Check(previousTop < 3.5f && bee.IsExhausted, "reproduces continuous-button exhaustion below the hive at old miniature speed");
            Call(embodiment, "OnDisable");
            bee.maxAscendSpeed = 6f; bee.maxFlightAltitude = 25f; embodiment.continuousFlightEnduranceMultiplier = 3f;
            float savedDrain = bee.staminaDrainPerSecond;
            Call(embodiment, "Start"); rig.SnapToBee();
            float newTop = ClimbUntilExhausted(12f);
            Check(newTop > 10f && !bee.IsExhausted, "continuous climb reaches above the coconut canopy with stamina remaining");
            Check(Mathf.Abs(bee.staminaDrainPerSecond - savedDrain / 3f) < 0.001f, "continuous endurance leaves gesture stroke cost separate");
            Check(bee.strokeStaminaCost == 3f, "one accepted swipe keeps its existing energy cost");
            bee.Teleport(new Vector3(24000f, 1f, 24000f), 0f); rig.SnapToBee();

            var swipe = root.AddComponent<BeeSwipeFlap>(); swipe.bee = bee; swipe.trackedCamera = rig.trackedCamera;
            swipe.xrOrigin = rig.playerRig.gameObject.AddComponent<Unity.XR.CoreUtils.XROrigin>();
            swipe.xrOrigin.Camera = rig.trackedCamera;
            swipe.autoSkinControllers = false; swipe.showRays = false; swipe.enableHaptics = false; swipe.debugLog = false;
            swipe.barWidth = 0.52f; swipe.barThickness = 0.012f; swipe.barDepth = 0.55f;
            swipe.barCenterOffset = new Vector2(0f, -0.16f);
            swipe.leftControllerAnchor = Obj("Test left hand").transform;
            swipe.rightControllerAnchor = Obj("Test right hand").transform;
            swipe.leftControllerAnchor.SetParent(rig.playerRig, false);
            swipe.rightControllerAnchor.SetParent(rig.playerRig, false);
            Obj("RayTip").transform.SetParent(swipe.leftControllerAnchor, false);
            Obj("RayTip").transform.SetParent(swipe.rightControllerAnchor, false);
            Call(swipe, "TryBuild"); Call(swipe, "FitCanvas");
            var canvas = (RectTransform)Get(swipe, "_canvasRect");
            Check(Mathf.Abs(canvas.localPosition.z - 0.75f) < 0.001f, "bar stays in front of the hands in camera-local metres");
            Check(Mathf.Abs(canvas.lossyScale.x - 0.001f * embodiment.Scale) < 0.000001f, "rendered bar inherits miniature scale exactly once");
            object left = Get(swipe, "_left"), right = Get(swipe, "_right");
            bool Touching(object hand) => (bool)hand.GetType().GetField("touching").GetValue(hand);
            void Aim(Transform hand, float x, float y) {
                hand.position = rig.trackedCamera.transform.TransformPoint(new Vector3(x < 0f ? -0.2f : 0.2f, -0.2f, 0.35f));
                Vector3 target = canvas.TransformPoint(new Vector3(x * 1000f, y * 1000f, 0f));
                hand.rotation = Quaternion.LookRotation(target - hand.position);
            }
            foreach (float scale in new[] { 1f, 0.15f }) {
                rig.playerRig.localScale = Vector3.one * scale;
                Call(swipe, "FitCanvas");
                Aim(swipe.leftControllerAnchor, -0.16f, 0f); Aim(swipe.rightControllerAnchor, 0.16f, 0.12f);
                Call(swipe, "UpdateHand", left, true); Call(swipe, "UpdateHand", right, false);
                Check(Touching(left) && !Touching(right), "left ray hits the visible bar independently at rig scale " + scale);
                Aim(swipe.rightControllerAnchor, 0.16f, 0f); Call(swipe, "UpdateHand", right, false);
                Check(Touching(left) && Touching(right), "both rays can touch simultaneously at rig scale " + scale);
                Aim(swipe.leftControllerAnchor, -0.16f, 0.12f); Call(swipe, "UpdateHand", left, true);
                Check(!Touching(left) && Touching(right), "leaving with one hand preserves the other hand contact at rig scale " + scale);
            }
            Check(swipe.PairedFlapCount >= 1, "accepted opposite-hand contacts produce the paired lift bonus");
            rig.playerRig.localScale = Vector3.one * embodiment.Scale;
            bee.Teleport(new Vector3(24000f, 1f, 24000f), 0f); rig.SnapToBee();

            var head = Obj("Branch test flower").transform; head.position = new Vector3(24000f, 2f, 24000f);
            var branch = Obj("Branch top").transform; branch.position = head.position + new Vector3(0f, -0.2f, -0.65f);
            var point = branch.gameObject.AddComponent<BeePerchPoint>(); point.standOnBranch = true; point.viewOffset = new Vector3(0f, 0.3f, -0.15f);
            var stem = Obj("Bending branch").transform; stem.position = branch.position + new Vector3(0f, -0.012f, -0.2f);
            var tip = Obj("Branch tip").transform; tip.SetParent(stem, false); tip.localPosition = new Vector3(0f, 0f, 0.27f);
            point.branchVisual = stem;
            Call(point, "Start"); Physics.SyncTransforms(); bee.groundCollision = true;
            var shaft = branch.GetComponentInChildren<CapsuleCollider>();
            Check(shaft != null && !shaft.isTrigger && shaft.height > 0.27f, "branch segment has a solid collider along its length");
            Check(shaft.Raycast(new Ray(stem.position + Vector3.forward * 0.08f + Vector3.up * 0.2f, Vector3.down), out _, 0.4f),
                "branch collision works outside the landing contact patch");
            var pilot = root.AddComponent<BeeHoneyPilot>(); pilot.beeRoot = root.transform;
            pilot.trackedCamera = rig.trackedCamera; pilot.rightHand = swipe.rightControllerAnchor;
            pilot.flowerAHead = pilot.flowerBHead = head; pilot.flowerAPerch = branch;
            Set(pilot, "beeController", bee); Set(pilot, "contact", 0);
            root.transform.position += point.ViewPosition + Vector3.right * 0.3f - pilot.InteractionPosition;
            Call(rig, "UpdateRigPose"); Vector3 tracked = rig.trackedCamera.transform.localPosition;
            Call(pilot, "StepFlowerPerching", 0.02f, true);
            Check(pilot.PerchedFlower == 0, "branch body landing begins with a deliberate left hold");
            bool bounded = true;
            for (int i = 0; i < 400; i++) {
                Vector3 before = pilot.InteractionPosition;
                Call(bee, "StepPositionHold", true, 0.02f); Call(pilot, "StepFlowerPerching", 0.02f, true); Call(rig, "UpdateRigPose");
                bounded &= Vector3.Distance(before, pilot.InteractionPosition) <= Mathf.Max(pilot.perchSettleSpeed, pilot.perchApproachSpeed) * 0.02f + 0.003f;
            }
            Check(bounded && pilot.IsFlowerSettled, "body and viewpoint settle smoothly within the shared approach speed");
            Check(Vector3.ProjectOnPlane(root.transform.position - branch.position, Vector3.up).magnitude < 0.005f
                && Mathf.Abs(root.transform.position.y - branch.position.y - Mathf.Max(bee.BodyRadius, bee.groundClearance) - 0.006f) < 0.008f,
                "body rests on top of the actual branch support rather than at the viewing offset");
            Check(Vector3.Distance(pilot.InteractionPosition, point.ViewPosition) < 0.01f, "separate viewpoint reaches the authored flower viewing position");
            Check(new BeeFlowerGeometry(head).Distance(pilot.InteractionPosition) < pilot.contactRadius * 0.95f, "landed player remains in the server collection radius");
            Vector3 stillView = pilot.InteractionPosition, unloadedSupport = point.SupportPosition;
            float lowest = unloadedSupport.y;
            for (int i = 0; i < 180; i++) {
                point.ApplyPressure(1f, 1f / 90f); Physics.SyncTransforms();
                Call(pilot, "StepFlowerPerching", 1f / 90f, true); Call(rig, "UpdateRigPose");
                lowest = Mathf.Min(lowest, point.SupportPosition.y);
            }
            Check(point.SupportPosition.y < unloadedSupport.y - 0.003f && lowest < point.SupportPosition.y - 0.0002f,
                "branch dips under weight then rebounds into a loaded resting pose");
            Check(Vector3.ProjectOnPlane(root.transform.position - point.SupportPosition, Vector3.up).magnitude < 0.005f
                && Mathf.Abs(root.transform.position.y - point.SupportPosition.y - Mathf.Max(bee.BodyRadius, bee.groundClearance) - 0.006f) < 0.008f,
                "bee body stays on the moving branch surface");
            Check(Vector3.Distance(stillView, pilot.InteractionPosition) < 0.005f && pilot.IsFlowerSettled,
                "branch bounce keeps the VR view stable and does not interrupt collection readiness");
            for (int i = 0; i < 180; i++) {
                point.ApplyPressure(0f, 1f / 90f); Physics.SyncTransforms();
                Call(pilot, "StepFlowerPerching", 1f / 90f, true); Call(rig, "UpdateRigPose");
            }
            Check(Vector3.Distance(point.SupportPosition, unloadedSupport) < 0.001f, "unloaded branch springs back to its original position");
            point.Restore(); Physics.SyncTransforms();
            Call(pilot, "StepFlowerPerching", 0.02f, true); Call(rig, "UpdateRigPose");
            Vector3 restView = pilot.InteractionPosition;
            Call(pilot, "StepFlowerPerching", 0.02f, false);
            for (int i = 0; i < 30; i++) { Call(pilot, "StepFlowerPerching", 0.02f, false); Call(rig, "UpdateRigPose"); }
            Check(!bee.IsFlowerPerched && rig.PerchViewOffset == Vector3.zero, "takeoff returns camera ownership to normal flight");
            Check(Vector3.Distance(restView, pilot.InteractionPosition) < 0.025f, "viewpoint does not drop when the bee leaves its branch: "
                + restView.ToString("F4") + " -> " + pilot.InteractionPosition.ToString("F4"));
            Check(rig.trackedCamera.transform.localPosition == tracked, "physical head tracking remains untouched throughout landing and release");

            // Authored sloped branches must be tested separately from the level runtime fallback.
            var slope = Obj("Authored sloped perch").AddComponent<BeePerchPoint>();
            slope.transform.position = new Vector3(24100f, 2f, 24100f); slope.standOnBranch = true;
            var slopeBone = Obj("Sloped branch bone").transform;
            slopeBone.position = slope.transform.position + new Vector3(0f, -0.05f, -0.1f);
            var slopeTip = Obj("Sloped branch tip").transform; slopeTip.SetParent(slopeBone, false);
            slopeTip.localPosition = new Vector3(0f, 0.1f, 0.2f); slope.branchVisual = slopeBone;
            var patch = Obj("Saved landing collider"); patch.transform.SetParent(slope.transform, false);
            patch.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            var savedBox = patch.AddComponent<BoxCollider>(); savedBox.size = new Vector3(0.035f, 0.008f, 0.035f);
            savedBox.center = Vector3.down * 0.004f; slope.landingSurface = savedBox;
            var savedShaft = Obj("Saved branch collider").AddComponent<CapsuleCollider>();
            savedShaft.transform.SetParent(patch.transform, false);
            savedShaft.transform.position = slopeBone.position + new Vector3(0.03f, 0.02f, 0f);
            savedShaft.transform.rotation = Quaternion.Euler(65f, 20f, 0f);
            savedShaft.radius = 0.0045f; savedShaft.height = 0.2f; slope.branchSurface = savedShaft;
            Vector3 shaftPosition = savedShaft.transform.position; Quaternion shaftRotation = savedShaft.transform.rotation;
            Call(slope, "Start"); Call(slope, "Start"); Physics.SyncTransforms();
            Check(slope.GetComponentsInChildren<Collider>().Length == 2, "saved branch colliders are reused without duplicates");
            Check(Vector3.Distance(savedShaft.transform.position, shaftPosition) < 0.001f
                && Quaternion.Angle(savedShaft.transform.rotation, shaftRotation) < 0.01f,
                "starting a perch preserves the collider pose authored in the Inspector");
            var probe = Obj("Sloped landing collision probe").transform;
            Vector3 slopeTarget = slope.BodyPosition(0.03f, 0.03f); probe.position = slopeTarget + Vector3.up * 0.1f;
            Vector3 resolved = new BeeFlightCollision().Move(probe, null, slopeTarget, 0.03f, 0.03f,
                Physics.DefaultRaycastLayers, out _, out _);
            Check(Vector3.Distance(resolved, slopeTarget) < 0.002f, "body reaches the sloped surface without collision blocking its landing target");
            Check(Mathf.Abs(Vector3.Dot(slopeTarget - slope.SupportPosition, savedBox.transform.up) - 0.036f) < 0.002f,
                "body clearance follows the surface normal rather than world up");
            Vector3 shaftLocal = slopeBone.InverseTransformPoint(savedShaft.transform.position);
            for (int i = 0; i < 180; i++) slope.ApplyPressure(1f, 1f / 90f);
            Check(Vector3.Distance(savedShaft.transform.position, slopeBone.TransformPoint(shaftLocal)) < 0.001f,
                "saved branch collider follows the branch bend at its authored offset");
            Check(savedBox.size == new Vector3(0.035f, 0.008f, 0.035f) && savedShaft.radius == 0.0045f && savedShaft.height == 0.2f,
                "branch deformation preserves saved collider dimensions");
            slope.Restore(); UnityEngine.Object.DestroyImmediate(slope);
            Check(savedBox != null && savedShaft != null && savedBox.gameObject.activeSelf,
                "removing the perch script in Edit Mode does not delete saved collider components");
            Call(embodiment, "OnDisable");
            Check(bee.staminaDrainPerSecond == savedDrain, "stopping runtime embodiment restores the saved stamina tuning");
            System.IO.Directory.CreateDirectory("Library/FlightReach");
            System.IO.File.WriteAllText("Library/FlightReach/checks.json", JsonConvert.SerializeObject(new { checks, previousTop, newTop }, Formatting.Indented));
        } finally {
            if (embodiment != null) Call(embodiment, "OnDisable");
            EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(original);
        }
        Check(original.isDirty == dirty, "authored scene dirty state preserved");
        return "Flight reach and branch landing: " + checks.Count + " checks passed.";
    }
}
