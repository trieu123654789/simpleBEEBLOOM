using UnityEngine;
using UnityEngine.Rendering.Universal;

// =============================================================================
// UIStructureFixer - Sửa cấu trúc UI Camera để XR ray không bị chặn.
// UIStructureFixer - Fixes UI Camera structure so XR rays are not blocked.
//
// VẤN ĐỀ:
//   "UI Camera" (Overlay) nằm trong camera stack của Main Camera.
//   → Mọi Canvas con của UI Camera render PHỦ LÊN TRÊN thế giới 3D.
//   → XRUIInputModule query GraphicRaycaster trên mọi Canvas →
//     ray bị chặn bởi UI dù UI ở xa.
//
// GIẢI PHÁP (chạy runtime):
//   1. Xóa UI Camera khỏi camera stack của Main Camera
//   2. Chuyển object "UI" (chứa UIGAMEMENU) sang con Main Camera
//   3. Tắt UI Camera (không cần nữa — Main Camera render tất cả)
//   4. Canvas World Space sẽ tôn trọng khoảng cách 3D
//
// CÁCH DÙNG:
//   Gắn lên EventSystem hoặc GameManager. Chạy 1 lần duy nhất ở Awake().
// =============================================================================
public class UIStructureFixer : MonoBehaviour
{
    [Tooltip("Tên của UI Camera object trong scene.\n"
           + "Name of the UI Camera object in scene.")]
    public string uiCameraName = "UI Camera";

    [Tooltip("Tên của object UI chính (con của UI Camera chứa UIGAMEMENU).\n"
           + "Name of the main UI object (child of UI Camera containing UIGAMEMENU).")]
    public string uiRootName = "UI";

    [Tooltip("Log chi tiết các bước sửa.\nLog detailed fix steps.")]
    public bool debugLog = true;

    private void Awake()
    {
        FixUIStructure();
    }

    public void FixUIStructure()
    {
        // === Bước 1: Tìm Main Camera (tag MainCamera, trong XR Rig) ===
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[UIStructureFixer] Camera.main not found!");
            return;
        }

        // === Bước 2: Tìm UI Camera ===
        GameObject uiCamObj = null;
        foreach (var cam in FindObjectsOfType<Camera>(true))
        {
            if (cam.gameObject.name == uiCameraName && cam != mainCam)
            {
                uiCamObj = cam.gameObject;
                break;
            }
        }

        if (uiCamObj == null)
        {
            if (debugLog) Debug.Log("[UIStructureFixer] UI Camera not found — may already be fixed.");
            return;
        }

        Camera uiCam = uiCamObj.GetComponent<Camera>();

        // === Bước 3: Xóa UI Camera khỏi camera stack của Main Camera ===
        var mainCamData = mainCam.GetUniversalAdditionalCameraData();
        if (mainCamData != null && mainCamData.cameraStack != null)
        {
            if (mainCamData.cameraStack.Contains(uiCam))
            {
                mainCamData.cameraStack.Remove(uiCam);
                if (debugLog) Debug.Log("[UIStructureFixer] Removed UI Camera from Main Camera stack.");
            }
        }

        // === Bước 4: Tìm object "UI" (con của UI Camera chứa CanvasFollower) ===
        Transform uiRoot = uiCamObj.transform.Find(uiRootName);
        if (uiRoot == null)
        {
            // Tìm trong children
            foreach (Transform child in uiCamObj.transform)
            {
                if (child.name == uiRootName)
                {
                    uiRoot = child;
                    break;
                }
            }
        }

        if (uiRoot != null)
        {
            // === Bước 5: Chuyển "UI" sang con Main Camera ===
            // Giữ nguyên world position/rotation
            uiRoot.SetParent(mainCam.transform, true);

            if (debugLog) Debug.Log($"[UIStructureFixer] Moved '{uiRootName}' to Main Camera ({mainCam.name}).");

            // CanvasFollower đã có targetCamera = Main Camera → sẽ tự follow đúng
        }
        else
        {
            if (debugLog) Debug.LogWarning($"[UIStructureFixer] '{uiRootName}' not found under UI Camera.");
        }

        // === Bước 6: Thêm Layer 5 (UI) vào cullingMask của Main Camera ===
        // UI Camera chỉ render Layer 5. Main Camera có thể không có layer này.
        int uiLayer = 1 << 5; // Layer 5 = UI
        if ((mainCam.cullingMask & uiLayer) == 0)
        {
            mainCam.cullingMask |= uiLayer;
            if (debugLog) Debug.Log("[UIStructureFixer] Added UI layer (5) to Main Camera culling mask.");
        }

        // === Bước 7: Tắt UI Camera ===
        uiCam.enabled = false;
        uiCamObj.SetActive(false);
        if (debugLog) Debug.Log("[UIStructureFixer] Disabled UI Camera.");

        if (debugLog) Debug.Log("[UIStructureFixer] DONE — UI now renders through Main Camera with 3D depth.");
    }
}
