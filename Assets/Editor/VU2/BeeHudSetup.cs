// =============================================================================
// BeeHudSetup - Menu Editor cai HUD con ong + preset goc nhin vao scene.
// BeeHudSetup - Menu Editor cài HUD con ong + preset góc nhìn vào scene.
//
// MENU
//   VU2/Bee/Add Bee HUD To Camera    -> gan BeeHud + BeeGameStats vao camera XR
//   VU2/Bee/Add Altitude Hold To Bee -> gan BeeAltitudeHold vao BeeRoot
//   VU2/Bee/Apply Bee POV Preset     -> dat thong so goc nhin nhu anh mau
//
// VI SAO CAN MENU
//   HUD duoc dung bang code (BeeHud.Build) nen khong the "keo prefab".
//   Menu nay chi wire tham chieu (camera, font, stats) MOT lan, sau do bam Play
//   la HUD hien ra nhu anh mau.
// =============================================================================
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;

public static class BeeHudSetup
{
    /// <summary>Font co dau tieng Viet dung cho HUD.</summary>
    private const string FontPath = "Assets/UI/Sources/Font/utm-avobold SDF.asset";

    private const string HudObjectName = "BeeHUD";

    // =========================================================================
    // 1. ADD HUD TO CAMERA
    // =========================================================================
    [MenuItem("VU2/Bee/Add Bee HUD To Camera", false, 12)]
    public static void AddHudToCamera()
    {
        Camera camera = FindXrCamera();
        if (camera == null)
        {
            Debug.LogError("[BeeHudSetup] Khong tim thay camera XR trong scene. " +
                           "Hay chay 'VU2/Bee/Setup Bee Flight Scene' truoc.");
            return;
        }

        // Node HUD nam NGAY TREN camera de canvas luon o truoc mat nguoi choi.
        Transform existing = camera.transform.Find(HudObjectName);
        GameObject hudObject;
        if (existing != null)
        {
            hudObject = existing.gameObject;
        }
        else
        {
            hudObject = new GameObject(HudObjectName);
            hudObject.transform.SetParent(camera.transform, false);
            hudObject.transform.localPosition = Vector3.zero;
            hudObject.transform.localRotation = Quaternion.identity;
            hudObject.transform.localScale = Vector3.one;
            Undo.RegisterCreatedObjectUndo(hudObject, "Create Bee HUD");
        }

        BeeHud hud = hudObject.GetComponent<BeeHud>();
        if (hud == null) hud = Undo.AddComponent<BeeHud>(hudObject);

        BeeGameStats stats = hudObject.GetComponent<BeeGameStats>();
        if (stats == null) stats = Undo.AddComponent<BeeGameStats>(hudObject);

        hud.targetCamera = camera;
        hud.stats = stats;
        hud.bee = Object.FindObjectOfType<BeeController>();
        hud.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (hud.font == null)
        {
            Debug.LogWarning("[BeeHudSetup] Khong tim thay font tai " + FontPath +
                             ". HUD se dung font mac dinh (co the thieu dau tieng Viet).");
        }

        EditorUtility.SetDirty(hud);
        EditorUtility.SetDirty(stats);

        Selection.activeGameObject = hudObject;
        EditorGUIUtility.PingObject(hudObject);

        Debug.Log("[BeeHudSetup] Da gan HUD con ong vao camera '" + camera.name +
                  "'. Bam Play de xem HUD (binh mat, la ban, thuoc toc do...).");
    }

    // =========================================================================
    // 2. ADD ALTITUDE HOLD TO BEE
    // =========================================================================
    [MenuItem("VU2/Bee/Add Altitude Hold To Bee", false, 13)]
    public static void AddAltitudeHoldToBee()
    {
        BeeController bee = Object.FindObjectOfType<BeeController>();
        if (bee == null)
        {
            Debug.LogError("[BeeHudSetup] Khong tim thay BeeController trong scene. " +
                           "Hay chay 'VU2/Bee/Setup Bee Flight Scene' truoc.");
            return;
        }

        BeeAltitudeHold hold = bee.GetComponent<BeeAltitudeHold>();
        if (hold == null) hold = Undo.AddComponent<BeeAltitudeHold>(bee.gameObject);

        hold.cruiseAltitude = 1.5f;
        hold.minClearance = 0.4f;

        // Bo qua XR rig khi do do cao (tranh bat nham capsule nguoi choi).
        XROrigin origin = Object.FindObjectOfType<XROrigin>();
        if (origin != null)
        {
            hold.playerRig = origin.transform;
            hold.ignorePlayerRig = true;
        }

        EditorUtility.SetDirty(hold);

        Selection.activeGameObject = bee.gameObject;
        Debug.Log("[BeeHudSetup] Da gan BeeAltitudeHold: ong luot sat mat nuoc ~" +
                  hold.cruiseAltitude + "m (khong lan xuong duoi " + hold.minClearance + "m).");
    }

    // =========================================================================
    // 3. APPLY POV PRESET
    // =========================================================================
    [MenuItem("VU2/Bee/Apply Bee POV Preset", false, 14)]
    public static void ApplyPovPreset()
    {
        BeeCameraRig rig = Object.FindObjectOfType<BeeCameraRig>();
        if (rig == null)
        {
            Debug.LogError("[BeeHudSetup] Khong tim thay BeeCameraRig trong scene.");
            return;
        }

        Undo.RecordObject(rig, "Apply Bee POV Preset");

        BeeController bee = Object.FindObjectOfType<BeeController>();
        if (bee != null)
        {
            Undo.RecordObject(bee, "Apply Bee POV Preset");
            rig.bee = bee;
        }

        rig.ApplyBeePovPreset();

        // Dat FOV ngay de Scene view thay dung khung hinh anh mau.
        if (rig.trackedCamera != null) rig.trackedCamera.fieldOfView = rig.targetFov;

        EditorUtility.SetDirty(rig);
        if (bee != null) EditorUtility.SetDirty(bee);

        Debug.Log("[BeeHudSetup] Da ap preset goc nhin con ong: FOV " + rig.targetFov +
                  ", nghien nhe khi re, cui xuong khi bay nhanh (speedPitchDown=" +
                  rig.speedPitchDown + ").");
    }

    // =========================================================================
    // HELPERS / HAM PHU
    // =========================================================================

    /// <summary>Camera XR: uu tien camera cua XROrigin, roi Main Camera.</summary>
    public static Camera FindXrCamera()
    {
        XROrigin origin = Object.FindObjectOfType<XROrigin>();
        if (origin != null && origin.Camera != null) return origin.Camera;

        if (Camera.main != null) return Camera.main;

        foreach (Camera camera in Object.FindObjectsOfType<Camera>())
        {
            if (!camera.isActiveAndEnabled) continue;
            if (camera.gameObject.name.Contains("UI Camera")) continue;
            return camera;
        }
        return null;
    }
}
