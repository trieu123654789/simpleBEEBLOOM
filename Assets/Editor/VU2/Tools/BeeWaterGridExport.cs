using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using LetItBee.HoneyPilot;

public static class BeeWaterGridExport
{
    public const string ExportPath = "Experiments/LetItBee-HoneyPilot/Water/gameplay-domain-survey.json";
    public const string UnityProfilePath = "Assets/Settings/LetItBee/Water/gameplay-domain-survey.json";
    [MenuItem("Let It Bee/Water/Export Current Water Survey")]
    public static void ExportCurrent() { Debug.Log(Export(8f)); }
    [MenuItem("Let It Bee/Water/Select Water Diagnostics")]
    public static void SelectDiagnostics()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Set up diagnostics in Edit Mode.");
        var pilot=UnityEngine.Object.FindAnyObjectByType<BeeHoneyPilot>();
        if(pilot==null) throw new InvalidOperationException("No Honey Pilot in the current scene.");
        var grid=pilot.GetComponent<BeeWaterGrid>();
        if(grid==null) {
            grid=Undo.AddComponent<BeeWaterGrid>(pilot.gameObject);
            grid.configuration=AssetDatabase.LoadAssetAtPath<TextAsset>(UnityProfilePath);
            grid.connectToGama=false; grid.runTransport=false;
            EditorUtility.SetDirty(grid);
        }
        Selection.activeGameObject=pilot.gameObject;
    }

    public static string Export(float cellSize)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Export in Edit Mode.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var surfaces = UnityEngine.Object.FindObjectsByType<BeeEnvironmentSurface>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)
            .Where(s=>s.gameObject.scene==scene).ToArray();
        var ground = surfaces.Where(s=>s.kind==BeeEnvironmentSurface.SurfaceKind.Ground)
            .SelectMany(s=>s.GetComponentsInChildren<Collider>()).Where(c=>c.enabled && !c.isTrigger).ToArray();
        var water = surfaces.Where(s=>s.kind==BeeEnvironmentSurface.SurfaceKind.Water)
            .SelectMany(s=>s.GetComponentsInChildren<Renderer>()).Where(r=>r.enabled).ToArray();
        if (ground.Length==0 || water.Length!=1) throw new InvalidOperationException("Expected explicit ground colliders and one active water renderer.");
        Bounds bounds = ground[0].bounds;
        foreach(var c in ground) bounds.Encapsulate(c.bounds);
        float surfaceY = water[0].bounds.center.y;
        if (cellSize<=0) throw new ArgumentOutOfRangeException(nameof(cellSize));
        float x0=Mathf.Floor(bounds.min.x/cellSize)*cellSize, z0=Mathf.Floor(bounds.min.z/cellSize)*cellSize;
        int width=Mathf.CeilToInt((bounds.max.x-x0)/cellSize),height=Mathf.CeilToInt((bounds.max.z-z0)/cellSize);
        if(width*height>4096) throw new InvalidOperationException("Survey exceeds 4096 cells; increase Cell Size.");
        var depths=new JArray(); var salinity=new JArray(); var unknown=new JArray(); var shoreline=new JArray();
        int wet=0;
        // Five samples per cell. Conservative mask: any dry/unknown sample blocks that cell.
        // This does not invent depth beneath holes or outside the artist ground mesh.
        Vector2[] offsets={Vector2.zero,new Vector2(-.45f,-.45f),new Vector2(.45f,-.45f),new Vector2(-.45f,.45f),new Vector2(.45f,.45f)};
        Physics.SyncTransforms();
        for(int i=0;i<width*height;i++) {
            Vector3 center=BeeWaterGrid.CellCenter(i,new Vector2(x0,z0),cellSize,width,surfaceY);
            bool known=true, allWet=true,anyWet=false; float minimumDepth=float.PositiveInfinity;
            foreach(var offset in offsets) {
                var origin=new Vector3(center.x+offset.x*cellSize,bounds.max.y+10f,center.z+offset.y*cellSize);
                float top=float.NegativeInfinity;
                foreach(var collider in ground) if(collider.Raycast(new Ray(origin,Vector3.down),out var hit,bounds.size.y+20f)) top=Mathf.Max(top,hit.point.y);
                bool covered=water[0].bounds.min.x<=origin.x && origin.x<=water[0].bounds.max.x && water[0].bounds.min.z<=origin.z && origin.z<=water[0].bounds.max.z;
                if(float.IsNegativeInfinity(top)||!covered) { known=false; allWet=false; continue; }
                float depth=surfaceY-top; anyWet |= depth>.02f; allWet &= depth>.02f;
                minimumDepth=Mathf.Min(minimumDepth,depth);
            }
            if(!known) unknown.Add(i);
            if(anyWet && !allWet) shoreline.Add(i);
            float accepted=known&&allWet?minimumDepth:0f;
            if(accepted>0) wet++;
            depths.Add(Math.Round(accepted,4)); salinity.Add(0.0);
        }
        var config=new JObject {
            ["schema"]="letitbee-water-grid-v1",["domain_id"]="gameplay-survey-"+AssetDatabase.AssetPathToGUID(scene.path),
            ["width"]=width,["height"]=height,["cell_size_m"]=cellSize,["origin_xz"]=new JArray(x0,z0),["surface_y"]=surfaceY,
            ["diffusivity_m2_s"]=0.0,["depth_m"]=depths,["salinity_g_l"]=salinity,["faces"]=new JArray(),
            ["review_required"]=true,["unknown_cells"]=unknown,["shoreline_cells"]=shoreline,
            ["scene"]=scene.path,["description"]="Geometry survey only: metre-per-world-unit assumption; dry/unknown cells blocked; zero prescribed flow and salinity. Review shoreline, units and boundary sources before enabling simulation."
        };
        Directory.CreateDirectory(Path.GetDirectoryName(ExportPath));
        string json=config.ToString(Formatting.Indented)+"\n";
        File.WriteAllText(ExportPath,json);
        Directory.CreateDirectory(Path.GetDirectoryName(UnityProfilePath));
        File.WriteAllText(UnityProfilePath,json);
        AssetDatabase.ImportAsset(UnityProfilePath);
        return string.Format("Water survey: {0}x{1}, {2} wet, {3} unknown, {4} shoreline. Scene and meshes unchanged. {5}",width,height,wet,unknown.Count,shoreline.Count,ExportPath);
    }
}
