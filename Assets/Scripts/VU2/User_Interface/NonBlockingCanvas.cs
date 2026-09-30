using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// NonBlockingCanvas - Ngăn Canvas chặn XR Ray trong VR.
// NonBlockingCanvas - Prevents Canvas from blocking XR Rays in VR.
//
// HAI CHẾ ĐỘ / TWO MODES:
//
//   DisplayOnly = true  (Canvas chỉ hiển thị, KHÔNG tương tác):
//     → Tắt mọi Raycaster (GraphicRaycaster + TrackedDeviceGraphicRaycaster)
//     → CanvasGroup.blocksRaycasts = false
//     → Tắt raycastTarget trên tất cả Graphic con
//     → XR ray xuyên qua hoàn toàn
//     → Dùng cho: HUD, bảng thông tin, tidal clock, season display
//
//   DisplayOnly = false (Canvas CÓ tương tác, ví dụ menu có Button):
//     → GIỮ nguyên Raycaster (để buttons hoạt động với XR)
//     → KHÔNG đặt CanvasGroup.blocksRaycasts = false
//     → Chỉ tắt raycastTarget trên Graphic KHÔNG thuộc Selectable
//     → XR ray chỉ hit vào buttons, xuyên qua phần trống
//     → Dùng cho: StartMenu, ResultMenu, SettingsMenu
// =============================================================================
public class NonBlockingCanvas : MonoBehaviour
{
    [Tooltip("Canvas này chỉ hiển thị, KHÔNG có button/tương tác?\n"
           + "True → tắt toàn bộ raycaster. False → giữ raycaster cho buttons.\n\n"
           + "Is this a display-only Canvas with NO buttons/interaction?\n"
           + "True → disable all raycasters. False → keep raycasters for buttons.")]
    public bool displayOnly = true;

    private GraphicRaycaster[] _cachedRaycasters;

    private void Awake()
    {
        Apply();
    }

    public void Apply()
    {
        if (displayOnly)
            ApplyDisplayOnly();
        else
            ApplyInteractiveCleanup();
    }

    // =========================================================================
    // CHẾ ĐỘ 1: Display Only — tắt hoàn toàn raycast
    // MODE 1: Display Only — disable all raycasting
    // =========================================================================
    private void ApplyDisplayOnly()
    {
        // Tắt mọi raycaster (GraphicRaycaster + TrackedDeviceGraphicRaycaster)
        _cachedRaycasters = GetComponentsInChildren<GraphicRaycaster>(true);
        foreach (var gr in _cachedRaycasters)
            gr.enabled = false;

        // CanvasGroup.blocksRaycasts = false
        SetBlocksRaycasts(gameObject, false);
        foreach (var canvas in GetComponentsInChildren<Canvas>(true))
            SetBlocksRaycasts(canvas.gameObject, false);

        // Tắt raycastTarget trên tất cả Graphic
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }

    // =========================================================================
    // CHẾ ĐỘ 2: Interactive Cleanup — giữ raycaster, chỉ dọn non-interactive
    // MODE 2: Interactive Cleanup — keep raycasters, clean non-interactive only
    // =========================================================================
    private void ApplyInteractiveCleanup()
    {
        // GIỮ raycaster enabled (cần cho button interaction)
        // KHÔNG đặt CanvasGroup.blocksRaycasts = false (sẽ phá buttons)

        // Chỉ tắt raycastTarget trên Graphic KHÔNG thuộc Selectable
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
        {
            // Nếu Graphic này (hoặc cha trực tiếp) là Selectable → giữ nguyên
            var selectable = graphic.GetComponent<Selectable>();
            if (selectable != null) continue;

            // Nếu Graphic này là targetGraphic của Selectable cha → giữ nguyên
            var parentSelectable = graphic.GetComponentInParent<Selectable>();
            if (parentSelectable != null && parentSelectable.targetGraphic == graphic)
                continue;

            // Nếu Graphic nằm trong cây con của Selectable → giữ nguyên
            if (parentSelectable != null)
            {
                // Kiểm tra Graphic có phải con trực tiếp của Selectable không
                if (graphic.transform.IsChildOf(parentSelectable.transform))
                    continue;
            }

            graphic.raycastTarget = false;
        }
    }

    // =========================================================================
    // API bật/tắt raycaster để UICamera gọi (chỉ cho displayOnly mode)
    // API enable/disable raycasters for UICamera (displayOnly mode only)
    // =========================================================================
    public void EnableRaycasters()
    {
        if (_cachedRaycasters == null) return;
        foreach (var gr in _cachedRaycasters)
            if (gr != null) gr.enabled = true;
    }

    public void DisableRaycasters()
    {
        if (_cachedRaycasters == null) return;
        foreach (var gr in _cachedRaycasters)
            if (gr != null) gr.enabled = false;
    }

    private void SetBlocksRaycasts(GameObject target, bool value)
    {
        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = target.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = value;
    }
}
