using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LetItBee.HoneyPilot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BeeInteractionV3Checks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [MenuItem("Let It Bee/Check Flower Honey Interaction")]
    public static void Menu() => Debug.Log(Run());
    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play before isolated interaction checks.");
        var original = SceneManager.GetActiveScene(); bool dirty = original.isDirty;
        var scene = EditorSceneManager.NewPreviewScene();
        var checks = new List<string>();
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks.Add(message); }
        GameObject Obj(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go; }
          var root = Obj("Isolated honey interaction"); root.SetActive(false);
        try {
            var pilot = root.AddComponent<BeeHoneyPilot>(); pilot.beeRoot = root.transform;
            pilot.rightHand = Obj("Hand").transform;
            pilot.trackedCamera = Obj("View").AddComponent<Camera>();
            pilot.trackedCamera.transform.position = new Vector3(0f,1.5f,-0.7f);
            pilot.flowerAHead = Obj("Flower A").transform; pilot.flowerAHead.position = new Vector3(0f,1.5f,0f);
            pilot.flowerBHead = Obj("Flower B").transform; pilot.flowerBHead.position = new Vector3(3f,1.5f,0f);
            pilot.slot = Obj("Socket").transform; pilot.slot.position = new Vector3(0f,1.5f,-0.4f);
            var petal = Obj("Petal_01_end").transform; petal.SetParent(pilot.flowerAHead,false); petal.localPosition = Vector3.right*0.7f;
            var petalB = Obj("Petal_01_end B").transform; petalB.SetParent(pilot.flowerBHead,false); petalB.localPosition = Vector3.right*0.7f;
            pilot.honeyMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            pilot.labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pilot.enhancedFeedback = false; pilot.enableFlowerPerching = false;
            pilot.collectDuration = 0.2f;
            typeof(BeeHoneyPilot).GetMethod("BuildViews",Private).Invoke(pilot,null);
            void Set(string name, object value) => typeof(BeeHoneyPilot).GetProperty(name).SetValue(pilot,value);
            object Field(string name) => typeof(BeeHoneyPilot).GetField(name,Private).GetValue(pilot);
            var state = new JObject { ["chunk_state"]="none", ["chunk_id"]="", ["chunk_pos"]=new JArray(0,0,0),
                ["chunk_flower"]=0,["stored"]=0,["nectar"]=new JArray(1,1),["pollinated"]=new JArray(false,false),["pollen_source"]=-1 };
            Set("State",state); Set("Live",true);
            var step = typeof(BeeHoneyPilot).GetMethod("StepInteraction",Private);
            void Step(float trigger=0f,float grip=0f,bool stable=true,float dt=0.1f) => step.Invoke(pilot,new object[]{trigger,grip,stable,false,dt});
            JObject Pending() => (JObject)Field("pendingRequest");
            void Ack(bool accepted=true,string reason="visited") {
                var request=Pending();
                typeof(BeeHoneyPilot).GetMethod("CompleteRequest",Private).Invoke(pilot,new object[]{request,new JObject{["accepted"]=accepted,["reason"]=reason}});
            }
            pilot.rightHand.position = new Vector3(0f,1.5f,-1f);
            Step();Step();Step();Step();Step();
            Check(Pending()==null,"flying past a flower never silently pollinates it");
            var geometry = new BeeFlowerGeometry(pilot.flowerAHead);
            Check(geometry.Radius>0.69f && geometry.ViewDistance(0.75f)>1.15f,"large petals increase viewing clearance without changing saved scale");
            Check(geometry.Distance(new Vector3(0f,1.525f,-1.2f))<pilot.contactRadius,"branch standoff uses blossom volume rather than hidden head bone");
            pilot.rightHand.position = geometry.Nectar;
            Step();Step();Step();Step();
            Check((string)Pending()?["kind"]=="visit","deliberate steady contact submits exactly one pollen visit");
            Step(1f);
            Check((string)Pending()?["kind"]=="visit" && pilot.Charge01==0f,"nectar waits for pollen acknowledgement without overwriting pending request");
            Ack();
            Step(1f);Step(1f);
            Check((string)Pending()?["kind"]=="collect","holding trigger completes nectar charge after confirmed contact");
            state["chunk_state"]="ready";state["chunk_id"]="test-honey";
            state["chunk_pos"]=new JArray(geometry.Nectar.x,geometry.Nectar.z,geometry.Nectar.y);
            Ack(true,"collected");
            Step();
            var chunk=(Transform)Field("chunk");
            chunk.position=geometry.Nectar;
            string hint=(string)typeof(BeeHoneyPilot).GetMethod("FlowerHint",Private).Invoke(pilot,new object[]{0});
            Check(hint=="","existing honey suppresses misleading flower collection labels");
            Step(0f,1f,false);
            Check((string)Pending()?["kind"]=="pick","right grip can pick nearby honey without left trigger hold");
            Step(0f,0f,false);
            Check((string)Pending()?["kind"]=="pick","release during pickup latency preserves the pending pickup identity");
            state["chunk_state"]="held"; Ack(true,"picked");
            Step(0f,0f,false);
            Check((string)Pending()?["kind"]=="drop","early grip release is processed after pickup acknowledgement");
            Vector3 released=(Vector3)Field("releaseChunk");
            Check(Vector3.Distance(chunk.position,released)<0.001f,"pending release freezes honey at the submitted visible position");
            state["chunk_state"]="loose";state["chunk_pos"]=new JArray(released.x,released.z,released.y);Ack(true,"dropped");
            chunk.position=released; pilot.rightHand.position=released;
            Step(0f,1f,false);state["chunk_state"]="held";Ack(true,"picked");
            pilot.rightHand.position=pilot.slot.position-new Vector3(0f,0.015f,0.12f);
            chunk.position=pilot.slot.position;
            Step(0f,0f,false);
            Check((string)Pending()?["kind"]=="deposit","release uses honey centre inside socket rather than palm position");
            Ack(false,"wrong_chunk_slot_or_distance");
            Check((float)Field("failureUntil")>Time.time,"server rejection produces a recoverable visible explanation");
            typeof(BeeHoneyPilot).GetField("failureUntil",Private).SetValue(pilot,0f);
            state["chunk_state"]="ready"; chunk.position=geometry.Nectar;
            Step(0f,0f);
            var label=(TextMesh)Field("labelChunk");
            Check(label.text.Contains("BEN HONG") && label.text.Length<70,"honey target names the physical side button instead of ambiguous grip jargon");
            var size=label.GetComponent<Renderer>().bounds.size;
            Check(Mathf.Max(size.x,size.z)<0.61f && size.y<0.12f,"long labels cannot cover the headset view");
            pilot.trackedCamera.transform.position=new Vector3(1.6f,1.525f,0f);
            int next=(int)typeof(BeeHoneyPilot).GetMethod("NearestContact",Private).Invoke(pilot,null);
            Check(next==1,"overlapping flower ranges can switch to the closer blossom instead of trapping the old target");
            string layout="BeeGripButtonProbe"+Guid.NewGuid().ToString("N");
            UnityEngine.InputSystem.InputDevice probe=null;
            try {
                UnityEngine.InputSystem.InputSystem.RegisterLayout("{\"name\":\""+layout+"\",\"extend\":\"XRController\",\"controls\":[{\"name\":\"gripPressed\",\"layout\":\"Button\",\"format\":\"BIT\"}]}");
                probe=UnityEngine.InputSystem.InputSystem.AddDevice(layout);
                var button=(UnityEngine.InputSystem.Controls.ButtonControl)probe["gripPressed"];
                var read=typeof(BeeHoneyPilot).GetMethod("ReadRightControl",Private);
                float Grip()=> (float)read.Invoke(pilot,new object[]{true,0f});
                UnityEngine.InputSystem.InputSystem.SetDeviceUsage(probe,UnityEngine.InputSystem.CommonUsages.LeftHand);
                UnityEngine.InputSystem.InputSystem.QueueDeltaStateEvent(button,(byte)1);UnityEngine.InputSystem.InputSystem.Update();
                Check(Grip()==0f,"left controller button cannot pick honey for the right hand");
                UnityEngine.InputSystem.InputSystem.SetDeviceUsage(probe,UnityEngine.InputSystem.CommonUsages.RightHand);
                Check(Grip()==1f,"right digital gripPressed works even when no analog grip control exists");
                Check((float)read.Invoke(pilot,new object[]{false,0f})==0f,"side button cannot accidentally charge the front trigger action");
                UnityEngine.InputSystem.InputSystem.QueueDeltaStateEvent(button,(byte)0);UnityEngine.InputSystem.InputSystem.Update();
                Check(Grip()==0f,"digital-only grip release clears the hold instead of sticking");
            } finally {
                if(probe!=null)UnityEngine.InputSystem.InputSystem.RemoveDevice(probe);
                UnityEngine.InputSystem.InputSystem.RemoveLayout(layout);
            }
            Check(SceneManager.GetActiveScene()==original && original.isDirty==dirty,"isolated checks preserve the user scene and dirty state");
            string json=JsonConvert.SerializeObject(new{passed=true,count=checks.Count,checks},Formatting.Indented);
            Directory.CreateDirectory("Library/HoneyPilot/InteractionV3"); File.WriteAllText("Library/HoneyPilot/InteractionV3/checks.json",json);
            return json;
        } finally {
            // OnDisable is normally called in Play. These inactive preview objects
            // need explicit runtime view cleanup before closing their scene.
            var p=root!=null?root.GetComponent<BeeHoneyPilot>():null;
            if(p!=null) typeof(BeeHoneyPilot).GetMethod("OnDisable",Private).Invoke(p,null);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
