// =============================================================================
// BeeControllerShakeAdapter - Bridge giữa BeeShakeFlap và BeeController.
// BeeControllerShakeAdapter - Bridge between shake detection and flight controller.
//
// VẤN ĐỀ / PROBLEM
//   Unity InputSystem không cho phép "fake press" InputAction từ code.
//   BeeShakeFlap phát hiện lắc tay nhưng BeeController chỉ đọc flapAction.IsPressed().
//
// GIẢI PHÁP / SOLUTION
//   Adapter này ghi đè flapAction của BeeController bằng một InputAction ảo
//   được điều khiển hoàn toàn từ code (không bind với hardware).
//   BeeShakeFlap.SetShakeFlapping(true/false) → Adapter enable/disable action ảo.
//
// CÁCH DÙNG / USAGE
//   Gắn lên cùng GameObject với BeeController và BeeShakeFlap.
//   Không cần cấu hình gì thêm — tự kết nối trong Awake().
// =============================================================================
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(BeeController))]
public class BeeControllerShakeAdapter : MonoBehaviour
{
    // =========================================================================
    // REFERENCES / THAM CHIẾU
    // =========================================================================
    [Header("References / Tham chiếu")]
    [Tooltip("BeeController sẽ nhận override. Để trống sẽ tự tìm.")]
    public BeeController bee;

    // =========================================================================
    // STATE / TRẠNG THÁI
    // =========================================================================
    private InputAction _fakeFlap;           // Action ảo thay thế flapAction
    private InputActionProperty _savedFlap;  // flapAction gốc để khôi phục
    private bool _shakeFlapping;
    private bool _installed;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        if (bee == null) bee = GetComponent<BeeController>();
    }

    private void OnEnable()
    {
        InstallFakeAction();
    }

    private void OnDisable()
    {
        RestoreOriginalAction();
    }

    // =========================================================================
    // InstallFakeAction - Tạo và gán InputAction ảo vào BeeController.
    // =========================================================================
    private void InstallFakeAction()
    {
        if (_installed || bee == null) return;

        // Lưu action gốc
        _savedFlap = bee.flapAction;

        // Tạo action ảo dạng Button, không bind phím nào
        _fakeFlap = new InputAction(
            name: "BeeShakeFlapVirtual",
            type: InputActionType.Button);

        // Thêm binding "hư" vào path không tồn tại để action valid nhưng không bao giờ fire từ hardware.
        // Unity yêu cầu ít nhất 1 binding để action có thể Enable().
        _fakeFlap.AddBinding("<Keyboard>/f24");  // F24 = phím không tồn tại trên thực tế

        _fakeFlap.Enable();

        // Ghi đè flapAction của BeeController
        bee.flapAction = new InputActionProperty(_fakeFlap);

        _installed = true;
        Debug.Log("[BeeControllerShakeAdapter] Installed fake flap action on BeeController.");
    }

    // =========================================================================
    // RestoreOriginalAction - Khôi phục flapAction gốc khi disable.
    // =========================================================================
    private void RestoreOriginalAction()
    {
        if (!_installed || bee == null) return;

        bee.flapAction = _savedFlap;
        if (_savedFlap.action != null) _savedFlap.action.Enable();

        _fakeFlap?.Disable();
        _fakeFlap?.Dispose();
        _fakeFlap = null;
        _installed = false;

        Debug.Log("[BeeControllerShakeAdapter] Restored original flap action.");
    }

    // =========================================================================
    // SetShakeFlapping - BeeShakeFlap gọi hàm này mỗi frame.
    //
    // TRICK: InputSystem không có API "set pressed". Nhưng ta có thể hack
    //        bằng cách đọc IsPressed() từ một script wrapper thay vì từ action.
    //        Để tránh thay đổi BeeController, ta override Update() để trực tiếp
    //        set Speed và IsFlapping qua reflection — KHÔNG khuyến nghị.
    //
    //        SẠCH HƠN: ta thêm một public property vào BeeController để nhận
    //        override từ bên ngoài. Adapter này sẽ gọi đó mỗi Update().
    //        Xem BeeController.SetFlapOverride(bool).
    // =========================================================================
    public void SetShakeFlapping(bool isFlapping)
    {
        _shakeFlapping = isFlapping;
    }

    // =========================================================================
    // Update - Cập nhật trạng thái đập cánh vào BeeController mỗi frame.
    //
    //   Vì InputSystem không cho phép fake press action từ code,
    //   ta sử dụng phương pháp trực tiếp hơn: ghi đè flapAction bằng
    //   một derived approach — enable/disable bindings động.
    //
    //   Thực tế hiệu quả nhất: thêm BeeController.ExternalFlapOverride (xem file đó).
    // =========================================================================
    private void Update()
    {
        if (bee == null) return;

        // Phương án B: dùng ExternalFlapOverride nếu BeeController hỗ trợ
        // (sẽ được thêm bên dưới trong BeeController extension)
        bee.ExternalFlapOverride = _shakeFlapping;
    }
}
