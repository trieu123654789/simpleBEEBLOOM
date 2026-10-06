// =============================================================================
// BeeFlightInput - Cho phép lái ong bằng BÀN PHÍM để test nhanh trong Editor.
// BeeFlightInput - Cho phép lái ong bằng bàn phím để test nhanh trong Editor.
//
// WHY / VÌ SAO CẦN
//   Debug trong Unity Editor không phải lúc nào cũng bật được XR Device
//   Simulator, và việc test bằng bàn phím nhanh hơn nhiều. Script này tự tạo
//   InputAction ảo (keyboard) và gán vào BeeController khi đang ở chế độ
//   Editor, còn khi chạy trên Quest thì không can thiệp (dùng joystick XR thật).
//
// BEHAVIOR / HÀNH VI
//   - Trong Editor (UNITY_EDITOR): W/S = pitch, A/D = yaw, Space = flap.
//   - Trên device (Quest/Quest2): KHÔNG động vào input, dùng XR controller.
//
// SETUP / THIẾT LẬP
//   Gắn script này lên BeeRoot cùng BeeController. Gán
//   BeeController.steerAction và flapAction = Use Reference (Reference Only)
//   để script ghi đè được trong Editor.
// =============================================================================
using UnityEngine;
using UnityEngine.InputSystem;

public class BeeFlightInput : MonoBehaviour
{
    // =========================================================================
    // KEYBOARD SETTINGS / CÀI ĐẶT BÀN PHÍM
    // =========================================================================
    [Header("Keyboard Mapping / Phím (chỉ dùng trong Editor)")]
    [Tooltip("Bật/tắt input bàn phím để test nhanh trong Editor.")]
    public bool enableKeyboardInEditor = true;

    [Tooltip("Phím đập cánh.")]
    public Key flapKey = Key.Space;

    // =========================================================================
    // REFERENCES / THAM CHIẾU
    // =========================================================================
    [Tooltip("BeeController sẽ nhận input từ script này. Để trống sẽ tự tìm.")]
    public BeeController bee;

    // =========================================================================
    // INTERNAL / NỘI BỘ
    // =========================================================================
    private InputAction _virtualSteer;
    private InputAction _virtualFlap;
    private bool _installed;

    // Luu lai action goc de khoi phuc khi script bi tat (vd khi mo menu).
    private InputActionProperty _originalSteer;
    private InputActionProperty _originalFlap;

    private void Awake()
    {
        if (bee == null)
        {
            bee = GetComponent<BeeController>();
        }
    }

    private void OnEnable()
    {
        if (!enableKeyboardInEditor) return;
        if (!Application.isEditor) return;

        InstallVirtualInput();
    }

    private void OnDisable()
    {
        RemoveVirtualInput();
    }

    // =========================================================================
    // InstallVirtualInput - Tạo InputAction ảo và gán vào BeeController.
    // =============================================================================
    private void InstallVirtualInput()
    {
        if (_installed || bee == null) return;

        // --- Action lái (2D) ---
        _virtualSteer = new InputAction(
            name: "BeeSteerKeyboard",
            type: InputActionType.Value,
            binding: "<Keyboard>/a,d,w,s");
        _virtualSteer.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // --- Action đập cánh ---
        _virtualFlap = new InputAction(
            name: "BeeFlapKeyboard",
            type: InputActionType.Button,
            binding: $"<Keyboard>/{flapKey.ToString().ToLower()}");

        // Luu action goc truoc khi ghi de.
        _originalSteer = bee.steerAction;
        _originalFlap = bee.flapAction;

        // Gán tham chiếu trực tiếp (Reference Only) - BeeController đọc được.
        bee.steerAction = new InputActionProperty(_virtualSteer);
        bee.flapAction = new InputActionProperty(_virtualFlap);

        // BeeController có thể đã chạy OnEnable trước script này, nên ta phải
        // tự Enable. Nếu không, ReadValue() sẽ trả về zero và con ong đứng yên.
        _virtualSteer.Enable();
        _virtualFlap.Enable();

        _installed = true;

        Debug.Log("[BeeFlightInput] Keyboard input active (Editor test mode). " +
                  "A/D = yaw, W/S = pitch, Space = flap.");
    }

    // =========================================================================
    // RemoveVirtualInput - Gỡ input ảo, khôi phục input XR gốc.
    // =============================================================================
    private void RemoveVirtualInput()
    {
        if (!_installed) return;

        // Khôi phục input XR gốc (vd joystick trên Quest).
        if (bee != null)
        {
            bee.steerAction = _originalSteer;
            bee.flapAction = _originalFlap;

            if (_originalSteer.action != null) _originalSteer.action.Enable();
            if (_originalFlap.action != null) _originalFlap.action.Enable();
        }

        _virtualSteer?.Disable();
        _virtualFlap?.Disable();
        _virtualSteer?.Dispose();
        _virtualFlap?.Dispose();
        _virtualSteer = null;
        _virtualFlap = null;
        _installed = false;
    }

    // =========================================================================
    // ContextMenu - Tiện ích gỡ input ảo thủ công khi debug.
    // =============================================================================
    [ContextMenu("Remove Virtual Input (Debug)")]
    private void DebugRemoveVirtualInput()
    {
        RemoveVirtualInput();
        Debug.Log("[BeeFlightInput] Virtual input removed.");
    }
}
