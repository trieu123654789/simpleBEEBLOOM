using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LetItBee.HoneyPilot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Uses disposable preview objects and real project art; never enters the user's scene in Play.
public static class BeeHoneyFeedbackChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const string Folder = "Library/HoneyPilot/HoneyFeedback";
    [MenuItem("Let It Bee/Check Honey Presentation")]
    public static void Menu() => Debug.Log(Run());
    public static string Run(bool renderMotion = false)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play before presentation checks.");
        Directory.CreateDirectory(Folder);
        string motionFolder = "Motion-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        if (renderMotion) Directory.CreateDirectory(Folder+"/"+motionFolder);
        int frames = 0, ticks = 0; bool record = renderMotion;
        var phases = new List<object>();
        void Phase(string title) { if (renderMotion) phases.Add(new {title,frame=frames}); }
        var source = UnityEngine.Object.FindFirstObjectByType<BeeHoneyPilot>();
        if (source == null || source.flowerAAnimator == null) throw new InvalidOperationException("Keep the current Let It Bee scene open for art references.");
        string sourceJson = EditorJsonUtility.ToJson(source);
        var sourceScene = SceneManager.GetActiveScene(); bool wasDirty = sourceScene.isDirty;
        var scene = EditorSceneManager.NewPreviewScene();
        BeeHoneyFeedback fx = null;
        RenderTexture target = null;
        var checks = new List<string>();
        void Check(bool valid, string name) { if (!valid) throw new Exception(name); checks.Add(name); }
        GameObject Obj(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go,scene); return go; }
        try {
            var owner = Obj("Feedback test owner");
            var camera = Obj("Preview camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0.08f,0.65f,-0.42f);
            camera.transform.LookAt(new Vector3(0.055f,0.12f,0f));
            camera.nearClipPlane = 0.04f; camera.farClipPlane = 10f; camera.fieldOfView = 58f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.025f,0.055f,0.065f);
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            camera.scene = scene;
            var lamp = Obj("Preview light").AddComponent<Light>(); lamp.type = LightType.Directional; lamp.intensity = 1.1f;
            lamp.transform.rotation = Quaternion.Euler(45f,-30f,0f);
            var flower = UnityEngine.Object.Instantiate(source.flowerAAnimator.gameObject);
            SceneManager.MoveGameObjectToScene(flower,scene); flower.name = "Preview daisy";
            flower.transform.localScale = source.flowerAAnimator.transform.lossyScale;
            var animation = flower.GetComponent<Animator>(); if (animation != null) animation.enabled = false;
            source.pilotFlowerIdle.animationClips[0].SampleAnimation(flower,0.45f);
            var a = flower.GetComponentsInChildren<Transform>(true).First(x => x.name == source.flowerAHead.name);
            flower.transform.position -= a.position;
            var second = UnityEngine.Object.Instantiate(flower); SceneManager.MoveGameObjectToScene(second,scene);
            var b = second.GetComponentsInChildren<Transform>(true).First(x => x.name == source.flowerAHead.name);
            second.transform.position += new Vector3(-1.25f,0f,0.1f)-b.position;
            second.SetActive(false);
            var slot = Obj("Preview hive socket").transform; slot.position = new Vector3(0.85f,0.35f,-0.05f);
            slot.rotation = Quaternion.LookRotation(new Vector3(0.2f,0.3f,-1f));
            var hive = UnityEngine.Object.Instantiate(source.slotMount.parent.gameObject);
            SceneManager.MoveGameObjectToScene(hive,scene); hive.name = "Preview actual hive";
            var originalPivot = source.slotMount.parent;
            var originalSlotRotation = Quaternion.LookRotation(source.slotMount.forward,Vector3.up);
            var localSocket = originalPivot.InverseTransformPoint(source.slotMount.TransformPoint(source.slotMountLocalPoint));
            hive.transform.localScale = originalPivot.lossyScale;
            hive.transform.rotation = slot.rotation*Quaternion.Inverse(originalSlotRotation)*originalPivot.rotation;
            hive.transform.position = slot.position-hive.transform.TransformVector(localSocket);
            var oldHoney = hive.GetComponentsInChildren<Renderer>(true).FirstOrDefault(x=>x.name==source.slotDecoration.name);
            if (oldHoney != null) oldHoney.enabled = false;
            hive.SetActive(false);
            var hand = Obj("Preview hand position").transform; hand.position = new Vector3(0.17f,0.12f,-0.1f);
            var chunk = Obj("Preview nectar").transform;
            chunk.gameObject.AddComponent<MeshFilter>().sharedMesh = source.honeyMesh;
            chunk.gameObject.AddComponent<MeshRenderer>().sharedMaterial = source.honeyMaterial;
            chunk.gameObject.SetActive(false); chunk.position = a.position + Vector3.up*0.16f;
            fx = new BeeHoneyFeedback(owner.transform,camera,a,b,slot,source.honeyMaterial,oldHoney,source.honeyMesh); fx.Configure(1f,0f,0f);
            chunk.position = fx.NectarRestPosition(0);
            if (fx.NectarMaterial != null) chunk.GetComponent<MeshRenderer>().sharedMaterial = fx.NectarMaterial;
            Check(fx.Ready && !fx.CollectionAnimating && !fx.DepositAnimating, "resources load without inventing confirmed actions");
            float charge = 0; bool waiting = false; string state = "none"; int pollen = -1, mask = 0, contact = 0;
            var responseType = typeof(BeeHoneyPilot).GetNestedType("FlowerResponse",BindingFlags.NonPublic);
            var poseA = Activator.CreateInstance(responseType,new object[]{a});
            var poseB = Activator.CreateInstance(responseType,new object[]{b});
            var applyPose = responseType.GetMethod("Apply");
            target = new RenderTexture(960,720,24); target.Create(); camera.targetTexture = target;
            void Tick(float dt = 0.02f) {
                if (state == "held") {
                    Vector3 pos; Quaternion rot;
                    if (fx.PickupAnimating) fx.PickupPose(hand,out pos,out rot);
                    else BeeHoneyFeedback.FollowHandPose(chunk.position,chunk.rotation,hand,dt,out pos,out rot);
                    chunk.SetPositionAndRotation(pos,rot);
                } else if (state == "stored") { fx.DepositPose(out var pos,out var rot); chunk.SetPositionAndRotation(pos,rot); }
                chunk.localScale = fx.NectarScale(state);
                bool holding = charge > 0f || waiting;
                fx.Step(dt,true,contact,charge,waiting,false,holding ? 0 : -1,holding,pollen,mask,state,chunk.position,hand,0.35f);
                applyPose.Invoke(poseA,new object[]{holding ? 1f : 0f,charge,fx.FlowerBloom(0),fx.FlowerRebound(0)});
                applyPose.Invoke(poseB,new object[]{0f,0f,fx.FlowerBloom(1),fx.FlowerRebound(1)});
                if (record && ticks++%2 == 0) Capture(motionFolder+"/"+(frames++).ToString("D4"));
            }
            Phase("Giọt mật hội tụ");
            for (int i = 0; i < 60; i++) { charge = (i+1)/60f; Tick(); }
            record = false;
            Check(fx.Formation > 0.6f && !fx.CollectionAnimating, "holding trigger forms nectar but cannot confirm collection");
            void Capture(string file) {
                camera.Render(); var old = RenderTexture.active; RenderTexture.active = target;
                var pixels = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                try { pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply(); File.WriteAllBytes(Folder+"/"+file+".png",pixels.EncodeToPNG()); }
                finally { RenderTexture.active = old; UnityEngine.Object.DestroyImmediate(pixels); }
            }
            Capture("01-nectar-gathering");
            charge = 0; for (int i = 0; i < 30; i++) Tick();
            Check(fx.Formation == 0 && !fx.CollectionAnimating, "release retracts unconfirmed nectar without granting a chunk");
            waiting = true; for (int i = 0; i < 40; i++) Tick();
            Check(fx.Formation > 0.9f && !fx.CollectionAnimating, "lost or slow GAMA reply stays in anticipation");
            Check(fx.Confirm("collect-1","collect",0,false,chunk.position,chunk.rotation), "accepted collection begins the reveal");
            Check(!fx.Confirm("collect-1","collect",0,false,chunk.position,chunk.rotation), "replayed event cannot repeat success feedback");
            waiting = false; state = "ready"; chunk.gameObject.SetActive(true);
            record = renderMotion;
            Phase("GAMA xác nhận: mật thành hình");
            for (int i = 0; i < 18; i++) Tick();
            record = false;
            Capture("02-nectar-confirmed");
            Check(fx.CollectionScale > 0.9f && fx.CollectionScale < 1.15f, "nectar reveal has a bounded soft overshoot");
            pollen = 0; fx.Confirm("visit-a","visit",0,false,chunk.position,chunk.rotation);
            for (int i = 0; i < 45; i++) Tick();
            Check(!fx.PickupAnimating,"ready nectar never lifts before a confirmed grip");
            Vector3 liftStart = chunk.position; Quaternion liftRotation = chunk.rotation;
            fx.Confirm("pick-1","pick",-1,false,liftStart,liftRotation);
            Check(!fx.Confirm("pick-1","pick",-1,false,liftStart,liftRotation),"replayed pickup cannot restart the elastic lift");
            fx.PickupPose(hand,out var liftedStart,out var liftedRotation);
            Check(Vector3.Distance(liftedStart,liftStart)<0.0001f && Quaternion.Angle(liftedRotation,liftRotation)<0.01f,"pickup begins exactly at the visible nectar pose");
            state = "held"; record = renderMotion; Phase("Nhấc mật: kéo sợi rồi theo tay");
            for (int i = 0; i < 8; i++) Tick();
            Check(fx.PickupThreadVisible,"a nearby lift stretches a visible nectar filament");
            for (int i = 0; i < 35; i++) { hand.position += new Vector3(0.002f,0.001f,-0.001f); Tick(); }
            Capture("06-nectar-held");
            Check(!fx.PickupAnimating && !fx.PickupThreadVisible,"nectar filament detaches instead of becoming a permanent tether");
            var handTarget = hand.TransformPoint(new Vector3(0f,0.015f,0.12f));
            BeeHoneyFeedback.FollowHandPose(hand.position+Vector3.right*5f,Quaternion.Euler(70,90,15),hand,0.02f,out var follow,out var followRotation);
            Check(Vector3.Distance(follow,handTarget)<=0.0351f && Quaternion.Angle(followRotation,hand.rotation)<=10.01f,"fast controller movement keeps held nectar within 3.5 cm and 10 degrees");
            record = false;
            second.SetActive(true);
            camera.transform.position = new Vector3(-0.5f,1.3f,-0.9f); camera.transform.LookAt(new Vector3(-0.6f,0f,0f)); camera.fieldOfView = 65f;
            fx.Confirm("visit-b","visit",1,true,chunk.position,chunk.rotation); pollen = 1; mask = 2;
            record = false;
            for (int i = 0; i < 12; i++) Tick();
            Capture("03-pollen-transfer");
            Check(fx.FlowerBloom(1) == 0f, "pollen travels before the destination flower response");
            for (int i = 0; i < 25; i++) Tick();
            Check(fx.FlowerBloom(1) > 0f && fx.FlowerBloom(0) == 0f, "confirmed transfer responds only at the destination flower");
            record = false;
            flower.SetActive(false); second.SetActive(false);
            hive.SetActive(true);
            camera.transform.position = slot.position + slot.forward*0.72f + slot.up*0.15f + slot.right*0.14f;
            camera.transform.LookAt(slot.position+slot.right*0.1f-slot.up*0.08f); camera.fieldOfView = 52f;
            hand.position = slot.position + slot.forward*0.19f + slot.right*0.06f;
            state = "held"; chunk.position = hand.position; Tick();
            record = renderMotion; Phase("Gắn mật: khớp ô và niêm viền sáp");
            for (int i = 0; i < 12; i++) Tick();
            Capture("07-hive-ready");
            Vector3 start = chunk.position; Quaternion startRotation = Quaternion.Euler(10,30,5);
            Check(!fx.DepositAnimating, "socket proximity alone cannot insert nectar");
            fx.Confirm("deposit-1","deposit",-1,false,start,startRotation);
            fx.DepositPose(out var snapStart,out var snapRotation);
            Check(Vector3.Distance(start,snapStart) < 0.0001f && Quaternion.Angle(startRotation,snapRotation)<0.01f, "accepted insertion starts at the currently held visual pose");
            state = "stored";
            record = renderMotion;
            for (int i = 0; i < 18; i++) Tick();
            fx.DepositPose(out var aligned,out var alignedRotation);
            Vector3 axis = fx.SocketRotation*Vector3.forward;
            Check(Quaternion.Angle(alignedRotation,fx.SocketRotation)<0.01f
                && Vector3.Cross(aligned-fx.SocketCenter,axis).magnitude<0.0001f
                && Vector3.Dot(aligned-fx.SocketCenter,axis)>=0.159f,
                "nectar aligns outside the real socket before sliding inward");
            float previousDepth = Vector3.Dot(aligned-fx.SocketCenter,axis);
            for (int i = 0; i < 20; i++) {
                Tick(); fx.DepositPose(out var pos,out var rot);
                float depth = Vector3.Dot(pos-fx.SocketCenter,axis);
                Check(Vector3.Cross(pos-fx.SocketCenter,axis).magnitude<0.0001f && depth<=previousDepth+0.0001f
                    && depth>=-0.0001f && Quaternion.Angle(rot,fx.SocketRotation)<0.01f,
                    "insertion stays aligned and advances inward without overshooting, frame "+i);
                previousDepth = depth;
            }
            Capture("04-hive-insertion");
            for (int i = 0; i < 20; i++) Tick();
            fx.DepositPose(out var finishedPosition,out var finishedRotation); chunk.SetPositionAndRotation(finishedPosition,finishedRotation);
            Capture("05-hive-confirmed");
            for (int i = 0; i < 20; i++) Tick();
            record = false;
            fx.DepositPose(out var end,out var endRotation);
            Check(oldHoney!=null && Vector3.Distance(end,oldHoney.transform.TransformPoint(oldHoney.localBounds.center))<0.0001f
                && Quaternion.Angle(endRotation,oldHoney.transform.rotation)<0.01f,
                "insertion seats at the original art centre and roll rather than floating at the interaction point");
            Vector3 actualSize = Vector3.Scale(source.honeyMesh.bounds.size,fx.NectarScale("stored"));
            Vector3 expectedSize = Vector3.Scale(oldHoney.localBounds.size,oldHoney.transform.lossyScale);
            Check(Vector3.Distance(actualSize,expectedSize)<0.0001f,"seated nectar fits the existing cell width height and depth");
            hive.transform.position += Vector3.right*0.03f; hive.transform.rotation = Quaternion.Euler(5f,7f,2f)*hive.transform.rotation;
            fx.DepositPose(out end,out endRotation);
            Check(Vector3.Distance(end,oldHoney.transform.TransformPoint(oldHoney.localBounds.center))<0.0001f
                && Quaternion.Angle(endRotation,oldHoney.transform.rotation)<0.01f, "moving hive art remains the authority for the seated target");
            fx.Confirm("pick-cancel-check","pick",-1,false,chunk.position,chunk.rotation);
            fx.Confirm("drop-cancel-check","drop",-1,false,chunk.position,chunk.rotation);
            Check(!fx.PickupAnimating,"dropping nectar cancels a pending visual lift");
            Check(owner.GetComponentsInChildren<Collider>(true).Length==0,"liquid previews and feedback add no physics or raycast targets");
            fx.Configure(0f,0f,0f); Tick(); Check(fx.VisibleMotes == 0, "visual intensity zero hides all powder sprites");
            fx.Configure(1f,0f,0f); for (int i = 0; i < 300; i++) Tick();
            Check(fx.VisibleMotes <= 160 && !fx.CollectionAnimating && !fx.DepositAnimating, "feedback remains bounded after long idle");
            var headLocal = camera.transform.localPosition; var headRotation = camera.transform.localRotation;
            Tick(); Check(camera.transform.localPosition == headLocal && Quaternion.Angle(camera.transform.localRotation,headRotation)<0.001f, "feedback never writes camera pose");
            var petals = a.GetComponentsInChildren<Transform>().Where(x=>x.name.StartsWith("Petal_")&&x.name.EndsWith("_0")).ToArray();
            var rest = petals.Select(x=>x.localRotation).ToArray(); var headPosition = a.localPosition; var headRest = a.localRotation;
            var response = Activator.CreateInstance(responseType,new object[]{a});
            responseType.GetMethod("Apply").Invoke(response,new object[]{1f,0.4f,0.8f,0.2f});
            Check(petals.Length>0 && petals.Where((x,i)=>Quaternion.Angle(x.localRotation,rest[i])>0.01f).Any(), "existing daisy bones respond without new body geometry");
            responseType.GetMethod("Restore").Invoke(response,null);
            Check(petals.Select((x,i)=>Quaternion.Angle(x.localRotation,rest[i])<0.01f).All(x=>x)
                && a.localPosition==headPosition && Quaternion.Angle(a.localRotation,headRest)<0.01f, "flower feedback restores the sampled animation pose");

            // Exercise the actual pilot ACK route as well, with a private in-memory state.
            var pilotObject = Obj("Pilot ACK check"); pilotObject.SetActive(false);
            var pilot = pilotObject.AddComponent<BeeHoneyPilot>(); pilot.enabled = false;
            pilot.beeRoot = owner.transform; pilot.rightHand = hand; pilot.flowerAHead = a; pilot.flowerBHead = b; pilot.slot = slot;
            typeof(BeeHoneyPilot).GetField("honeyFeedback",Private).SetValue(pilot,fx);
            typeof(BeeHoneyPilot).GetField("chunk",Private).SetValue(pilot,chunk);
            var confirmed = new JObject { ["pollen_source"] = 1, ["pollinated"] = new JArray(false,true),["chunk_state"]="stored",["stored"]=1 };
            typeof(BeeHoneyPilot).GetProperty("State").SetValue(pilot,confirmed);
            typeof(BeeHoneyPilot).GetField("pendingRequest",Private).SetValue(pilot,new JObject { ["event_id"]="pilot-deposit",["kind"]="deposit" });
            string stateBefore = confirmed.ToString(Formatting.None);
            typeof(BeeHoneyPilot).GetMethod("Accepted",Private).Invoke(pilot,new object[]{"deposit",-1});
            Check(fx.DepositAnimating && confirmed.ToString(Formatting.None)==stateBefore, "real pilot accepted route starts presentation without changing GAMA state");
            typeof(BeeHoneyPilot).GetField("visitedContact",Private).SetValue(pilot,0);
            var visit = new JObject { ["event_id"]="already-visited",["kind"]="visit",["flower"]=0 };
            typeof(BeeHoneyPilot).GetField("pendingRequest",Private).SetValue(pilot,visit);
            typeof(BeeHoneyPilot).GetMethod("CompleteRequest",Private).Invoke(pilot,new object[]{visit,new JObject { ["accepted"]=false,["reason"]="same_visit" }});
            Check(!pilot.Busy && (int)typeof(BeeHoneyPilot).GetField("visitedContact",Private).GetValue(pilot)==0,
                "restored same_visit response releases the queue without repeatedly rearming the visit");
            typeof(BeeHoneyPilot).GetField("pendingRequest",Private).SetValue(pilot,visit);
            typeof(BeeHoneyPilot).GetMethod("CompleteRequest",Private).Invoke(pilot,new object[]{visit,new JObject { ["accepted"]=false,["reason"]="too_far_from_flower" }});
            Check(!pilot.Busy && (int)typeof(BeeHoneyPilot).GetField("visitedContact",Private).GetValue(pilot)==-1
                && confirmed.ToString(Formatting.None)==stateBefore,
                "genuine rejected visit can still retry and never grants pollen");
            // Prevent the inactive fixture from owning the shared feedback during cleanup.
            typeof(BeeHoneyPilot).GetField("honeyFeedback",Private).SetValue(pilot,null);
            Check(EditorJsonUtility.ToJson(source)==sourceJson && sourceScene.isDirty==wasDirty, "saved scene component and dirty state remain unchanged");
            Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("HoneyFeedback"))
                && !ShaderUtil.ShaderHasError(Resources.Load<Shader>("HoneyNectar")), "presentation shaders render without compiler errors");
            File.WriteAllText(Folder+"/validation.json",JsonConvert.SerializeObject(new {passed=true,count=checks.Count,checks,finishedUtc=DateTime.UtcNow.ToString("O")},Formatting.Indented));
            if (renderMotion) File.WriteAllText(Folder+"/motion.json",JsonConvert.SerializeObject(new {folder=Folder+"/"+motionFolder,frames,frameMilliseconds=60,phases}));
            return "Honey presentation: " + checks.Count + " checks passed; collect, lift, carry and actual hive rendered.";
        } catch (Exception ex) {
            File.WriteAllText(Folder+"/validation.json",JsonConvert.SerializeObject(new {passed=false,count=checks.Count,checks,error=ex.ToString()},Formatting.Indented)); throw;
        } finally {
            fx?.Dispose();
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
