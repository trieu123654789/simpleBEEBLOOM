using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class BeeBloomDemo : MonoBehaviour
{
    public string serverUrl = "ws://127.0.0.1:6871";
    public Material demoMaterial;
    GamaClient client;
    JArray snapshot;
    readonly List<GameObject> flowers = new();
    readonly Queue<(string action, JObject args)> actions = new();
    GameObject bee;
    Vector3 desired = new(20, 2, 20);
    string status = "Connecting to GAMA...";
    string lastAction = "Visit a flower, collect a chunk, return to the hive.";
    bool connected, stopped, selfTest, reconnecting;
    int eventId;
    float lastSnapshotTime;
    GUIStyle heading, text, small;
    string root;
    readonly List<string> checks = new();
    bool captured;

    static string Argument(string key, string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, key);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }
    async void Start()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
        root = Path.GetFullPath(Argument("-demoRoot", Path.Combine(Application.dataPath, "../..")));
        serverUrl = Argument("-gamaUrl", serverUrl);
        selfTest = Array.IndexOf(Environment.GetCommandLineArgs(), "-selfTest") >= 0;
        try { BuildWorld(); await Run(); }
        catch (Exception ex) { status = ex.Message; Debug.LogException(ex); if(selfTest) WriteTestResult(false,ex.ToString()); }
    }
    async Task Run()
    {
        reconnecting = true;
        client?.Dispose(); client = new GamaClient();
        connected = false; snapshot = null; actions.Clear(); eventId = 0;
        try {
            status = "Loading real GAMA model...";
            await client.Connect(serverUrl, Path.Combine(root, "GAMA/BeeBloomDemo.gaml"));
            if (stopped) return;
            if (selfTest) { await SelfTest(); return; }
            await client.Command("play");
            connected = true;
            status = "LIVE | GAMA experiment " + client.ExperimentId;
            while (!stopped) {
                await Pose(desired);
                while (actions.Count > 0) {
                    var action = actions.Dequeue();
                    await client.Ask(action.action, action.args);
                }
                snapshot = await client.Snapshot();
                if (stopped) break;
                lastSnapshotTime = Time.realtimeSinceStartup;
                UpdateWorld();
                if (!captured && (int)snapshot[0] > 20 && Array.IndexOf(Environment.GetCommandLineArgs(), "-capture") >= 0) {
                    captured = true;
                    ScreenCapture.CaptureScreenshot(Path.Combine(root,"Runtime/unity-demo.png"));
                }
                await Task.Delay(100);
            }
        } catch (Exception ex) {
            connected = false; status = "OFFLINE / ERROR: " + ex.Message;
            Debug.LogError(status);
            if (selfTest) WriteTestResult(false, ex.ToString());
        } finally { reconnecting = false; }
    }
    Task<JToken> Pose(Vector3 p) => client.Ask("set_pose", new JObject { ["px"] = p.x, ["py"] = p.z, ["pz"] = p.y });
    void Update()
    {
        if (!connected || selfTest) return;
        Vector3 move = new((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),
            (Input.GetKey(KeyCode.E)?1:0)-(Input.GetKey(KeyCode.Q)?1:0),
            (Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0));
        desired += Vector3.ClampMagnitude(move, 1) * (6 * Time.deltaTime);
        desired = new Vector3(Mathf.Clamp(desired.x,.5f,39.5f),Mathf.Clamp(desired.y,.5f,10),Mathf.Clamp(desired.z,.5f,39.5f));
        if (Input.GetKeyDown(KeyCode.Space)) Collect();
        if (Input.GetKeyDown(KeyCode.H)) Deposit();
    }
    void Collect()
    {
        if (snapshot == null) return;
        int nearest = 0; float distance = float.MaxValue;
        foreach (JArray f in (JArray)snapshot[8]) {
            float d = Vector3.Distance(desired, new Vector3((float)f[1],2,(float)f[2]));
            if (d < distance) { distance = d; nearest = (int)f[0]; }
        }
        actions.Enqueue(("collect", new JObject { ["flower_id"] = nearest, ["event_id"] = ++eventId }));
        lastAction = $"Collect flower {nearest}: GAMA checks distance, nectar and cargo.";
    }
    void Deposit()
    {
        actions.Enqueue(("deposit", new JObject { ["event_id"] = ++eventId }));
        lastAction = "Deposit requested: GAMA checks hive distance and carried chunk.";
    }
    GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
    {
        var obj = GameObject.CreatePrimitive(type); obj.name = name;
        obj.transform.position = position; obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().material = new Material(demoMaterial) { color = color };
        return obj;
    }
    void BuildWorld()
    {
        var cameraObject = new GameObject("Demo Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(10,43,-16); camera.transform.LookAt(new Vector3(10,0,20));
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.055f,.10f,.15f);
        camera.orthographic = true; camera.orthographicSize = 27;
        var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(55,-30,0); light.intensity = 1.3f;
        RenderSettings.ambientLight = new Color(.55f,.6f,.65f);
        Primitive("Garden",PrimitiveType.Cube,new Vector3(20,-.35f,20),new Vector3(40,.5f,40),new Color(.19f,.36f,.24f));
        Primitive("River",PrimitiveType.Cube,new Vector3(37,0,20),new Vector3(4,.12f,40),new Color(.12f,.48f,.66f));
        Primitive("Hive",PrimitiveType.Cylinder,new Vector3(20,1,20),new Vector3(3,1,3),new Color(.9f,.5f,.08f));
        bee = Primitive("Bee - position confirmed by GAMA",PrimitiveType.Sphere,desired,new Vector3(1.4f,.8f,1.0f),Color.yellow);
    }
    void UpdateWorld()
    {
        bee.transform.position = new Vector3((float)snapshot[1],(float)snapshot[3],(float)snapshot[2]);
        foreach (JArray f in (JArray)snapshot[8]) {
            int id = (int)f[0];
            while (flowers.Count <= id) flowers.Add(Primitive("Flower "+flowers.Count,PrimitiveType.Sphere,Vector3.zero,new Vector3(2,.5f,2),Color.magenta));
            flowers[id].transform.position = new Vector3((float)f[1],2,(float)f[2]);
            Color color = (bool)f[4] ? new Color(.2f,.85f,.45f) : Color.Lerp(new Color(1,.4f,.65f),new Color(.5f,.3f,.16f),(float)snapshot[7]);
            flowers[id].GetComponent<Renderer>().material.color = color * Mathf.Lerp(.4f,1,(float)f[3]);
        }
    }
    void OnGUI()
    {
        heading ??= new GUIStyle(GUI.skin.label) { fontSize=25, fontStyle=FontStyle.Bold, normal={textColor=new Color(1,.8f,.25f)} };
        text ??= new GUIStyle(GUI.skin.label) { fontSize=17, wordWrap=true };
        small ??= new GUIStyle(GUI.skin.label) { fontSize=14, wordWrap=true };
        GUI.Box(new Rect(16,16,380,Screen.height-32),GUIContent.none);
        GUILayout.BeginArea(new Rect(32,28,348,Screen.height-56));
        GUILayout.Label("BEE BLOOM / LAB",heading);
        GUILayout.Label("UNITY  <-->  GAMA",text);
        GUILayout.Label("Isolated desktop connection experiment",small);
        GUILayout.Space(15); GUILayout.Label(status,text);
        if (snapshot != null) {
            float age = Time.realtimeSinceStartup-lastSnapshotTime;
            GUILayout.Label($"Cycle {snapshot[0]} | received {age:0.0}s ago",small);
            if (age > 2) GUILayout.Label("STALE DATA - waiting for GAMA",text);
            GUILayout.Space(12);
            GUILayout.Label($"Carrying: {snapshot[4]} / 1\nHive: {snapshot[5]} / 5\nPollinated visits: {snapshot[6]}\nHeight: {(float)snapshot[3]:0.0} m",text);
            if ((int)snapshot[5] == 5) GUILayout.Label("ROUND COMPLETE!",heading);
        }
        GUILayout.Space(14);
        GUILayout.Label("WASD: move | Q/E: down/up\nSPACE: collect | H: deposit",text);
        GUI.enabled = connected && actions.Count < 8;
        if (GUILayout.Button("Collect nearest flower",GUILayout.Height(34))) Collect();
        if (GUILayout.Button("Deposit at hive",GUILayout.Height(34))) Deposit();
        GUILayout.Space(10);
        GUILayout.Label("Quick transport test",small);
        if (GUILayout.Button("Go to next available flower",GUILayout.Height(30)) && snapshot != null) {
            foreach(JArray f in (JArray)snapshot[8]) if ((float)f[3]>=1) { desired=new Vector3((float)f[1],2,(float)f[2]); break; }
        }
        if (GUILayout.Button("Return to hive",GUILayout.Height(30))) desired=new Vector3(20,2,20);
        GUILayout.Space(10);
        if (GUILayout.Button("Toggle illustrative vegetation stress",GUILayout.Height(32)))
            actions.Enqueue(("set_stress",new JObject{["value"] = snapshot != null && (float)snapshot[7]>.5f ? 0f : 1f}));
        GUI.enabled = true;
        GUILayout.Label("Stress is an uncalibrated test value, not measured salinity or ecological evidence.",small);
        GUILayout.Space(12); GUILayout.Label(lastAction,small);
        if (!connected && !reconnecting && GUILayout.Button("Reconnect / start fresh demo")) _ = Run();
        GUILayout.Label("Yellow: bee | pink: flower\nGreen: pollinated | orange: hive",small);
        GUILayout.EndArea();
    }
    void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAILED: " + name);
        checks.Add(name); Debug.Log("PASS: " + name);
    }
    async Task SelfTest()
    {
        await client.Command("step",new JObject{["nb_step"]=1,["sync"]=true});
        snapshot = await client.Snapshot();
        Check(((JArray)snapshot[8]).Count==10,"GAMA initializes ten flowers");
        await client.Ask("collect",new JObject{["flower_id"]=0,["event_id"]=++eventId});
        snapshot=await client.Snapshot(); Check((int)snapshot[4]==0,"Reject collection outside radius");
        for(int i=0;i<5;i++) {
            await Pose(new Vector3(8+i*6,2,8));
            snapshot=await client.Snapshot();
            Check(Math.Abs((float)snapshot[1]-(8+i*6))<.01f && (float)snapshot[2]==8 && (float)snapshot[3]==2,"Unity XYZ to GAMA XZY round trip "+i);
            int collectId=++eventId;
            await client.Ask("collect",new JObject{["flower_id"]=i,["event_id"]=collectId});
            await client.Ask("collect",new JObject{["flower_id"]=i,["event_id"]=collectId});
            snapshot=await client.Snapshot();
            Check((int)snapshot[4]==1 && (int)snapshot[6]==i+1,"Collect acknowledged once "+i);
            Check((bool)snapshot[8][i][4] && (float)snapshot[8][i][3]<1,"Pollination and nectar state returned "+i);
            await client.Ask("deposit",new JObject{["event_id"]=++eventId});
            snapshot=await client.Snapshot(); Check((int)snapshot[5]==i,"Reject deposit away from hive "+i);
            await Pose(new Vector3(20,2,20));
            int depositId=++eventId;
            await client.Ask("deposit",new JObject{["event_id"]=depositId});
            await client.Ask("deposit",new JObject{["event_id"]=depositId});
            snapshot=await client.Snapshot(); Check((int)snapshot[5]==i+1 && (int)snapshot[4]==0,"Deposit acknowledged once "+i);
        }
        await client.Ask("set_stress",new JObject{["value"]=1.0});
        snapshot=await client.Snapshot(); Check((float)snapshot[7]==1,"GAMA environmental state reaches Unity");
        await client.Command("play"); await Task.Delay(400); await client.Command("pause");
        snapshot=await client.Snapshot(); Check((int)snapshot[0]>1,"GAMA advances independently while Unity runs");
        UpdateWorld();
        await client.Command("stop");
        client.Dispose();
        bool disconnected=false;
        try { await client.Snapshot(); } catch { disconnected=true; }
        Check(disconnected,"Disconnected transport reports error instead of fabricated data");
        client=new GamaClient(); await client.Connect(serverUrl,Path.Combine(root,"GAMA/BeeBloomDemo.gaml"));
        snapshot=await client.Snapshot(); Check((int)snapshot[5]==0,"Reconnect starts an explicitly fresh isolated experiment");
        await client.Command("stop"); client.Dispose();
        WriteTestResult(true,null);
    }
    void WriteTestResult(bool passed,string error)
    {
        Directory.CreateDirectory(Path.Combine(root,"Runtime"));
        File.WriteAllText(Path.Combine(root,"Runtime/unity-test-result.json"),new JObject {
            ["passed"]=passed,["utc"]=DateTime.UtcNow.ToString("O"),["checks"]=new JArray(checks),["error"]=error,
            ["server"]=serverUrl,["source"]="Unity standalone player connected to actual GAMA server"
        }.ToString());
        Application.Quit(passed?0:1);
    }
    void OnDestroy() { stopped=true; client?.Dispose(); }
    void OnApplicationQuit() { stopped=true; client?.Dispose(); }
}
