using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Read-only scene capture. This never moves objects or changes artist meshes.
public static class BeeObservationMapExport
{
    public const string OutputPath = "Experiments/LetItBee-HoneyPilot/Water/gameplay-observation-map.json";
    [MenuItem("Let It Bee/Water/Export Observation Map Background")]
    public static void Export()
    {
        string json = Capture();
        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        File.WriteAllText(OutputPath,json);
        Debug.Log("Observation map exported; scene unchanged: " + OutputPath);
    }
    public static string Capture()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Export the authored scene in Edit Mode.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var surfaces=UnityEngine.Object.FindObjectsByType<BeeEnvironmentSurface>(FindObjectsSortMode.None).Where(x=>x.gameObject.scene==scene).ToArray();
        var ground=surfaces.Where(x=>x.kind==BeeEnvironmentSurface.SurfaceKind.Ground).SelectMany(x=>x.GetComponentsInChildren<Collider>()).Where(x=>x.enabled&&!x.isTrigger).Distinct().ToArray();
        var water=surfaces.Where(x=>x.kind==BeeEnvironmentSurface.SurfaceKind.Water).SelectMany(x=>x.GetComponentsInChildren<Renderer>()).Where(x=>x.enabled).Distinct().ToArray();
        if(ground.Length==0||water.Length!=1) throw new InvalidOperationException("Expected classified ground and exactly one visible water plane.");
        Bounds bounds=ground[0].bounds; foreach(var c in ground) bounds.Encapsulate(c.bounds);
        const float dx=2f;
        float x0=Mathf.Floor(bounds.min.x/dx)*dx,z0=Mathf.Floor(bounds.min.z/dx)*dx;
        int width=Mathf.CeilToInt((bounds.max.x-x0)/dx),height=Mathf.CeilToInt((bounds.max.z-z0)/dx);
        if(width*height>100000) throw new InvalidOperationException("Observation map exceeds capture limit.");
        Physics.SyncTransforms();
        var runs=new JArray(); int wet=0,land=0,unknown=0;
        for(int row=0;row<height;row++) {
            int start=0,last=-1;
            for(int col=0;col<=width;col++) {
                int kind=-2;
                if(col<width) {
                    float x=x0+(col+.5f)*dx,z=z0+(row+.5f)*dx,top=float.NegativeInfinity;
                    var ray=new Ray(new Vector3(x,bounds.max.y+10,z),Vector3.down);
                    foreach(var c in ground) if(c.Raycast(ray,out var hit,bounds.size.y+20)) top=Mathf.Max(top,hit.point.y);
                    bool covered=x>=water[0].bounds.min.x&&x<=water[0].bounds.max.x&&z>=water[0].bounds.min.z&&z<=water[0].bounds.max.z;
                    kind=float.IsNegativeInfinity(top)?0:(covered&&top<water[0].bounds.center.y-.02f?2:1);
                    if(kind==2)wet++; else if(kind==1)land++;else unknown++;
                }
                // Split at transport-grid boundaries: each water run samples one coarse cell.
                if(col>0&&(kind!=last||col==width||(last==2&&Mathf.Abs((x0+col*dx)/8f-Mathf.Round((x0+col*dx)/8f))<.001f))) {
                    if(last!=0) runs.Add(new JObject{["kind"]=last==2?"water":"land",["center"]=new JArray(x0+(start+col)*.5f*dx,z0+(row+.5f)*dx),["size"]=new JArray((col-start)*dx,dx)});
                    start=col;
                }
                last=kind;
            }
        }
        var markers=new JArray(); var seen=new HashSet<Transform>();
        foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) {
            if(t.gameObject.scene!=scene)continue;
            Transform root=null;string kind=null;
            if(t.name=="NipaRig"){root=t.parent;kind="nipa";}
            else if(t.name.StartsWith("CayDua")||t.name.StartsWith("Caydua")||t.name.StartsWith("coconut_new")||t.name=="SM_MainHouse"||t.name=="SM_House2") {root=t;kind=t.name.StartsWith("SM_")?"house":"palm";}
            if(root==null||!seen.Add(root))continue;
            var renderers=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray(); if(renderers.Length==0)continue;
            var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            var markerPosition=kind=="nipa"?root.position:b.center;
            markers.Add(new JObject{["kind"]=kind,["label"]=root.name,["position"]=new JArray(markerPosition.x,markerPosition.z),["size"]=new JArray(b.size.x,b.size.z)});
        }
        return new JObject{["schema"]="letitbee-observation-map-v1",["scene"]=scene.path,["scene_guid"]=AssetDatabase.AssetPathToGUID(scene.path),["captured_at"]=DateTime.UtcNow.ToString("O"),["unsaved_scene"]=scene.isDirty,["cell_size_m"]=dx,["bounds"]=new JArray(x0,z0,x0+width*dx,z0+height*dx),["wet_samples"]=wet,["land_samples"]=land,["unknown_samples"]=unknown,["tiles"]=runs,["landmarks"]=markers}.ToString(Formatting.None);
    }
}
