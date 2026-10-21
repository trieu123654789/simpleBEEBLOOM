// =============================================================================
// BeeFlightSceneSetup - Dung nguyen scene bay ong VR hoan chinh, 1 thao tac.
//
// BO CUNG (menu): "VU2/Bee/Setup Bee Flight Scene"
//
//   1. Tao XR Origin chuan cua XRI 2.5.2 (dung chinh API cua package nen camera
//      + tracked pose driver + action asset deu dung chuan).
//   2. Tao prefab ong tam (neu chua co).
//   3. Ghep BeeRoot vao scene va noi day du tham chieu.
//   4. Tat locomotri XRI (ContinuousMove/SnapTurn/Teleportation) vi dang bay.
//
// LUU Y: nen chay khi scene TRONG, de moi them vao dung scene dang lam viec.
// =============================================================================
using Unity.XR.CoreUtils;   // XROrigin
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class BeeFlightSceneSetup
{
    private const string XriActionsPath =
        "Assets/ThirdParty/Samples_XR/XR Interaction Toolkit/2.5.2/Starter Assets/XRI Default Input Actions.inputactions";

    private const string BeePrefabPath = "Assets/Prefabs/VU2/Bee_Placeholder.prefab";

    [MenuItem("VU2/Bee/Setup Bee Flight Scene", false, 11)]
    public static void SetupBeeFlightScene()
    {
        // --- 1. XR Origin (dung API chuan cua XRI de khong sai cau truc) ---
        GameObject xrOrigin = CreateXrOrigin();
        if (xrOrigin == null) return;

        var xro = xrOrigin.GetComponent<XROrigin>();
        Camera xrCamera = xro != null ? xro.Camera : Camera.main;

        // --- 2. Prefab ong ---
        // Tu tao lai neu chua co HOAC neu la ban CU (thieu node "BeeVisual").
        // Ban cu co Body lam con truc tiep cua BeeRoot -> rig se keo luon
        // BeeRoot va lam con ong bien mat.
        GameObject beePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BeePrefabPath);
        if (beePrefab == null || FindDeep(beePrefab.transform, "BeeVisual") == null)
        {
            if (beePrefab != null)
            {
                Debug.LogWarning("[BeeFlightSceneSetup] Prefab ong la ban cu (thieu BeeVisual). " +
                                 "Dang tao lai...");
                AssetDatabase.DeleteAsset(BeePrefabPath);
            }
            BeePrefabBuilder.CreatePlaceholderBeePrefab();
            beePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BeePrefabPath);
        }
        if (beePrefab == null) return;

        // --- 3. The BeeRoot vao scene ---
        GameObject beeRoot = (GameObject)PrefabUtility.InstantiatePrefab(beePrefab);
        beeRoot.name = "BeeRoot";
        PlaceBeeNearPlayer(beeRoot.transform, xrOrigin);

        var rig = beeRoot.GetComponent<BeeCameraRig>();

        // --- 4. Noi tham chieu ---
        if (rig != null)
        {
            rig.playerRig = xrOrigin.transform;
            rig.trackedCamera = xrCamera;
            rig.bee = beeRoot.GetComponent<BeeController>();

            // visualRoot PHAI la node "BeeVisual" (cha cua Body + 2 canh).
            // Khong dung body.parent: neu Body la con truc tiep cua BeeRoot thi
            // body.parent chinh la BeeRoot -> rig se keo luon BeeRoot.
            Transform visual = FindDeep(beeRoot.transform, "BeeVisual");
            rig.visualRoot = visual;
            if (visual == null)
            {
                Debug.LogWarning("[BeeFlightSceneSetup] Khong tim thay 'BeeVisual'. " +
                                 "Hay tao lai prefab ong (VU2/Bee/Create Placeholder Bee Prefab) " +
                                 "hoac keo node chua Body + canh vao o Visual Root.");
            }
        }

        AssignInputActions(beeRoot.GetComponent<BeeController>());

        // --- 5. Tat XRI locomotion ---
        DisableXriLocomotion(xrOrigin);

        // --- 6. Goc nhin con ong nhu anh mau ---
        // FOV rong, nghien nhe khi re, cui xuong khi bay nhanh, giu 2 canh o day.
        if (rig != null)
        {
            rig.ApplyBeePovPreset();

            if (rig.trackedCamera != null)
            {
                rig.trackedCamera.fieldOfView = rig.targetFov;
                EditorUtility.SetDirty(rig.trackedCamera);
            }
            EditorUtility.SetDirty(rig);

            BeeController presetBee = beeRoot.GetComponent<BeeController>();
            if (presetBee != null) EditorUtility.SetDirty(presetBee);
        }

        // Dat ong ngay TREN MAT NUOC ~1.5m: mat nuoc nam ngay duoi tam nhin
        // dung nhu khung hinh anh mau (hoa sen, la sen o nua duoi man hinh).
        SnapBeeToCruiseAltitude(beeRoot.transform);

        // --- 7. Bay sat mat nuoc + HUD ---
        BeeHudSetup.AddAltitudeHoldToBee();   // chan ong lan xuong nuoc
        BeeHudSetup.AddHudToCamera();         // binh mat, la ban, thuoc toc do...

        // --- 7b. Che do test tren PC (khong can kinh VR / tay cam) ---
        // Cho phep bay ngay tren may tinh: WASD + Space + chuot.
        // Script nay tu tat BeeCameraRig/BeeAltitudeHold va tu dieu camera khi Play.
        BeeDesktopTestMode desktopTest = beeRoot.GetComponent<BeeDesktopTestMode>();
        if (desktopTest == null) desktopTest = Undo.AddComponent<BeeDesktopTestMode>(beeRoot);
        desktopTest.bee = beeRoot.GetComponent<BeeController>();
        desktopTest.cam = xrCamera;
        desktopTest.showHelpOverlay = true;
        EditorUtility.SetDirty(desktopTest);

        Undo.RegisterCreatedObjectUndo(beeRoot, "Setup Bee Flight Rig");
        Selection.activeGameObject = xrOrigin;

        Debug.Log("[BeeFlightSceneSetup] Da dung xong rig bay ong (goc nhin con ong " +
                  "theo anh mau).\n" +
                  "   * TEST KHONG CAN VR: nhan Play, dung WASD + Space + chuot " +
                  "(F doi goc ninh, Shift dap canh lien tuc).\n" +
                  "   * Bat che do test: bo tick component 'BeeDesktopTestMode' tren BeeRoot.");
    }

    // =========================================================================
    // SnapBeeToCruiseAltitude - Dat ong o do cao "luot" so voi mat ben duoi.
    //   Khong hardcode vi moi scene VU2 co cao do mat nuoc khac nhau.
    // =========================================================================
    private static void SnapBeeToCruiseAltitude(Transform bee, float cruiseAltitude = 1.5f)
    {
        if (bee == null) return;

        Vector3 origin = bee.position + Vector3.up * (cruiseAltitude + 1f);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 200f, ~0,
            QueryTriggerInteraction.Ignore);

        float surfaceY = float.NegativeInfinity;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            // Bo qua chinh con ong (than/canh co the co collider).
            if (hit.collider.transform == bee || hit.collider.transform.IsChildOf(bee)) continue;
            if (hit.point.y > surfaceY) surfaceY = hit.point.y;
        }

        if (surfaceY == float.NegativeInfinity)
        {
            Debug.LogWarning("[BeeFlightSceneSetup] Khong tim thay mat ben duoi ong. " +
                             "Giu nguyen do cao hien tai.");
            return;
        }

        Vector3 position = bee.position;
        position.y = surfaceY + cruiseAltitude;
        bee.position = position;

        Debug.Log("[BeeFlightSceneSetup] Dat ong o do cao " + cruiseAltitude +
                  "m tren mat (y = " + position.y.ToString("0.##") + ").");
    }
