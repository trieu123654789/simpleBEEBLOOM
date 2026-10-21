using System;
using System.Collections.Generic;
using System.Reflection;
using LetItBee.HoneyPilot;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BeeFullHiveChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [MenuItem("Let It Bee/Check Full Hive")]
    public static void Menu() => Debug.Log(Run());
    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        var current = SceneManager.GetActiveScene(); bool dirty = current.isDirty;
        var preview = EditorSceneManager.NewPreviewScene();
        var checks = new List<string>();
        void Check(bool ok,string label) { if (!ok) throw new Exception(label); checks.Add(label); }
        GameObject Obj(string name) { var go=new GameObject(name); SceneManager.MoveGameObjectToScene(go,preview); return go; }
        var root=Obj("Isolated full hive"); root.SetActive(false);
        BeeHoneyPilot pilot=null;
        try {
            pilot=root.AddComponent<BeeHoneyPilot>(); pilot.beeRoot=root.transform;
            pilot.rightHand=Obj("Hand").transform;
            pilot.flowerAHead=Obj("A").transform; pilot.flowerBHead=Obj("B").transform;
            pilot.slot=Obj("Original authored target").transform;
            pilot.honeyMesh=Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            pilot.labelFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); pilot.enhancedFeedback=false;
            var hiveRoot=Obj("Hive_Pivot").transform;
            string[] names={"Cylinder.001","Cylinder.003","Cylinder.005","Cylinder.008","Cylinder.009","Cylinder.011","Cylinder.013","Cylinder.010"};
            var renderers=new List<Renderer>();
            for(int i=0;i<names.Length;i++) {
                var go=Obj(names[i]);go.transform.SetParent(hiveRoot,false);go.transform.localPosition=Vector3.right*i*0.6f;
                go.AddComponent<MeshFilter>().sharedMesh=pilot.honeyMesh;renderers.Add(go.AddComponent<MeshRenderer>());
            }
            pilot.slotDecoration=renderers[0];pilot.slotMount=renderers[0].transform;pilot.slotMountLocalPoint=Vector3.forward*.12f;
            pilot.sessionId=Guid.NewGuid().ToString("N");pilot.checkpointDirectoryOnServer="isolated";
            Transform original=pilot.slot; Vector3 savedPosition=original.position;
            object Call(string method,params object[] args)=>typeof(BeeHoneyPilot).GetMethod(method,Private).Invoke(pilot,args);
            void State(JObject state)=>typeof(BeeHoneyPilot).GetProperty("State").SetValue(pilot,state);
            Call("BeginHive"); Call("BuildViews");
            Check(pilot.HiveCapacity==7,"discover seven inserts, exclude shell");
            Check(pilot.slot!=original && original.position==savedPosition,"runtime target never moves authored target");
            Check(renderers[7].enabled && !renderers[0].enabled,"shell remains visible and unfilled inserts are hidden");
            var config=pilot.BuildConfiguration();
            Check(((JArray)config["hive_slots"]).Count==7 && (string)config["hive_slots"][0]["id"]=="slot-a","configuration preserves legacy first cell identity");
            var legacyState=new JObject { ["stored"]=1,["chunk_state"]="stored" };
            bool legacyRejected=false;
            try { Call("ValidateServerHive",config,legacyState); }
            catch(TargetInvocationException ex) { legacyRejected=ex.InnerException.Message.Contains("GAMA_MODEL_OUTDATED"); }
            Check(legacyRejected,"legacy one-cell server cannot advertise full-hive readiness");
            var compatibleState=new JObject { ["hive_capacity"]=7,["hive_slots"]=config["hive_slots"].DeepClone(),["filled_slots"]=new JArray("slot-a") };
            Call("ValidateServerHive",config,compatibleState);
            Check(((JArray)compatibleState["filled_slots"]).Count==1,"server compatibility check preserves existing deposit");
            Call("ValidateServerHive",new JObject(),legacyState);
            Check((int)legacyState["stored"]==1,"intentional single-cell configuration remains supported");
            var state=new JObject { ["stored"]=1,["hive_capacity"]=7,["filled_slots"]=new JArray("slot-a"),
                ["chunk_state"]="held",["chunk_pos"]=new JArray(0,0,0),["chunk_id"]="second",["nectar"]=new JArray(1,1),
                ["pollinated"]=new JArray(false,true),["pollen_source"]=1 };
            State(state); pilot.rightHand.position=renderers[1].transform.position+Vector3.forward*.12f;
            var piece=(Transform)typeof(BeeHoneyPilot).GetField("chunk",Private).GetValue(pilot);
            piece.position=pilot.rightHand.position;Call("UpdateSlotPose");
            Check(Vector3.Distance(pilot.slot.position,pilot.rightHand.position)<.001f,"nearest empty cell selected instead of occupied cell");
            Check(renderers[0].enabled && !renderers[1].enabled,"previous deposit remains visible while carrying next chunk");
            Check(!pilot.HiveFull && !pilot.RoundComplete,"one old deposit does not complete full hive");
            state["chunk_state"]="stored";state["stored"]=2;state["filled_slots"]=new JArray("slot-a","slot-Cylinder.003");state["deposited_slot"]="slot-Cylinder.003";
            Call("UpdateSlotPose");
            Check(renderers[0].enabled && !renderers[1].enabled,"active seated chunk is not rendered twice");
            state["chunk_state"]="ready";state["chunk_id"]="third";state["chunk_flower"]=0;state["chunk_pos"]=new JArray(0,0,0);
            Call("UpdateSlotPose");Call("UpdateViews");
            Check(renderers[0].enabled && renderers[1].enabled,"both completed cells remain filled after next nectar spawns");
            Check(Vector3.Distance(piece.position,pilot.flowerAHead.position)<.3f,"new chunk spawns at flower rather than lerping from hive");
            state["stored"]=7;
            Check(pilot.HiveFull && !pilot.RoundComplete,"pollination objective stays separate from filled hive");
            state["pollinated"]=new JArray(true,true);
            Check(pilot.RoundComplete,"full hive plus pollination completes round");
            Call("EndHive");
            Check(pilot.slot==original && original.position==savedPosition,"authored target restored on disable");
            Check(renderers.TrueForAll(r=>r.enabled),"all authored renderer states restored");
        } finally {
            if(pilot!=null) typeof(BeeHoneyPilot).GetMethod("OnDisable",Private).Invoke(pilot,null);
            EditorSceneManager.ClosePreviewScene(preview);
        }
        Check(current.isDirty==dirty,"saved scene dirty state preserved");
        return "Full hive checks passed: "+checks.Count;
    }
}
