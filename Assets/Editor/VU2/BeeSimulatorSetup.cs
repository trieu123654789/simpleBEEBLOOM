// =============================================================================
// BeeSimulatorSetup - Cai dat scene de test bay ong bang XR DEVICE SIMULATOR
// (khong can kinh VR that, chi can may tinh + ban phim/chuot).
//
// MENU: VU2/Bee/Setup XR Device Simulator For Bee
//
// NOI DUNG
//   1. Tao / tim XR Origin (Camera Offset + Main Camera)
//   2. Bo prefab 'XR Interaction Setup' (XRInteractionManager + Input Action Manager)
//   3. Them XRDeviceSimulatorUI len camera -> bat panel simulator trong Game view
//   4. Gán input cho ong: steer <- XRI LeftHand Locomotion/Move
//                          flap  <- XRI LeftHand Interaction/Activate
//   5. Bat BeeCameraRig (keo XR Origin theo ong)
//   6. TAT BeeDesktopTestMode de khong tranh input voi simulator
//
// PHIM KHI DUNG SIMULATOR (mac dinh cua Unity)
//   W/S tien-lui | A/D di ngang | Q/E len-xuong | Chuot nhin | Tab doi tay cam
// =============================================================================
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
// XRDeviceSimulatorUI trong sample co "class" khong ghi "public" => internal,
// Assembly-CSharp-Editor khong goi truc tiep duoc. Dung reflection (ben duoi).

public static class BeeSimulatorSetup
{
    private const string InteractionSetupPrefabPath =
        "Assets/ThirdParty/Samples_XR/XR Interaction Toolkit/2.5.2/Starter Assets/Prefabs/XR Interaction Setup.prefab";

    private const string XriActionsPath =
        "Assets/ThirdParty/Samples_XR/XR Interaction Toolkit/2.5.2/Starter Assets/XRI Default Input Actions.inputactions";

    [MenuItem("VU2/Bee/Setup XR Device Simulator For Bee", false, 16)]
    public static void Setup()
    {
        BeeController bee = Object.FindObjectOfType<BeeController>();
        if (bee == null)
        {
            EditorUtility.DisplayDialog("Bee Simulator Setup",
                "Khong tim thay BeeController trong scene.\n" +
                "Hay chay 'VU2/Bee/Setup Bee Flight Scene' truoc.", "OK");
            return;
        }

        // --- 1. XR Origin ---
        XROrigin origin = Object.FindObjectOfType<XROrigin>();
        if (origin == null)
        {
            if (EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (VR)"))
                origin = Object.FindObjectOfType<XROrigin>();
        }
        if (origin == null)
        {
            Debug.LogError("[BeeSimulatorSetup] Khong tao duoc XR Origin.");
            return;
        }

        // --- 2. XR Interaction Setup (manager + input actions) ---
        if (Object.FindObjectOfType<XRInteractionManager>() == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InteractionSetupPrefabPath);
            if (prefab != null)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.name = "XR Interaction Setup";
                Undo.RegisterCreatedObjectUndo(inst, "Add XR Interaction Setup");
            }
            else
            {
                Debug.LogWarning("[BeeSimulatorSetup] Khong tim thay prefab 'XR Interaction Setup'.");
            }
        }

        // --- 3. Bat panel XR Device Simulator len camera XR ---
        Camera xrCam = origin.Camera != null ? origin.Camera : Camera.main;
        if (xrCam != null)
        {
            if (AddSimulatorUI(xrCam.gameObject))
                Debug.Log("[BeeSimulatorSetup] Da them XRDeviceSimulatorUI vao '" + xrCam.name + "'.");
        }

        // --- 4. Gan input ong theo action cua XRI ---
        bool bound = BindXriActions(bee);

        // --- 5. Bat BeeCameraRig ---
        BeeCameraRig rig = bee.GetComponent<BeeCameraRig>();
        if (rig == null) rig = Undo.AddComponent<BeeCameraRig>(bee.gameObject);
        rig.playerRig = origin.transform;
        rig.trackedCamera = xrCam;
        rig.bee = bee;
        rig.enabled = true;
        EditorUtility.SetDirty(rig);

