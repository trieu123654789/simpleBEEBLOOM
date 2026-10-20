using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LetItBee.HoneyPilot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BeeWorldSyncChecks
{
    [MenuItem("Let It Bee/World Sync/Check world sync")]
    public static void Menu() => Debug.Log(Run());
    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play before isolated world checks.");
        var saved = SceneManager.GetActiveScene(); bool dirty = saved.isDirty;
        var source = UnityEngine.Object.FindFirstObjectByType<BeeHoneyPilot>();
        string sourceJson = source != null ? EditorJsonUtility.ToJson(source) : "";
        var scene = EditorSceneManager.NewPreviewScene(); var checks = new List<string>();
        void Check(bool condition,string name) { if (!condition) throw new Exception(name); checks.Add(name); }
        GameObject Obj(string name) { var go=new GameObject(name); SceneManager.MoveGameObjectToScene(go,scene);return go; }
        try {
            var owner=Obj("Sync fixture"); owner.SetActive(false);
            var pilot=owner.AddComponent<BeeHoneyPilot>(); pilot.enabled=false;
            pilot.beeRoot=Obj("Bee reference").transform;
            pilot.flowerAHead=Obj("Flower A").transform;pilot.flowerBHead=Obj("Flower B").transform;
            pilot.slot=Obj("Slot").transform;pilot.slotMount=pilot.slot;
            pilot.sessionId=Guid.NewGuid().ToString("N");pilot.checkpointDirectoryOnServer="unused";
            var sync=new BeeWorldSync();
            Check(sync.Capture(pilot).Count==4,"existing player, two flowers and hive register without changing scene components");
            var a=Obj("Additional bee").AddComponent<BeeWorldEntity>();a.kind=BeeWorldEntity.EntityKind.Bee;
            var b=Obj("Additional flower").AddComponent<BeeWorldEntity>();
            a.transform.position=new Vector3(-4,2,9); b.transform.position=new Vector3(6,3,-2);
            var entities=sync.Capture(pilot);
            Check(entities.Count==6 && entities.Count(x=>(string)x["kind"]=="bee")==2,"active opted-in objects automatically extend counts");
            Check(JToken.DeepEquals(entities.First(x=>(string)x["id"]==a.Id)["position"],new JArray(-4d,9d,2d)),"additional objects use the existing signed XZY metre mapping");
            string id=a.Id; a.enabled=false;
            Check(sync.Capture(pilot).Count==5,"disabled component removes its observation");
            a.enabled=true;
            Check(a.Id==id && sync.Capture(pilot).Count==6,"re-enable retains identity without duplication");
            var clone=UnityEngine.Object.Instantiate(a.gameObject); clone.name="Duplicated bee";
            SceneManager.MoveGameObjectToScene(clone,scene);
            Check(clone.GetComponent<BeeWorldEntity>().Id!=a.Id,"instantiating an opted-in object gives the copy a distinct identity");
            var anchor=Obj("Flower head").transform;anchor.position=new Vector3(2,4,-8);b.anchor=anchor;
            Check(JToken.DeepEquals(sync.Capture(pilot).First(x=>(string)x["id"]==b.Id)["position"],new JArray(2d,-8d,4d)),"optional bone anchor drives observation position");
            var marked=pilot.flowerAHead.gameObject.AddComponent<BeeWorldEntity>();
            Check(sync.Capture(pilot).Count==7,"marking an existing pilot flower cannot register it twice");
            var payload=sync.Prepare(sync.Capture(pilot));
            var retry=sync.Prepare(new JArray());
            Check(JToken.DeepEquals(payload,retry),"unacknowledged retries retain exact sequence and content");
            bool rejected=false;
            try { sync.Accept(new JObject{["accepted"]=true,["source"]="foreign",["sequence"]=sync.Sequence}); }catch(InvalidOperationException){rejected=true;}
            Check(rejected,"foreign or stale acknowledgements cannot discard pending changes");
            sync.Accept(new JObject{["accepted"]=true,["source"]=sync.Source,["sequence"]=sync.Sequence});
            Check(sync.Prepare(sync.Capture(pilot))==null,"unchanged world does not resend a full snapshot");
            b.transform.position+=Vector3.one; // anchor still wins
            Check(sync.Prepare(sync.Capture(pilot))==null,"moving an unused parent does not send a false anchor movement");
            anchor.position+=Vector3.up;
            var moved=sync.Prepare(sync.Capture(pilot));
            Check((long)moved["sequence"]==2,"tracked movement produces a new revision");
            sync.Reconnect();
            Check(sync.Prepare(sync.Capture(pilot))!=null && sync.Source==(string)payload["source"],"reconnect publishes a full world with the same stream identity");
            var fresh=new BeeWorldSync();var duplicate=new JArray(payload["entities"].First.DeepClone(),payload["entities"].First.DeepClone());
            rejected=false;try{fresh.Prepare(duplicate);}catch(InvalidOperationException){rejected=true;}
            Check(rejected,"ambiguous IDs are rejected before transport");
            UnityEngine.Object.DestroyImmediate(b.gameObject);
            Check(sync.Capture(pilot).Count==6,"destroying an opted-in object removes it from the next full snapshot");
            Check(source==null || EditorJsonUtility.ToJson(source)==sourceJson,"saved pilot fields remain unchanged");
            Check(saved.isDirty==dirty,"isolated checks preserve source scene dirty state");
            string folder="Library/HoneyPilot/WorldSync";Directory.CreateDirectory(folder);
            File.WriteAllText(folder+"/unity-validation.json",JsonConvert.SerializeObject(new{passed=true,count=checks.Count,checks},Formatting.Indented));
            return "World sync: "+checks.Count+" checks passed in a disposable preview scene.";
        } finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