// =========================================================================
    // FindDeep - Tim child theo ten o moi cap.
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

    // =========================================================================
    // PlaceBeeNearPlayer - Dat ong ngay truoc mat nguoi choi, dung theo rig
    //   that cua scene (thay gia da dat XROrigin_VU1 san). Khong hardcode
    //   toa do vi moi scene VU2 co layout khac nhau.
    // =========================================================================
    private static void PlaceBeeNearPlayer(Transform bee, GameObject xrOrigin)
    {
        // Uu tien dung camera thuc trong scene de lay huong nhin chinh xac.
        var xro = xrOrigin.GetComponent<XROrigin>();
        if (xro != null && xro.Camera != null)
        {
            Transform cam = xro.Camera.transform;
            bee.position = cam.position + cam.forward * 1.2f;
            bee.rotation = Quaternion.LookRotation(cam.forward);
            return;
        }

        bee.position = xrOrigin.transform.position + Vector3.up * 1.5f + Vector3.forward * 1.2f;
        bee.rotation = Quaternion.identity;
    }

    // =========================================================================
    // CreateXrOrigin - Uu tien DUNG rig co san trong scene (vu thay gi ap).
    //   Chi khi scene thuc su khong co XROrigin moi tao moi.
    // =========================================================================
    private static GameObject CreateXrOrigin()
    {
        // 1. Reuse rig co san neu co (tu khi thay gia da lam VU2).
        XROrigin existing = Object.FindObjectOfType<XROrigin>();
        if (existing != null)
        {
            Debug.Log("[BeeFlightSceneSetup] Dung lai XR Origin co san: " + existing.name);
            return existing.gameObject;
        }

        GameObject byName = GameObject.Find("XR Origin (XR Rig)");
        if (byName != null) return byName;

        // 2. Scene chua co rig -> tao moi theo chuan XRI.
        if (!EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (VR)"))
        {
            Debug.LogError("[BeeFlightSceneSetup] Khong chay duoc menu XR Origin (VR). " +
                           "Kiem tra XRI 2.5.2 da import chua.");
            return null;
        }

        GameObject created = Selection.activeGameObject;
        if (created == null || created.GetComponent<XROrigin>() == null)
        {
            Debug.LogError("[BeeFlightSceneSetup] Menu XR Origin khong tao ra XROrigin.");
            return null;
        }
        return created;
    }

    // =========================================================================
    // AssignInputActions - Dung action co san cua XRI Default Input Actions:
    //   Move (Vector2) -> steer,  Activate (Button) -> flap.
    // Lay tu map Locomotion cho Move va map Interaction cho Activate.
    // =========================================================================
    private static void AssignInputActions(BeeController bee)
    {
        if (bee == null) return;

        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(XriActionsPath);
        if (asset == null)
        {
            Debug.LogWarning("[BeeFlightSceneSetup] Khong tim thay XRI Default Input Actions. " +
                             "Gan thu cong Steer/Flap trong Inspector.");
            return;
        }

        InputAction steer = null;
        InputAction flap = null;

        foreach (InputActionMap map in asset.actionMaps)
        {
            if (map.name.EndsWith("Locomotion") && steer == null)
            {
                steer = map.FindAction("Move", false);
            }
            if (map.name.EndsWith("Interaction") && flap == null)
            {
                flap = map.FindAction("Activate", false);
            }
        }

        if (steer != null) bee.steerAction = new InputActionProperty(steer);
        else Debug.LogWarning("[BeeFlightSceneSetup] Khong tim action 'Move'.");

        if (flap != null) bee.flapAction = new InputActionProperty(flap);
        else Debug.LogWarning("[BeeFlightSceneSetup] Khong tim action 'Activate'.");
    }

    // =========================================================================
    // DisableXriLocomotion - Dang bay thi khong can di chuyen boi, va no se keo
    // nguoi choi nguoc huong bay.
    // =========================================================================
    private static void DisableXriLocomotion(GameObject xrOrigin)
    {
        MonoBehaviour[] all = xrOrigin.GetComponentsInChildren<MonoBehaviour>(true);
        int count = 0;

        foreach (MonoBehaviour b in all)
        {
            if (b == null) continue;
            string n = b.GetType().Name;

            if (n.Contains("MoveProvider") ||
                n.Contains("TurnProvider") ||
                n.Contains("TeleportationProvider") ||
                n.Contains("NavigationProvider") ||
                n.Contains("CharacterControllerDriver"))
            {
                b.enabled = false;
                count++;
            }
        }

        CharacterController cc = xrOrigin.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        if (count > 0)
        {
            Debug.Log("[BeeFlightSceneSetup] Da tat " + count +
                      " locomotri XRI (dang bay nen khong can di chuyen boi).");
        }
    }
}
