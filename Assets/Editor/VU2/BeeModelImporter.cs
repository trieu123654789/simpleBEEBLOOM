// =============================================================================
// BeeModelImporter - Ghep model ong THAT vao rig bay.
//
// BO CUNG (menu): "VU2/Bee/Import Bee Model From Art Folder"
//
// CACH DUNG
//   1. Tai model ong (.fbx) vao Assets/Art/Bee/  -> Unity tu import.
//   2. Chay menu nay. Tool tim model, chuan hoa don vi + huong, va tao
//      pivot canh de BeeFlapping xoay quanh vai.
//
// YEU CAU MODEL
//   - Poly < 2000, texture <= 1024 (project build cho Quest).
//   - Tool KHONG doan hinh dong. Canh duoc nhan biet qua ten mesh
//     chua "wing" hoac "canh". Neu khong co, tao pivot trong Blender.
// =============================================================================
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BeeModelImporter
{
    private const string BeeArtDir = "Assets/Art/Bee";
    private const string BeePrefabPath = "Assets/Prefabs/VU2/Bee_Placeholder.prefab";

    private static readonly string[] WingHints = { "wing", "canh", "flap" };

    [MenuItem("VU2/Bee/Import Bee Model From Art Folder", false, 12)]
    public static void ImportBeeModel()
    {
        if (!Directory.Exists(BeeArtDir))
        {
            Debug.LogError("[BeeModelImporter] Khong co thu muc " + BeeArtDir);
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { BeeArtDir });
        if (guids.Length == 0)
        {
            Debug.LogError("[BeeModelImporter] Khong tim thay model .fbx trong " + BeeArtDir +
                           ". Hay tai model ong vao thu muc nay truoc.");
            return;
        }

        string modelPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelAsset == null)
        {
            Debug.LogError("[BeeModelImporter] Khong load duoc model: " + modelPath);
            return;
        }

        Debug.Log("[BeeModelImporter] Dung model: " + modelPath +
                  " (" + CountRenderers(modelAsset) + " renderer)");

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        instance.name = "BeeModelRaw";

        Transform body = FindDeep(instance.transform, "Body") ?? instance.transform;
        NormalizeModel(body);

        // Boc Body vao node "BeeVisual" de BeeCameraRig co the dich chuyen
        // rieng phan hinh anh ma khong keo theo BeeRoot (vong lap vat ly).
        Transform visual = FindDeep(instance.transform, "BeeVisual");
        if (visual == null)
        {
            GameObject visualGo = new GameObject("BeeVisual");
            visualGo.transform.SetParent(instance.transform, false);
            body.SetParent(visualGo.transform, true);
            visual = visualGo.transform;
        }

        Transform[] wings = SetupWingPivots(body);
        if (wings == null)
        {
            Debug.LogWarning("[BeeModelImporter] KHONG tim thay mesh canh. Bay van chay duoc " +
                             "nhung khong co canh vo. Hay dat ten mesh canh chua 'Wing' hoac 'Canh'.");
        }

        var flapping = instance.AddComponent<BeeFlapping>();
        flapping.body = body;
        flapping.wingLeft = wings != null && wings.Length > 0 ? wings[0] : null;
        flapping.wingRight = wings != null && wings.Length > 1 ? wings[1] : null;

        instance.AddComponent<BeeController>();
        instance.AddComponent<BeeCameraRig>();
        instance.name = "BeeRoot";

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, BeePrefabPath);
        Object.DestroyImmediate(instance);

        Selection.activeGameObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        AssetDatabase.SaveAssets();

        Debug.Log("[BeeModelImporter] Da tao prefab ong: " + BeePrefabPath +
                  ". Tiep theo: VU2/Bee/Setup Bee Flight Scene");
    }
// =========================================================================
    // SetupWingPivots - Bien mesh canh thanh PIVOT de BeeFlapping xoay quanh vai.
    //   Giu nguyen mesh, chi them transform rong o giua mesh roi reparent vao.
    // =========================================================================
    private static Transform[] SetupWingPivots(Transform body)
    {
        Renderer[] all = body.GetComponentsInChildren<Renderer>(true);

        Renderer wingL = null, wingR = null;
        foreach (Renderer r in all)
        {
            if (!IsWingName(r.gameObject.name)) continue;

            // Trai/phai theo vi tri X so voi chinh giua than.
            if (r.bounds.center.x <= 0f) { if (wingL == null) wingL = r; }
            else { if (wingR == null) wingR = r; }
        }

        // Khong duoc -> thu lai theo duoi "_l"/"_r" trong ten.
        if (wingL == null || wingR == null)
        {
            foreach (Renderer r in all)
            {
                string n = r.gameObject.name.ToLower();
                if (!IsWingName(n)) continue;
                if (n.Contains("_l") && wingL == null) wingL = r;
                if (n.Contains("_r") && wingR == null) wingR = r;
            }
        }

        if (wingL == null || wingR == null) return null;

        return new[] { CreatePivotAtMeshCentre(wingL), CreatePivotAtMeshCentre(wingR) };
    }

    private static bool IsWingName(string name)
    {
        string n = name.ToLower();
        foreach (string h in WingHints)
        {
            if (n.Contains(h)) return true;
        }
        return false;
    }

    /// <summary>Tao GameObject rong lam pivot o giua mesh canh roi reparent mesh vao.</summary>
    private static Transform CreatePivotAtMeshCentre(Renderer wing)
    {
        Transform mesh = wing.transform;

        GameObject pivot = new GameObject(mesh.name + "_Pivot");
        pivot.transform.SetParent(mesh.parent, false);
        pivot.transform.position = wing.bounds.center;

        mesh.SetParent(pivot.transform, true);

        // Doi ten pivot thanh chuan "Wing_L"/"Wing_R" ma script can.
        string n = mesh.name.ToUpper();
        pivot.name = n.Contains("_L") ? "Wing_L" : (n.Contains("_R") ? "Wing_R" : n);
        mesh.name = "Mesh";

        return pivot.transform;
    }

    // =========================================================================
    // NormalizeModel - Dua model ve dung don vi va huong bay chuan.
    //   Ong that dai ~1.5cm; tool chuan hoa ve ~1.5 don vi de sua offset
    //   trong Inspector cho de.
    // =========================================================================
    private static void NormalizeModel(Transform body)
    {
        Renderer[] renderers = body.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }

        // Center vao goc toa do.
        body.position -= b.center;

        // Scale cho do lon hop ly.
        float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (maxDim > 0.0001f && maxDim < 5f)
        {
            body.localScale *= 1.5f / maxDim;
        }
        else
        {
            Debug.LogWarning("[BeeModelImporter] Kich thuoc model bat thuong (" +
                             maxDim.ToString("0.00") + "). Giu nguyen scale, " +
                             "hay chinh tay trong Inspector.");
        }
    }

    // =========================================================================
    // Helpers / Tien ich
    // =========================================================================
    private static Transform FindDeep(Transform root, string childName)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name == childName) return c;
            Transform d = FindDeep(c, childName);
            if (d != null) return d;
        }
        return null;
    }

    private static int CountRenderers(GameObject go)
    {
        return go.GetComponentsInChildren<Renderer>(true).Length;
    }
}
