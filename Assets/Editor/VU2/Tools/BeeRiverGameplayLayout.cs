#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using LetItBee.HoneyPilot;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BeeRiverGameplayLayout
{
    const string ScenePath = "Assets/Scenes/VU2/SCN_LetItBee_Gameplay.unity";
    static Collider ground;
    static float Ground(float x, float z)
    {
        if (!ground.Raycast(new Ray(new Vector3(x,80,z),Vector3.down),out var hit,160)) throw new Exception("No ground at " + x + "," + z);
        if (hit.point.y < 1.0f) throw new Exception("Flower placement would be underwater.");
        return hit.point.y;
    }
    static string Settings(Component c) => EditorJsonUtility.ToJson(c);
    [MenuItem("Let It Bee/Tools/Apply River Gameplay Layout")]
    public static void Menu() => Debug.Log(Apply());
    public static string Apply()
    {
        var current = SceneManager.GetActiveScene();
        if (Application.isPlaying || current.path != ScenePath || current.isDirty) throw new Exception("Save the current river scene in Edit Mode first.");
        try { return ApplySaved(); }
        catch { EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); throw; }
    }
    static string ApplySaved()
    {
        var scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath || scene.isDirty) throw new Exception("Use the saved current river scene in Edit Mode.");
        if (GameObject.Find("GAMEPLAY - River Route") != null) throw new Exception("Route already exists; edit its groups directly.");
        var pilot = UnityEngine.Object.FindFirstObjectByType<BeeHoneyPilot>();
        var environment = GameObject.Find("ENVIRONMENT - Nha Original");
        ground = environment.GetComponentsInChildren<Transform>().First(t=>t.name=="land2").GetComponentInChildren<Collider>();
        var environmentBefore = environment.GetComponentsInChildren<Transform>(true).Select(t=>Settings(t)).ToArray();
        var protectedComponents = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true))
            .Where(c=>c!=null && c!=pilot && !c.transform.IsChildOf(pilot.flowerAAnimator.transform) && !c.transform.IsChildOf(pilot.flowerBAnimator.transform)).ToArray();
        var protectedBefore = protectedComponents.Select(Settings).ToArray();
        string backup = "Library/FlowerLayoutBackup/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-scene.unity";
        Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.Copy(scene.path,backup);
        int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Place river flowers and hive");
        var route = new GameObject("GAMEPLAY - River Route"); Undo.RegisterCreatedObjectUndo(route,"River route");
        var points = new[] {new Vector2(145,224),new Vector2(148,226),new Vector2(144,228),
            new Vector2(119,207),new Vector2(121,209),new Vector2(117,211),
            new Vector2(149,195),new Vector2(152,197),new Vector2(151,201)};
        var source = pilot.flowerAAnimator.transform;
        string headPath = AnimationUtility.CalculateTransformPath(pilot.flowerAHead,source);
        string perchPath = AnimationUtility.CalculateTransformPath(pilot.flowerAPerch,source);
        var entries = new List<BeeHoneyPilot.FlowerReference>();
        var groups = new[] {"Flowers A - Hive Garden","Flowers B - Riverbank","Flowers C - Palm Clearing"};
        for (int g=0;g<3;g++) {
            var group = new GameObject(groups[g]); group.transform.SetParent(route.transform,false);
            for (int n=0;n<3;n++) {
                int i=g*3+n; var xy=points[i]; float y=Ground(xy.x,xy.y);
                var flower = UnityEngine.Object.Instantiate(source.gameObject,group.transform);
                flower.name="Daisy "+(i+1).ToString("00");
                flower.transform.position=new Vector3(xy.x,y+0.015f,xy.y);
                flower.transform.rotation=Quaternion.Euler(0,new[]{18f,133f,271f,74f,215f,318f,36f,153f,287f}[i],0);
                pilot.pilotFlowerIdle.animationClips[0].SampleAnimation(flower,0f);
                var entry=new BeeHoneyPilot.FlowerReference {head=flower.transform.Find(headPath),perch=flower.transform.Find(perchPath),animator=flower.GetComponent<Animator>(),id="flower-"+(i+1).ToString("00"),plantId="plant-"+(i+1).ToString("00")};
                if(entry.head==null||entry.perch==null)throw new Exception("Missing copied flower anchors");
                foreach(var identity in flower.GetComponentsInChildren<BeeWorldEntity>(true)) identity.RenewIdentity();
                entries.Add(entry);
            }
        }
        var oldA=pilot.flowerAAnimator.gameObject; var oldB=pilot.flowerBAnimator.gameObject;
        Undo.RecordObject(pilot,"Assign river flowers");
        pilot.flowerAHead=entries[0].head; pilot.flowerAPerch=entries[0].perch; pilot.flowerAAnimator=entries[0].animator;
        pilot.flowerBHead=entries[1].head; pilot.flowerBPerch=entries[1].perch; pilot.flowerBAnimator=entries[1].animator;
        pilot.additionalFlowers=entries.Skip(2).ToArray();
        oldA.SetActive(false); oldB.SetActive(false);
        var oldMount=pilot.slotMount; var oldHive=oldMount.parent;
        var hive=UnityEngine.Object.Instantiate(oldHive.gameObject,route.transform); hive.name="Hive - Seven Cells"; hive.transform.localScale=oldHive.lossyScale;
        pilot.slotMount=hive.transform.Find(AnimationUtility.CalculateTransformPath(oldMount,oldHive));
        pilot.slotDecoration=hive.transform.Find(AnimationUtility.CalculateTransformPath(pilot.slotDecoration.transform,oldHive)).GetComponent<Renderer>();
        pilot.hiveCells=Array.Empty<Renderer>();
        var shell=hive.GetComponentsInChildren<Renderer>().First(r=>r.name=="Cylinder.010");
        // Find the visible palm trunk from its baked mesh, not the imported root far below ground.
        var palm=environment.GetComponentsInChildren<Transform>().First(t=>t.name=="coconut_new");
        var skin=palm.GetComponentsInChildren<Renderer>().OrderByDescending(r=>r.bounds.size.y).First();
        Mesh baked;
        if(skin is SkinnedMeshRenderer skinned) { baked=new Mesh(); skinned.BakeMesh(baked); }
        else baked=UnityEngine.Object.Instantiate(skin.GetComponent<MeshFilter>().sharedMesh);
        var probe=new GameObject("Temporary trunk probe"); probe.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation); probe.transform.localScale=skin.transform.lossyScale;
        var collider=probe.AddComponent<MeshCollider>(); collider.sharedMesh=baked; Physics.SyncTransforms();
        Vector3 from=new Vector3(155,12,235), toward=new Vector3(134,12,214);
        Vector3 mounting; Vector3 normal;
        if(collider.Raycast(new Ray(from,(toward-from).normalized),out var trunkHit,60)) { mounting=trunkHit.point; normal=Vector3.ProjectOnPlane(trunkHit.normal,Vector3.up).normalized; }
        else { UnityEngine.Object.DestroyImmediate(probe); UnityEngine.Object.DestroyImmediate(baked); throw new Exception("Could not locate the palm trunk; no scene saved."); }
        UnityEngine.Object.DestroyImmediate(probe); UnityEngine.Object.DestroyImmediate(baked);
        using(var layout=new BeeHiveLayout(pilot.slotDecoration,pilot.slotMount,pilot.slotMountLocalPoint,pilot.slot,pilot.hiveCells)) {
            if(layout.Count!=7)throw new Exception("Hive must have seven cells");
            var face=layout.Rotation(0)*Vector3.forward;
            hive.transform.rotation=Quaternion.FromToRotation(face,normal)*hive.transform.rotation;
        }
        hive.transform.position+=mounting+normal*0.55f-shell.bounds.center;
        // Keep the original rig and imported preview art available, but remove duplicate gameplay art from view.
        oldHive.GetComponentInParent<Animator>().gameObject.SetActive(false);
        var imported=GameObject.Find("ENVIRONMENT - New Assets"); if(imported!=null) imported.SetActive(false);
        Undo.RecordObject(pilot.slot,"Move hive target"); pilot.slot.SetPositionAndRotation(pilot.slotMount.TransformPoint(pilot.slotMountLocalPoint),pilot.slotMount.rotation);
        Vector3 start=new Vector3(150,Ground(150,231)+1.1f,231);
        Vector3 delta=start-pilot.beeRoot.position;
        foreach(string name in new[]{"BeeRoot","XR Origin (XR Rig)","XR Interaction Manager","LetItBee_MainMenu"}) {var obj=GameObject.Find(name); if(obj!=null){Undo.RecordObject(obj.transform,"Move route start");obj.transform.position+=delta;}}
        // A changed resource layout gets its own checkpoint; prior sessions remain untouched.
        pilot.sessionId=Guid.NewGuid().ToString("N");
        EditorUtility.SetDirty(pilot);
        var after=environment.GetComponentsInChildren<Transform>(true).Select(Settings).ToArray();
        if(!environmentBefore.SequenceEqual(after))throw new Exception("Environment transforms changed");
        for(int i=0;i<protectedComponents.Length;i++) if(Settings(protectedComponents[i])!=protectedBefore[i])throw new Exception("Saved component changed: "+protectedComponents[i].name);
        Physics.SyncTransforms(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); Undo.CollapseUndoOperations(undo);
        var config=pilot.BuildConfiguration(); using(var layout=new BeeHiveLayout(pilot.slotDecoration,pilot.slotMount,pilot.slotMountLocalPoint,pilot.slot,pilot.hiveCells)){config["slot"]=layout.Configuration()[0];config["hive_slots"]=layout.Configuration();config["nectar_refill_seconds"]=pilot.nectarRefillSeconds;}
        config["world_entities"]=new BeeWorldSync().Capture(pilot);
        File.WriteAllText("Library/river-route-configuration.json",config.ToString());
        return "River route saved: 9 interactive daisies, 3 groups, 7 hive cells. Environment transforms and saved component settings unchanged. Backup: "+backup;
    }
}
#endif
