using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LetItBee.HoneyPilot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class BeeSimpleChecks
{
    public static string Begin()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Run SIMPLE client checks in Play Mode.");
        _ = Run(); return "Actual Unity SIMPLE client checks started (separate test session).";
    }
    static async Task Run()
    {
        var checks = new List<string>();
        var go = new GameObject("SIMPLE protocol checks (temporary)");
        IHoneyClient client = null;
        try {
            var pilot = UnityEngine.Object.FindFirstObjectByType<BeeHoneyPilot>();
            if (pilot != null && pilot.enabled) throw new InvalidOperationException("Disable the pilot for isolated checks before Play.");
            void Check(bool value, string label) { if (!value) throw new Exception(label); checks.Add(label); }
            string session = Guid.NewGuid().ToString("N");
            var a = new JArray(0,0,1.5); var b = new JArray(2,0,1.5); var h = new JArray(0,4,5.2);
            var config = new JObject {
                ["session"] = session, ["checkpoint"] = Application.dataPath.Replace("/Assets", "") + "/Experiments/LetItBee-HoneyPilot/Runtime/simple-check-"+session+".json",
                ["flowers"] = new JArray(
                    new JObject { ["id"]="flower-a",["plant_id"]="plant-a",["species"]="pilot",["position"]=a.DeepClone() },
                    new JObject { ["id"]="flower-b",["plant_id"]="plant-b",["species"]="pilot",["position"]=b.DeepClone() }),
                ["slot"]=new JObject { ["id"]="slot-a",["position"]=h.DeepClone() },
                ["contact_radius"]=1.0,["grab_radius"]=1.2,["snap_radius"]=0.35
            };
            var world = new BeeWorldSync(); config["world_source"] = world.Source;
            client = new HoneySimpleClient(go.transform);
            string endpoint = SessionState.GetString("LetItBee.Simple.CheckEndpoint", "ws://127.0.0.1:8080");
            var snap = await client.ConnectAsync(endpoint, "", config);
            Check((string)snap["state"]["session"]==session,"Unity connects using SIMPLE ConnectionManager and LinkToUnity");
            long cycle=(long)snap["cycle"]; await Task.Delay(400);snap=await client.SnapshotAsync();
            Check((long)snap["cycle"]>cycle,"GAMA advances independently through SIMPLE");
            // Actual MonoBehaviour lifecycle + actual Unity client + real GAMA.
            var fixture = new GameObject("World reference fixture");fixture.transform.SetParent(go.transform);fixture.SetActive(false);
            var observer = fixture.AddComponent<BeeHoneyPilot>();observer.enabled=false;
            Transform Ref(string name,Vector3 position) { var item=new GameObject(name).transform;item.SetParent(fixture.transform);item.position=position;return item; }
            observer.beeRoot=Ref("Bee",new Vector3(10,2,20));
            observer.flowerAHead=Ref("Flower A",new Vector3(0,1.5f,0));observer.flowerBHead=Ref("Flower B",new Vector3(2,1.5f,0));
            observer.slot=Ref("Hive",new Vector3(0,5.2f,4));observer.slotMount=observer.slot;
            async Task<JObject> SyncWorld() {
                var data=world.Prepare(world.Capture(observer),true);
                var result=(JObject)await client.AskAsync("sync_world",new JObject{["world_json"]=data.ToString(Formatting.None)});
                world.Accept((JObject)result["world_sync"]);return (JObject)result["world_sync"];
            }
            var observation=await SyncWorld();Check((int)observation["count"]==4,"world automatically registers player, pilot flowers and hive");
            var extraBee=new GameObject("Additional observed bee");extraBee.transform.SetParent(go.transform);extraBee.transform.position=new Vector3(-4,3,-7);
            var beeEntity=extraBee.AddComponent<BeeWorldEntity>();beeEntity.kind=BeeWorldEntity.EntityKind.Bee;
            var extraFlower=new GameObject("Additional observed flower");extraFlower.transform.SetParent(go.transform);
            var flowerEntity=extraFlower.AddComponent<BeeWorldEntity>();
            observation=await SyncWorld();Check((int)observation["count"]==6 && (int)observation["bees"]==2 && (int)observation["flowers"]==3,"adding bee and flower in Unity creates matching GAMA actors");
            var copiedBee=UnityEngine.Object.Instantiate(extraBee,go.transform);
            observation=await SyncWorld();Check((int)observation["bees"]==3 && copiedBee.GetComponent<BeeWorldEntity>().Id!=beeEntity.Id,"duplicating a bee creates one distinct GAMA actor");
            extraBee.transform.position=new Vector3(-8,4,12);
            observation=await SyncWorld();
            var observed=((JArray)observation["entities"]).First(x=>(string)x["id"]==beeEntity.Id);
            Check((float)observed["position"][0]==-8 && (float)observed["position"][1]==12 && (float)observed["position"][2]==4,"moving an actual Unity object updates signed GAMA position and altitude");
            extraFlower.SetActive(false);observation=await SyncWorld();Check((int)observation["flowers"]==2,"deactivating a Unity flower removes the GAMA actor");
            extraFlower.SetActive(true);observation=await SyncWorld();Check((int)observation["flowers"]==3,"reactivating a flower restores one actor");
            UnityEngine.Object.Destroy(copiedBee);await Task.Delay(50);observation=await SyncWorld();Check((int)observation["bees"]==2,"destroying a Unity bee removes it from GAMA");
            var unacknowledged=world.Prepare(world.Capture(observer),true);
            var accepted=(JObject)await client.AskAsync("sync_world",new JObject{["world_json"]=unacknowledged.ToString(Formatting.None)});
            var replay=(JObject)await client.AskAsync("sync_world",new JObject{["world_json"]=unacknowledged.ToString(Formatting.None)});
            Check((string)replay["world_sync"]["reason"]=="replayed" && (int)replay["world_sync"]["count"]==6,"replayed world snapshot cannot duplicate actors after an ignored ACK");
            world.Accept((JObject)replay["world_sync"]);
            var invalid=(JObject)unacknowledged.DeepClone();invalid["sequence"]=(long)invalid["sequence"]+1;
            ((JArray)invalid["entities"]).Add(invalid["entities"][0].DeepClone());
            var refused=(JObject)await client.AskAsync("sync_world",new JObject{["world_json"]=invalid.ToString(Formatting.None)});
            Check((bool)refused["world_sync"]["accepted"]==false,"ambiguous world IDs are rejected by actual GAMA");
            observation=await SyncWorld();Check((int)observation["count"]==6,"invalid update leaves previous actor set intact");
            JObject Request(string kind, JArray pos, int flower=-1) => new JObject {
                ["event_id"]=Guid.NewGuid().ToString("N"),["session"]=session,["kind"]=kind,["flower"]=flower,
                ["bee"]=pos.DeepClone(),["hand"]=pos.DeepClone(),["chunk_id"]=(string)snap["state"]["chunk_id"],["slot_id"]="slot-a"
            };
            async Task Send(JObject r) {
                await client.AskAsync("transact",new JObject { ["request_json"]=r.ToString(Formatting.None) });snap=await client.SnapshotAsync();
            }
            await client.AskAsync("set_pose",new JObject { ["pose_json"]=new JObject { ["bee"]=a.DeepClone(),["hand"]=a.DeepClone() }.ToString(Formatting.None) });
            snap=await client.SnapshotAsync();Check((float)snap["bee_position"][2]==1.5f,"live bee pose arrives in GAMA");
            await Send(Request("collect",new JArray(20,20,20),0));Check(!(bool)snap["response"]["accepted"],"reject remote collection");
            await Send(Request("visit",a,0));Check((int)snap["state"]["pollen_source"]==0,"flower A provides pollen");
            var collect=Request("collect",a,0);await Send(collect);Check((string)snap["state"]["chunk_state"]=="ready","GAMA allocates nectar identity");
            string chunk=(string)snap["state"]["chunk_id"];await Send(collect);
            Check((string)snap["state"]["chunk_id"]==chunk && (int)snap["state"]["nectar"][0]==0,"retry cannot duplicate nectar");
            await Send(Request("pick",a));Check((string)snap["state"]["chunk_state"]=="held","grip picks the registered chunk");
            await Send(Request("visit",b,1));Check((bool)snap["state"]["pollinated"][1],"cross pollination through SIMPLE");
            await Send(Request("deposit",a));Check(!(bool)snap["response"]["accepted"],"reject distant deposit");
            var deposit=Request("deposit",h);await Send(deposit);Check((int)snap["state"]["stored"]==1,"release fills one hive slot");
            client.Dispose();client=null;await Task.Delay(500);
            client=new HoneySimpleClient(go.transform);snap=await client.ConnectAsync(endpoint,"",config);
            Check((int)snap["state"]["stored"]==1,"reconnect preserves confirmed progress");
            world.Reconnect();observation=await SyncWorld();
            Check((int)observation["count"]==6 && (int)observation["bees"]==2,"reconnect rebuilds observations without duplicating objects");
            snap=await client.SnapshotAsync();Check((int)snap["state"]["stored"]==1,"world reconciliation preserves delivered honey");
            await Send(deposit);Check((int)snap["state"]["stored"]==1 && (bool)snap["response"]["accepted"],"lost deposit acknowledgement safely replays");
            Write(new {passed=true,count=checks.Count,checks,finishedUtc=DateTime.UtcNow.ToString("O")});
        } catch(Exception ex) { Write(new {passed=false,count=checks.Count,checks,error=ex.ToString()});Debug.LogError(ex); }
        finally { client?.Dispose();UnityEngine.Object.Destroy(go);BeeSimpleCheckSession.Finish(); }
    }
    static void Write(object result) { Directory.CreateDirectory("Library/HoneyPilot");File.WriteAllText("Library/HoneyPilot/unity-simple-validation.json",JsonConvert.SerializeObject(result,Formatting.Indented)); }
}
