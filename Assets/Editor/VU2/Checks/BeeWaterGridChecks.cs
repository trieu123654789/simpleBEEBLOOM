using System;
using System.IO;
using System.Threading.Tasks;
using LetItBee.HoneyPilot;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class BeeWaterGridChecks
{
    [MenuItem("Let It Bee/Water/Check Coordinate Mapping")]
    public static void Menu() { Debug.Log(Run()); }
    public static string Run()
    {
        var origin = new Vector2(-56,-64);
        for (int i=0;i<56*55;i++) {
            Vector3 center=BeeWaterGrid.CellCenter(i,origin,8,56,.5f);
            if(BeeWaterGrid.CellIndex(center,origin,8,56,55)!=i) throw new Exception("Cell round trip failed: "+i);
            center.y=500;
            if(BeeWaterGrid.CellIndex(center,origin,8,56,55)!=i) throw new Exception("Altitude changed horizontal cell.");
        }
        if(BeeWaterGrid.CellIndex(new Vector3(-56.01f,0,-64),origin,8,56,55)!=-1
            || BeeWaterGrid.CellIndex(new Vector3(-56,0,-64.01f),origin,8,56,55)!=-1
            || BeeWaterGrid.CellIndex(new Vector3(392,0,-64),origin,8,56,55)!=-1
            || BeeWaterGrid.CellIndex(new Vector3(-56,0,376),origin,8,56,55)!=-1)
            throw new Exception("Outside boundary accepted.");
        return "PASS: 3080 cell round trips, altitude independence, negative origin and four outside boundaries. Scene unchanged.";
    }

    public const string IntegrationReport = "Experiments/LetItBee-HoneyPilot/Runtime/unity-water-validation.json";
    [MenuItem("Let It Bee/Water/Check GAMA Round Trip (Port 6873)")]
    public static async void RunIntegration()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Run isolated integration checks in Edit Mode.");
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        GameObject go=null; TextAsset config=null; HoneyGamaClient client=null;
        try {
            var folder=Path.GetFullPath("Experiments/LetItBee-HoneyPilot/Runtime/unity-water-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            foreach(string name in new[]{"LetItBeeHoneyPilot.gaml","LetItBeeWater.gaml"})
                File.Copy("Experiments/LetItBee-HoneyPilot/GAMA/"+name,Path.Combine(folder,name));
            var settings=new JObject {
                ["session"]=Guid.NewGuid().ToString("N"),["checkpoint"]=Path.Combine(folder,"checkpoint.json"),
                ["flowers"]=new JArray(
                    new JObject{["id"]="a",["plant_id"]="a",["species"]="test",["position"]=new JArray(0,0,1)},
                    new JObject{["id"]="b",["plant_id"]="b",["species"]="test",["position"]=new JArray(2,0,1)}),
                ["slot"]=new JObject{["id"]="slot-a",["position"]=new JArray(0,4,5)},
                ["contact_radius"]=1.0,["grab_radius"]=1.2,["snap_radius"]=.35
            };
            go=new GameObject("Water transport check");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
            var grid=go.AddComponent<BeeWaterGrid>();
            config=new TextAsset(File.ReadAllText("Experiments/LetItBee-HoneyPilot/Water/channel-demo.json"));
            grid.configuration=config; grid.connectToGama=true; grid.runTransport=false; grid.snapshotIntervalSeconds=.5f;
            client=new HoneyGamaClient();
            var first=await client.ConnectAsync("ws://127.0.0.1:6873",Path.Combine(folder,"LetItBeeHoneyPilot.gaml"),settings);
            string original=first["state"].ToString();
            await grid.SynchronizeAsync(client);
            if(grid.Snapshot==null || !grid.IsFresh) throw new Exception(grid.Status);
            await client.AskAsync("advance_water",new JObject{["seconds"]=10.0});
            await Task.Delay(650);
            await grid.SynchronizeAsync(client);
            if((double)grid.Snapshot["time_seconds"]!=10 || (double)grid.Snapshot["salt_mass_kg"]<=0)
                throw new Exception("Unity did not receive the advanced GAMA salt field.");
            if((await client.SnapshotAsync())["state"].ToString()!=original) throw new Exception("Water touched honey progress.");
            File.WriteAllText(IntegrationReport,new JObject{["passed"]=true,["status"]=grid.Status,["snapshot"]=grid.Snapshot}.ToString());
            Debug.Log("PASS: Unity -> GAMA -> Unity water configuration, salt field and freshness; honey state unchanged.");
        } catch(Exception ex) {
            File.WriteAllText(IntegrationReport,new JObject{["passed"]=false,["error"]=ex.ToString()}.ToString());
            Debug.LogException(ex);
        } finally {
            client?.Dispose();
            if(go!=null) UnityEngine.Object.DestroyImmediate(go);
            if(config!=null) UnityEngine.Object.DestroyImmediate(config);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
