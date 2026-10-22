using System;
using System.Collections.Generic;
using System.Reflection;
using LetItBee.HoneyPilot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BeePetalLandingChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);

    [MenuItem("Let It Bee/Check Petal Landing")]
    public static void Menu() => Debug.Log(Run());
    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        var source = UnityEngine.Object.FindAnyObjectByType<BeeHoneyPilot>();
        var original = SceneManager.GetActiveScene(); bool dirty = original.isDirty;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        int count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
        GameObject Obj(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go; }
        try {
            foreach (int flower in new[] { 0, 1 }) {
                Transform sourceAnchor = flower == 0 ? source.flowerAPerch : source.flowerBPerch;
                Transform sourceHead = flower == 0 ? source.flowerAHead : source.flowerBHead;
                var plant = UnityEngine.Object.Instantiate(sourceAnchor.parent.gameObject);
                SceneManager.MoveGameObjectToScene(plant, scene); plant.transform.position += new Vector3(2500f, 0f, 2500f);
                source.pilotFlowerIdle.animationClips[0].SampleAnimation(plant, 0f);
                var points = plant.GetComponentsInChildren<BeePerchPoint>();
                var group = Array.Find(points, p => p.name == sourceAnchor.name);
                var head = Array.Find(plant.GetComponentsInChildren<Transform>(), t => t.name == sourceHead.name);
                Check(group.landingPoints.Length == 13, "all thirteen white petals have saved landing surfaces");
                foreach (var point in group.landingPoints) {
                    Call(point, "Start");
                    Check(point.branchVisual.name.StartsWith("Petal_") && point.landingSurface.enabled && !point.landingSurface.isTrigger,
                        "solid collider belongs to a white petal: " + point.name);
                }
                Physics.SyncTransforms();
                var root = Obj("Petal landing bee"); root.SetActive(false);
                var bee = root.AddComponent<BeeController>(); bee.bodyRadius = 0.0375f; bee.groundClearance = 0.0525f;
                var rig = root.AddComponent<BeeCameraRig>(); rig.bee = bee; rig.playerRig = Obj("Petal XR rig").transform;
                rig.playerRig.localScale = Vector3.one * 0.15f;
                rig.trackedCamera = Obj("Petal tracked camera").AddComponent<Camera>(); rig.trackedCamera.enabled = false;
                rig.trackedCamera.transform.SetParent(rig.playerRig, false); rig.trackedCamera.transform.localPosition = Vector3.up * 1.6f;
                rig.SnapToBee();
                var pilot = root.AddComponent<BeeHoneyPilot>(); pilot.beeRoot = root.transform;
                pilot.trackedCamera = rig.trackedCamera; pilot.rightHand = rig.trackedCamera.transform;
                pilot.flowerAHead = pilot.flowerBHead = head; pilot.flowerAPerch = pilot.flowerBPerch = group.transform;
                pilot.togglePetalLanding = true; Set(pilot, "beeController", bee);
                var desired = group.landingPoints[0];
                root.transform.position += desired.ViewPosition + Vector3.up * 0.12f - pilot.InteractionPosition; rig.SnapToBee();
                Call(pilot, "StepFlowerPerching", 1f / 90f, true);
                Check(pilot.PerchedFlower >= 0, "one press starts automatic petal landing");
                for (int i = 0; i < 1500; i++) {
                    Call(bee, "StepPositionHold", false, 1f / 90f);
                    Call(pilot, "StepFlowerPerching", 1f / 90f, false); Call(rig, "UpdateRigPose");
                }
                Check(pilot.IsFlowerSettled && bee.IsFlowerPerched, "releasing trigger keeps the bee securely landed without a network connection");
                var selected = (BeePerchPoint)typeof(BeeHoneyPilot).GetField("selectedLandingPoint", Private).GetValue(pilot);
                Vector3 eye = pilot.InteractionPosition; Vector3 tracked = rig.trackedCamera.transform.localPosition;
                for (int i = 0; i < 180; i++) {
                    selected.ApplyPressure(1f, 1f / 90f); Physics.SyncTransforms();
                    Call(pilot, "StepFlowerPerching", 1f / 90f, false); Call(rig, "UpdateRigPose");
                }
                Check(pilot.IsFlowerSettled && Vector3.Distance(root.transform.position, selected.BodyPosition(bee.BodyRadius, bee.groundClearance)) < 0.012f,
                    "loaded petal carries the bee without losing its landing latch");
                Check(Vector3.Distance(eye, pilot.InteractionPosition) < 0.008f && rig.trackedCamera.transform.localPosition == tracked,
                    "petal spring preserves the tracked head pose and a steady viewpoint");
                Check(new BeeFlowerGeometry(head).Distance(pilot.InteractionPosition) <= pilot.contactRadius,
                    "petal view remains inside the GAMA contact radius");
                Call(pilot, "StepFlowerPerching", 1f / 90f, true);
                for (int i = 0; i < 90; i++) { Call(pilot, "StepFlowerPerching", 1f / 90f, false); Call(rig, "UpdateRigPose"); }
                Check(pilot.PerchedFlower < 0 && !bee.IsFlowerPerched, "second press releases the petal and returns to flight");
                foreach (var point in group.landingPoints) point.Restore();
                UnityEngine.Object.DestroyImmediate(rig.playerRig.gameObject);
                UnityEngine.Object.DestroyImmediate(plant); UnityEngine.Object.DestroyImmediate(root);
            }
        } finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(original); }
        Check(original.isDirty == dirty, "user scene dirty state is preserved");
        return "Petal landing: " + count + " checks passed.";
    }
}