        // --- 6. Tat che do test PC (de khong tranh input) ---
        BeeDesktopTestMode desktop = bee.GetComponent<BeeDesktopTestMode>();
        if (desktop != null)
        {
            desktop.enabled = false;
            Debug.Log("[BeeSimulatorSetup] Da TAT BeeDesktopTestMode (simulator se dieu khien).");
        }

        // --- 7. Trong giup dap canh bang Space/Shift ---
        BeeKeyboardAssist assist = bee.GetComponent<BeeKeyboardAssist>();
        if (assist == null) assist = Undo.AddComponent<BeeKeyboardAssist>(bee.gameObject);
        assist.bee = bee;
        assist.alsoQeVertical = false;   // simulator da dung Q/E de bay len/xuong
        EditorUtility.SetDirty(assist);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[BeeSimulatorSetup] Xong. Nhan Play -> panel XR Device Simulator hien tren Game view.\n" +
                  "   [QUAN TRONG] 'Selected Device Input' phai la 'Left Controller' " +
                  "(nhan T / Y de doi) - dang HMD thi WASD khong dung.\n" +
                  "   Dap canh: Space | Shift giu.\n" +
                  "   Bay bang: WASD + Q/E tren cay dieu khien ao." +
                  (bound ? "" : "\n   [CANH BAO] khong gan duoc input action!") );
    }
    // ===================== ADD SIMULATOR UI (REFLECTION) =====================
    private const string SimulatorUIFullName =
        "UnityEngine.XR.Interaction.Toolkit.Samples.DeviceSimulator.XRDeviceSimulatorUI";

    private static System.Type FindSimulatorUIType()
    {
        foreach (System.Reflection.Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            System.Type t = asm.GetType(SimulatorUIFullName);
            if (t != null) return t;
        }
        return null;
    }

    /// <summary>
    /// Them XRDeviceSimulatorUI len GameObject. Class nay internal nen phai reflection.
    /// </summary>
    private static bool AddSimulatorUI(GameObject target)
    {
        System.Type simType = FindSimulatorUIType();
        if (simType == null)
        {
            Debug.LogError("[BeeSimulatorSetup] Khong tim thay class " + SimulatorUIFullName +
                           ". Hay them component 'XR Device Simulator UI' bang tay tren camera.");
            return false;
        }

        if (target.GetComponent(simType) != null) return true;

        Component c = Undo.AddComponent(target, simType);
        if (c == null)
        {
            Debug.LogError("[BeeSimulatorSetup] Them XRDeviceSimulatorUI that bai. " +
                           "Hay them bang tay: chon camera > Add Component > XR Device Simulator UI.");
            return false;
        }
        return true;
    }

    // ===================== BIND XRI ACTIONS =====================
    private static bool BindXriActions(BeeController bee)
    {
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(XriActionsPath);
        if (asset == null)
        {
            Debug.LogError("[BeeSimulatorSetup] Khong load duoc XRI Default Input Actions: " + XriActionsPath);
            return false;
        }

        InputAction steer = null;
        InputAction flap = null;

        foreach (InputActionMap map in asset.actionMaps)
        {
            // Lay map LeftHand truoc (simulator dieu khien tay cam trai mac dinh).
            if (map.name.Contains("LeftHand") || map.name.EndsWith("Locomotion") || map.name.EndsWith("Interaction"))
            {
                if (steer == null && map.name.EndsWith("Locomotion")) steer = map.FindAction("Move", false);
                if (flap == null && map.name.EndsWith("Interaction")) flap = map.FindAction("Activate", false);
            }
        }

        if (steer != null) bee.steerAction = new InputActionProperty(steer);
        else Debug.LogError("[BeeSimulatorSetup] Khong tim thay action 'Move' trong map Locomotion.");

        if (flap != null) bee.flapAction = new InputActionProperty(flap);
        else Debug.LogError("[BeeSimulatorSetup] Khong tim thay action 'Activate' trong map Interaction.");

        EditorUtility.SetDirty(bee);
        bool ok = steer != null && flap != null;
        Debug.Log(ok
            ? "[BeeSimulatorSetup] Da gan input ong: steer <- Move, flap <- Activate."
            : "[BeeSimulatorSetup] GAN INPUT KHONG DAY DU.");
        return ok;
    }
}