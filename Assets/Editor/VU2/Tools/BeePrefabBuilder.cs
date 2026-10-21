// =============================================================================
// BeePrefabBuilder - Tao ong TAM bang primitive de test nhanh trong Editor.
//
// BO CUNG (menu): "VU2/Bee/Create Placeholder Bee Prefab"
//
// Script chi can 3 transform ten dung: Body, Wing_L, Wing_R. Do vay mot con ong
// tao tu Capsule/Sphere/Quad la du de chay thu toan bo cam giac bay truoc khi
// co model that. Khi co model that, chi can giu nguyen ten 3 transform do.
//
// Scale tham kho (ong that dai ~1.5cm, nhung o day dung co 1 don vi = 5cm de
// de canh hinh va de sua offset trong Inspector).
// =============================================================================
using UnityEditor;
using UnityEngine;

public static class BeePrefabBuilder
{
    private const string PrefabDir = "Assets/Prefabs/VU2";
    private const string PrefabPath = PrefabDir + "/Bee_Placeholder.prefab";

    [MenuItem("VU2/Bee/Create Placeholder Bee Prefab", false, 10)]
    public static void CreatePlaceholderBeePrefab()
    {
        System.IO.Directory.CreateDirectory(PrefabDir);

        GameObject root = new GameObject("BeeRoot");

        // BeeVisual: node RIENG chua than + canh. Phai tach khoi BeeRoot vi
        // BeeCameraRig se dich chuyen BeeVisual theo camera o first-person.
        // Neu de Body lam con truc tiep cua BeeRoot thi rig se keo luon ca
        // BeeRoot -> pha vong lap vat ly cua BeeController.
        GameObject visual = new GameObject("BeeVisual");
        visual.transform.SetParent(root.transform, false);

        // --- Body / than ong (dung Sphere de de bo doi huong dau) ---
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(visual.transform, false);
        body.transform.localScale = new Vector3(0.12f, 0.11f, 0.18f);

        // --- Head / dau ong ---
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(body.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.05f, 0.10f);
        head.transform.localScale = new Vector3(0.09f, 0.08f, 0.09f);

        // --- Wings / CANH ---
        // Pivot dat o vai (xac) nen xoay quanh truc Z se vo nhu that.
        CreateWing(body.transform, "Wing_L", new Vector3(-0.05f, 0.05f, 0f));
        CreateWing(body.transform, "Wing_R", new Vector3(0.05f, 0.05f, 0f));

        // --- Scripts ---
        // BeeCameraRig cung nam tren BeeRoot: no doc yaw/pitch/bank tu BeeController
        // roi dich CHUYEN XR Origin (khong bao gio ghi de camera da track).
        var flapping = root.AddComponent<BeeFlapping>();
        flapping.body = body.transform;
        flapping.wingLeft = body.transform.Find("Wing_L");
        flapping.wingRight = body.transform.Find("Wing_R");

        root.AddComponent<BeeController>();
        root.AddComponent<BeeCameraRig>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        Selection.activeGameObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("[BeePrefabBuilder] Da tao ong tam tai: " + PrefabPath);
    }

    private static void CreateWing(Transform parent, string name, Vector3 shoulder)
    {
        Transform pivot = new GameObject(name).transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = shoulder;
        // Canh ban ra phia ngoai: lui ra sau theo Z de khong lam mau vao mat.
        pivot.localRotation = Quaternion.Euler(0f, 0f, name == "Wing_L" ? 20f : -20f);

        GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Quad);
        wing.name = "Mesh";
        wing.transform.SetParent(pivot, false);
        // Quad mac dinh 1 don vi -> scale nho lai cho hien thuc.
        wing.transform.localScale = new Vector3(0.16f, 0.09f, 1f);
        // Xoay -90 do X de mat canh HUONG LEN. Camera nhin tu tren xuong,
        // nen canh phai mat len moi thay duoc (mat xuong se bi backface culling).
        wing.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        // Canh mo phia lui (ra sau) chung voi huong bay.
        wing.transform.localPosition = new Vector3(0f, 0f, -0.05f);

        Object.DestroyImmediate(wing.GetComponent<Collider>());
    }
}