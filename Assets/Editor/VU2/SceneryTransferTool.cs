// =============================================================================
// SceneryTransferTool - Chuyen moi truong / canh quan tu SCN_VU2_Level1_New
// sang scene dang mo (vd: SCN_BeeBloom). BO phan gameplay / nhan vat / do vat.
//
// MENU
//   VU2/Transfer/Preview Scenery From Level1   -> xem se chuyen gi (khong thay doi)
//   VU2/Transfer/Copy Scenery To Active Scene  -> clone moi truong vao scene hien tai
//   VU2/Transfer/Copy Scenery + Lighting       -> kem skybox / ambient / fog / anh sang
//
// MAC DINH CHUYEN (moi truong - canh quan)
//   ENVIRONMENT : Terrain, dike, tuong bao, thoi tiet, cau, ao, TeleportAreas...
//   Scenery, VFX, AUDIO
//
// KHONG CHUYEN (gameplay / nhan vat / do vat / UI / game cu)
//   INTERACTABLES, CHARACTERS, SYSTEM, UI, GameManager
//
// LUU Y
//   - Script legacy VU1 (rotateTaget, waterSound...) bi go bo khi clone.
//   - Den cua scene dich bi TAT de tranh 2 nguon sang chieu;
//     phan intensity/mau/huong cua Den Level1 duoc copy sang Den scene dich.
//   - Ctrl+Z de undo, nho Save Scene sau khi chay.
// =============================================================================
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneryTransferTool
{
    private const string SourceScenePath = "Assets/Scenes/VU2/SCN_VU2_Level1_New.unity";

    private static readonly string[] CopyRoots = { "ENVIRONMENT", "Scenery", "VFX", "AUDIO" };

    private static readonly string[] StripScripts =
    {
        "rotateTaget", "waterSound", "BoatController", "DayToNightSky"
    };

    // ===================== PREVIEW =====================
    [MenuItem("VU2/Transfer/Preview Scenery From Level1", false, 320)]
    public static void Preview()
    {
        Scene src = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
        Debug.Log("[SceneryTransfer] PREVIEW - scene nguon: " + src.path);
        foreach (string rootName in CopyRoots)
        {
            GameObject r = FindRoot(src, rootName);
            if (r == null) { Debug.LogWarning("  [THIEU] root '" + rootName + "' khong ton tai."); continue; }
            Debug.Log(string.Format("  [COPY] {0}: {1} object | scripts: {2}",
                rootName, CountAll(r.transform),
                string.Join(", ", ScriptsOf(r).Distinct().ToArray())));
        }
        Debug.Log("[SceneryTransfer] KHONG chuyen: INTERACTABLES, CHARACTERS, SYSTEM, UI, GameManager");
        EditorSceneManager.CloseScene(src, true);
    }

    // ===================== COPY =====================
    [MenuItem("VU2/Transfer/Copy Scenery To Active Scene", false, 321)]
    public static void CopyScenery()
    {
        DoCopy(false);
    }

    [MenuItem("VU2/Transfer/Copy Scenery + Lighting", false, 322)]
    public static void CopySceneryAndLighting()
    {
        DoCopy(true);
    }

    private static void DoCopy(bool copyLighting)
    {
        Scene dst = SceneManager.GetActiveScene();
        if (!dst.isLoaded || string.IsNullOrEmpty(dst.path))
        {
            EditorUtility.DisplayDialog("Scenery Transfer",
                "Scene hien tai chua duoc luu.\nMo va SAVE scene dich (vd: SCN_BeeBloom) truoc khi chuyen.", "OK");
            return;
        }

        Scene src = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);

        LightSnapshot lightSnap = null;
        if (copyLighting)
        {
            SceneManager.SetActiveScene(src);
            lightSnap = LightSnapshot.Capture(src);
            SceneManager.SetActiveScene(dst);
        }

        int total = 0;
        foreach (string rootName in CopyRoots)
        {
            GameObject root = FindRoot(src, rootName);
            if (root == null) { Debug.LogWarning("[SceneryTransfer] Bo qua (khong co root): " + rootName); continue; }

            GameObject copy = Object.Instantiate(root);
            copy.name = rootName;
            SceneManager.MoveGameObjectToScene(copy, dst);
            Undo.RegisterCreatedObjectUndo(copy, "Transfer Scenery: " + rootName);

            int stripped = StripLegacy(copy);
            int lights = DisableLights(copy);
            total += CountAll(copy.transform);
            Debug.Log(string.Format("[SceneryTransfer] '{0}': +{1} object | go {2} script VU1 | tat {3} Light",
                rootName, CountAll(copy.transform), stripped, lights));
        }

        if (lightSnap != null) lightSnap.ApplyTo(dst);

        EditorSceneManager.MarkSceneDirty(dst);
        EditorSceneManager.CloseScene(src, true);
        Debug.Log("[SceneryTransfer] Xong: +" + total + " object moi truong trong '" + dst.path +
                  "'. Bam Ctrl+S de luu scene.");
    }
    // ===================== LIGHTING SNAPSHOT =====================
    private class LightSnapshot
    {
        public Material skybox;
        public UnityEngine.Rendering.AmbientMode ambientMode;
        public Color ambientSky, ambientEquator, ambientGround;
        public float ambientIntensity;
        public bool fog; public Color fogColor; public FogMode fogMode;
        public bool srcHasDirLight, dirLightOn, dirShadows;
        public Vector3 dirEuler; public Color dirColor; public float dirIntensity;

        public static LightSnapshot Capture(Scene src)
        {
            LightSnapshot s = new LightSnapshot
            {
                skybox = RenderSettings.skybox,
                ambientMode = RenderSettings.ambientMode,
                ambientSky = RenderSettings.ambientSkyColor,
                ambientEquator = RenderSettings.ambientEquatorColor,
                ambientGround = RenderSettings.ambientGroundColor,
                ambientIntensity = RenderSettings.ambientIntensity,
                fog = RenderSettings.fog,
                fogColor = RenderSettings.fogColor,
                fogMode = RenderSettings.fogMode
            };
            Light dir = LightsOf(src).FirstOrDefault(l => l.type == LightType.Directional);
            if (dir != null)
            {
                s.srcHasDirLight = true;
                s.dirEuler = dir.transform.eulerAngles;
                s.dirColor = dir.color;
                s.dirIntensity = dir.intensity;
                s.dirLightOn = dir.enabled;
                s.dirShadows = dir.shadows != LightShadows.None;
            }
            return s;
        }

        public void ApplyTo(Scene dst)
        {
            SceneManager.SetActiveScene(dst);
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.fog = fog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;

            if (srcHasDirLight)
            {
                Light dir = LightsOf(dst).FirstOrDefault(l => l.type == LightType.Directional);
                if (dir != null)
                {
                    dir.transform.rotation = Quaternion.Euler(dirEuler);
                    dir.color = dirColor;
                    dir.intensity = dirIntensity;
                    dir.enabled = dirLightOn;
                    dir.shadows = dirShadows ? LightShadows.Soft : LightShadows.None;
                    Undo.RecordObject(dir, "Transfer Lighting");
                    EditorUtility.SetDirty(dir);
                    Debug.Log("[SceneryTransfer] Da chuyen thong so Den chinh sang scene dich.");
                }
            }
            Debug.Log(string.Format("[SceneryTransfer] Lighting: skybox='{0}', ambient={1} ({2}), fog={3}",
                skybox != null ? skybox.name : "(null)", ambientMode, ambientIntensity, fog));
        }
    }

    // ===================== HELPERS =====================
    private static IEnumerable<Light> LightsOf(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Light l in root.GetComponentsInChildren<Light>(true))
                if (l != null) yield return l;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().FirstOrDefault(g => g.name == name);
    }

    private static int CountAll(Transform t)
    {
        int n = 1;
        for (int i = 0; i < t.childCount; i++) n += CountAll(t.GetChild(i));
        return n;
    }

    private static List<string> ScriptsOf(GameObject root)
    {
        List<string> list = new List<string>();
        foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb != null) list.Add(mb.GetType().Name);
        return list;
    }

    private static int StripLegacy(GameObject root)
    {
        int removed = 0;
        foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            string n = mb.GetType().Name;
            foreach (string s in StripScripts)
            {
                if (n == s) { Undo.DestroyObjectImmediate(mb); removed++; break; }
            }
        }
        return removed;
    }

    private static int DisableLights(GameObject root)
    {
        int n = 0;
        foreach (Light l in root.GetComponentsInChildren<Light>(true))
        {
            if (l == null) continue;
            Undo.RecordObject(l, "Disable Copied Light");
            l.enabled = false;
            EditorUtility.SetDirty(l);
            n++;
        }
        return n;
    }
}