using System;
using System.Reflection;
using LetItBee.HoneyPilot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BeePetalWalkingChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Private).Invoke(obj, args);
    static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Private).SetValue(obj, value);
    static object Get(object obj, string field) => obj.GetType().GetField(field, Private).GetValue(obj);

    [MenuItem("Let It Bee/Check Petal Walking And Gravity")]
    public static void Menu() => Debug.Log(Run());
    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        var original = SceneManager.GetActiveScene(); bool dirty = original.isDirty;
        var source = UnityEngine.Object.FindAnyObjectByType<BeeHoneyPilot>();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        int count = 0;
        var views = new System.Collections.Generic.List<object>();
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
        GameObject Obj(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go; }
        try {
            foreach (int flower in new[] { 0, 1 }) {
                var anchor = flower == 0 ? source.flowerAPerch : source.flowerBPerch;
                var sourceHead = flower == 0 ? source.flowerAHead : source.flowerBHead;
                var plant = UnityEngine.Object.Instantiate(anchor.parent.gameObject);
                SceneManager.MoveGameObjectToScene(plant, scene); plant.transform.position += new Vector3(3500f, 0f, 3500f);
                source.pilotFlowerIdle.animationClips[0].SampleAnimation(plant, 0f);
                var points = plant.GetComponentsInChildren<BeePerchPoint>();
                var group = Array.Find(points, p => p.name == anchor.name);
                var head = Array.Find(plant.GetComponentsInChildren<Transform>(), t => t.name == sourceHead.name);
                foreach (var point in group.landingPoints) Call(point, "Start");
                var surface = group.GetComponent<BeeFlowerSurface>();
                Check(surface != null && surface.Refresh(0f, true), "full animated flower collision generated");
                Physics.SyncTransforms();
                var root = Obj("Walking bee"); root.SetActive(false);
                var bee = root.AddComponent<BeeController>(); bee.bodyRadius = 0.0375f; bee.groundClearance = 0.0525f;
                var rig = root.AddComponent<BeeCameraRig>(); rig.bee = bee; rig.playerRig = Obj("Walking XR").transform;
                rig.playerRig.localScale = Vector3.one * 0.15f;
                rig.trackedCamera = Obj("Walking head").AddComponent<Camera>(); rig.trackedCamera.enabled = false;
                rig.trackedCamera.transform.SetParent(rig.playerRig, false); rig.trackedCamera.transform.localPosition = Vector3.up * 1.6f;
                var pilot = root.AddComponent<BeeHoneyPilot>(); pilot.beeRoot = root.transform;
                pilot.trackedCamera = rig.trackedCamera; pilot.rightHand = rig.trackedCamera.transform;
                pilot.flowerAHead = pilot.flowerBHead = head; pilot.flowerAPerch = pilot.flowerBPerch = group.transform;
                pilot.togglePetalLanding = pilot.enablePetalWalking = true; Set(pilot, "beeController", bee);
                Vector3 tracked = rig.trackedCamera.transform.localPosition;
                foreach (var point in group.landingPoints) {
                    Call(pilot, "LeaveFlower"); Set(pilot, "landingButtonDown", false);
                    root.transform.position = point.ViewPosition + Vector3.up * 0.12f; rig.SnapToBee();
                    root.transform.position += point.ViewPosition + Vector3.up * 0.12f - pilot.InteractionPosition; rig.SnapToBee();
                    Call(pilot, "StepFlowerPerching", 1f / 90f, true);
                    for (int i = 0; i < 1100; i++) { Call(pilot, "StepFlowerPerching", 1f / 90f, false); Call(rig, "UpdateRigPose"); }
                    Check(pilot.IsFlowerSettled && (bool)Get(pilot, "surfaceWalking"), "lands on continuous surface " + point.name);
                    var start = root.transform.position;
                    bool supported = true, reachable = true;
                    for (int i = 0; i < 350; i++) {
                        var direction = Vector3.ProjectOnPlane(head.position - root.transform.position, Vector3.up);
                        if (direction.magnitude < 0.025f) direction = Vector3.zero; else direction.Normalize();
                        Call(pilot, "StepSurfaceWalking", 1f / 90f, direction); Call(rig, "UpdateRigPose");
                        RaycastHit hit;
                        supported &= surface.Sample(root.transform.position, 0.4f, out hit) && root.transform.position.y >= hit.point.y + 0.03f;
                        reachable &= new BeeFlowerGeometry(head).Distance(pilot.InteractionPosition) <= pilot.contactRadius;
                    }
                    Check(supported, "no penetration along petal to flower centre " + point.name);
                    Check(Vector3.ProjectOnPlane(root.transform.position - head.position, Vector3.up).magnitude < 0.12f,
                        "walks from petal into flower centre " + point.name + " position=" + (root.transform.position-head.position));
                    Check(Vector3.Distance(start, root.transform.position) > 0.1f && reachable, "walking is visible and GAMA-reachable " + point.name);
                    views.Add(new { flower, point = point.name + " centre", view = BeeHoneyPilot.ToGama(pilot.InteractionPosition - new Vector3(3500f, 0f, 3500f)) });
                    var outward = Vector3.ProjectOnPlane(start - head.position, Vector3.up).normalized;
                    for (int i = 0; i < 700; i++) { Call(pilot, "StepSurfaceWalking", 1f / 90f, outward); Call(rig, "UpdateRigPose"); }
                    RaycastHit edge;
                    Check(pilot.IsFlowerSettled && bee.IsFlowerPerched && surface.Sample(root.transform.position, 0.4f, out edge),
                        "edge never drops the bee or releases the latch " + point.name);
                    Call(pilot, "StepSurfaceWalking", 0.1f, outward);
                    Check(surface.Sample(root.transform.position, 0.4f, out edge), "long frame does not tunnel over edge " + point.name);
                    views.Add(new { flower, point = point.name + " edge", view = BeeHoneyPilot.ToGama(pilot.InteractionPosition - new Vector3(3500f, 0f, 3500f)) });
                    Vector3 beforeSpring = pilot.InteractionPosition;
                    for (int i = 0; i < 60; i++) {
                        var loaded = (BeePerchPoint)Get(pilot, "selectedLandingPoint");
                        loaded.ApplyPressure(1f, 1f / 90f);
                        if (i % 6 == 0) surface.Refresh(0f, true);
                        Physics.SyncTransforms();
                        Call(pilot, "StepSurfaceWalking", 1f / 90f, Vector3.zero); Call(rig, "UpdateRigPose");
                    }
                    Check(surface.Sample(root.transform.position, 0.4f, out edge) && root.transform.position.y > edge.point.y,
                        "animated petal still supports the bee at the edge " + point.name);
                    Check(Vector3.Distance(pilot.InteractionPosition, beforeSpring) < 0.03f,
                        "spring does not shake the view after walking " + point.name);
                    foreach (var petal in group.landingPoints) petal.Restore();
                    surface.Refresh(0f, true); Physics.SyncTransforms();
                    Call(pilot, "StepFlowerPerching", 1f / 90f, true);
                    for (int i = 0; i < 90; i++) { Call(pilot, "StepFlowerPerching", 1f / 90f, false); Call(rig, "UpdateRigPose"); }
                    Check(pilot.PerchedFlower < 0 && !bee.IsFlowerPerched, "second trigger releases after walking " + point.name);
                }
                Check(rig.trackedCamera.transform.localPosition == tracked, "walking never writes tracked head pose");
                var centre = head.position + Vector3.up * 0.2f;
                root.transform.position = centre + Vector3.up;
                var collision = new BeeFlightCollision(); bool grounded, ceiling;
                Vector3 caught = collision.Move(root.transform, rig.playerRig, centre - Vector3.up * 2f,
                    bee.BodyRadius, bee.groundClearance, ~0, out grounded, out ceiling);
                RaycastHit flowerHit;
                Check(grounded && surface.Sample(caught, 0.4f, out flowerHit) && caught.y >= flowerHit.point.y + 0.03f,
                    "high-speed swept fall cannot tunnel through the flower");
                UnityEngine.Object.DestroyImmediate(rig.playerRig.gameObject);
                UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(plant);
            }
            var fallRoot = Obj("World gravity bee"); fallRoot.SetActive(false);
            var falling = fallRoot.AddComponent<BeeController>(); falling.useWorldGravity = true;
            falling.fallAirDrag = 0f; falling.gravityAcceleration = 9.81f;
            foreach (int fps in new[] { 30, 72, 90, 120 }) {
                float v = 0f;
                for (int frame = 0; frame < fps; frame++) v = (float)Call(falling, "StepWorldGravity", v, 1f / fps);
                Check(Mathf.Abs(v + 9.81f) < 0.0001f, "Earth gravity after one second at " + fps + " FPS");
            }
            falling.fallAirDrag = 0.12f; falling.gravityAcceleration = 1.4f;
            var miniature = fallRoot.AddComponent<BeeEmbodiment>(); miniature.playerScale = 0.15f;
            Set(falling, "_verticalVelocity", 0f);
            for (int i = 0; i < 90; i++) Set(falling, "_verticalVelocity", Call(falling, "StepRhythmVertical", 1f / 90f, false));
            Check(Mathf.Abs((float)Get(falling, "_verticalVelocity") + falling.terminalFallSpeed) < 0.001f,
                "balanced descent is capped independently of miniature scale");
            Set(falling, "_restGlideRemaining", 2f); Set(falling, "_verticalVelocity", -0.4f);
            float descent = (float)Call(falling, "StepRhythmVertical", 1f / 90f, false);
            Check(descent < -0.4f, "legacy glide hold does not suspend balanced descent");
            Set(falling, "_verticalVelocity", -0.9f);
            float recovery = (float)Call(falling, "StepRhythmVertical", 1f / 90f, true);
            Check(recovery > -0.9f && recovery < -0.85f, "flapping arrests fall progressively rather than snapping upward");
            Set(falling, "_verticalVelocity", -100f);
            Check((float)Call(falling, "StepRhythmVertical", 1f / 90f, false) == -falling.terminalFallSpeed,
                "world descent cap replaces miniature fall speed limit");
            var ground = Obj("Solid fall ground"); var box = ground.AddComponent<BoxCollider>(); box.size = new Vector3(20f, 0.2f, 20f);
            ground.transform.position = fallRoot.transform.position - Vector3.up;
            Physics.SyncTransforms(); fallRoot.transform.position += Vector3.up * 4f;
            var sweptFall = new BeeFlightCollision(); bool onGround, hitCeiling;
            var safe = sweptFall.Move(fallRoot.transform, null, fallRoot.transform.position - Vector3.up * 30f,
                0.0375f, 0.0525f, ~0, out onGround, out hitCeiling);
            Check(onGround && safe.y >= box.bounds.max.y + 0.05f, "fast gravity uses swept ground contact instead of tunnelling");
        } finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(original); }
        Check(original.isDirty == dirty, "saved scene state preserved");
        System.IO.Directory.CreateDirectory("Library/PetalWalk");
        System.IO.File.WriteAllText("Library/PetalWalk/gama-views.json", Newtonsoft.Json.JsonConvert.SerializeObject(views));
        return "Petal walking and descent: " + count + " checks passed.";
    }
}
