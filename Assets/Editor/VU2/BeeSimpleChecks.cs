using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BeeBloom.HoneyPilot;
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
                ["session"] = session, ["checkpoint"] = Application.dataPath.Replace("/Assets", "") + "/Experiments/BeeBloom-HoneyPilot/Runtime/simple-check-"+session+".json",
                ["flowers"] = new JArray(
                    new JObject { ["id"]="flower-a",["plant_id"]="plant-a",["species"]="pilot",["position"]=a.DeepClone() },
                    new JObject { ["id"]="flower-b",["plant_id"]="plant-b",["species"]="pilot",["position"]=b.DeepClone() }),
                ["slot"]=new JObject { ["id"]="slot-a",["position"]=h.DeepClone() },
                ["contact_radius"]=1.0,["grab_radius"]=1.2,["snap_radius"]=0.35
            };
            client = new HoneySimpleClient(go.transform);
            var snap = await client.ConnectAsync("ws://127.0.0.1:8080", "", config);
            Check((string)snap["state"]["session"]==session,"Unity connects using SIMPLE ConnectionManager and LinkToUnity");
            long cycle=(long)snap["cycle"]; await Task.Delay(400);snap=await client.SnapshotAsync();
            Check((long)snap["cycle"]>cycle,"GAMA advances independently through SIMPLE");
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
            client=new HoneySimpleClient(go.transform);snap=await client.ConnectAsync("ws://127.0.0.1:8080","",config);
            Check((int)snap["state"]["stored"]==1,"reconnect preserves confirmed progress");
            await Send(deposit);Check((int)snap["state"]["stored"]==1 && (bool)snap["response"]["accepted"],"lost deposit acknowledgement safely replays");
            Write(new {passed=true,count=checks.Count,checks,finishedUtc=DateTime.UtcNow.ToString("O")});
        } catch(Exception ex) { Write(new {passed=false,count=checks.Count,checks,error=ex.ToString()});Debug.LogError(ex); }
        finally { client?.Dispose();UnityEngine.Object.Destroy(go); }
    }
    static void Write(object result) { Directory.CreateDirectory("Library/HoneyPilot");File.WriteAllText("Library/HoneyPilot/unity-simple-validation.json",JsonConvert.SerializeObject(result,Formatting.Indented)); }
}
