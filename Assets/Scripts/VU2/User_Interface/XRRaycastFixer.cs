using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// XRRaycastFixer - Tự động dọn sạch mọi nguồn chặn XR ray khi scene load.
// XRRaycastFixer - Automatically cleans all XR ray blockers at scene load.
//
// VẤN ĐỀ:
//   XRUIInputModule duyệt TẤT CẢ GraphicRaycaster đang enabled trong scene.
//   Mỗi World Space Canvas có GraphicRaycaster/TrackedDeviceGraphicRaycaster
//   sẽ chặn XR ray nếu ray giao cắt mặt phẳng Canvas — bất kể Canvas đó
//   thuộc camera nào (Base hay Overlay) hoặc ở vị trí nào.
//
// GIẢI PHÁP:
//   Script này chạy ở Awake() (trước mọi Start()) và:
//   1. Tìm TẤT CẢ GraphicRaycaster trong scene (kể cả TrackedDevice kế thừa)
//   2. Canvas NÀO không có Selectable (Button, Toggle...) → tắt raycaster
//   3. Canvas NÀO có Selectable → chỉ tắt raycastTarget trên Graphic trang trí
//
// CÁCH DÙNG:
//   Gắn lên EventSystem hoặc GameManager (object luôn tồn tại trong scene).
//   Chỉ cần 1 instance duy nhất.
// =============================================================================
public class XRRaycastFixer : MonoBehaviour
{
    [Tooltip("Bật log debug để kiểm tra Canvas nào bị tắt raycaster.\n"
           + "Enable debug logs to see which Canvas had raycaster disabled.")]
    public bool debugLog = true;

    private void Awake()
    {
        FixAllCanvasRaycasters();
    }

    /// <summary>
    /// Duyệt toàn bộ scene, tắt raycaster trên Canvas không tương tác,
    /// dọn raycastTarget trên Canvas tương tác.
    /// </summary>
    public void FixAllCanvasRaycasters()
    {
        // Tìm TẤT CẢ GraphicRaycaster (bao gồm TrackedDeviceGraphicRaycaster)
        // kể cả trên object inactive.
        GraphicRaycaster[] allRaycasters = Resources.FindObjectsOfTypeAll<GraphicRaycaster>();

        int disabledCount = 0;
        int cleanedCount = 0;

        foreach (var raycaster in allRaycasters)
        {
            // Bỏ qua object thuộc prefab (chưa instantiate vào scene)
            if (raycaster.gameObject.scene.name == null) continue;
            if (!raycaster.gameObject.scene.isLoaded) continue;

            Canvas canvas = raycaster.GetComponent<Canvas>();
            if (canvas == null) continue;

            // Kiểm tra Canvas này có chứa Selectable (Button, Toggle...) không
            bool hasInteractable = HasActiveSelectable(raycaster.gameObject);

            if (!hasInteractable)
            {
                // === Canvas không tương tác → TẮT HOÀN TOÀN raycaster ===
                raycaster.enabled = false;

                // Thêm CanvasGroup blocksRaycasts = false
                CanvasGroup cg = raycaster.gameObject.GetComponent<CanvasGroup>();
                if (cg == null) cg = raycaster.gameObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;

                // Tắt raycastTarget trên tất cả Graphic
                foreach (var g in raycaster.GetComponentsInChildren<Graphic>(true))
                    g.raycastTarget = false;

                disabledCount++;
                if (debugLog)
                    Debug.Log($"[XRRaycastFixer] DISABLED raycaster on '{GetFullPath(raycaster.gameObject)}' (no interactable)");
            }
            else
            {
                // === Canvas tương tác → CHỈ dọn Graphic trang trí ===
                foreach (var g in raycaster.GetComponentsInChildren<Graphic>(true))
                {
                    // Giữ raycastTarget cho Graphic thuộc Selectable
                    if (IsPartOfSelectable(g)) continue;
                    g.raycastTarget = false;
                }

                cleanedCount++;
                if (debugLog)
                    Debug.Log($"[XRRaycastFixer] CLEANED non-interactive graphics on '{GetFullPath(raycaster.gameObject)}' (has buttons)");
            }
        }

        if (debugLog)
            Debug.Log($"[XRRaycastFixer] Done: {disabledCount} raycasters disabled, {cleanedCount} canvases cleaned.");
    }

    /// <summary>
    /// Canvas có chứa Selectable đang active không?
    /// </summary>
    private bool HasActiveSelectable(GameObject obj)
    {
        // Bao gồm cả Selectable trên object inactive (có thể được bật sau).
        // Include Selectables on inactive objects (may be activated later).
        return obj.GetComponentsInChildren<Selectable>(true).Length > 0;
    }

    /// <summary>
    /// Graphic này có thuộc về Selectable (Button, Toggle...) không?
    /// </summary>
    private bool IsPartOfSelectable(Graphic g)
    {
        // Graphic chính nó là Selectable
        if (g.GetComponent<Selectable>() != null) return true;

        // Graphic nằm trong cây con của Selectable
        var parent = g.GetComponentInParent<Selectable>();
        if (parent != null && g.transform.IsChildOf(parent.transform))
            return true;

        return false;
    }

    private string GetFullPath(GameObject obj)
    {
        string path = obj.name;
        Transform t = obj.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}
