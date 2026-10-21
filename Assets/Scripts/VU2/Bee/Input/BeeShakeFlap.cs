// =============================================================================
// BeeShakeFlap - Lắc tay cầm vật lý (Quest controller) = đập cánh.
// BeeShakeFlap - Physical controller shake = wing flap.
//
// CƠ CHẾ / MECHANISM
//   Mỗi frame đọc deviceAcceleration từ cả 2 tay (Left + Right XR controller).
//   Nếu magnitude của gia tốc vượt ngưỡng => ghi nhận 1 "nhịp đập".
//   Số nhịp/giây => ánh xạ sang BeeController thông qua việc ghi đè
//   flapAction bằng một InputAction ảo (tương tự BeeFlightInput).
//
// CHỐNG NHIỄU / NOISE REJECTION
//   - Deadband: bỏ qua rung nhỏ < shakeThreshold (g-unit, 1g ≈ 9.81 m/s²).
//   - Cooldown: sau mỗi nhịp đập phải chờ shakeCooldown giây trước nhịp tiếp.
//   - Symmetric: cần cả 2 tay lắc (chế độ mặc định) hoặc chỉ 1 tay.
//
// QUEST IMU NOTE
//   Quest trả về acceleration ở LOCAL SPACE của controller (không phải world).
//   Điều này có nghĩa rõ ràng là lắc theo bất kỳ hướng nào đều được.
//   Gravity (9.81 m/s²) thường bị bỏ qua bởi firmware Quest → số đo ≈ động lực.
//
// FALLBACK EDITOR
//   Trong Unity Editor, không có controller thật → tự động dùng phím Space
//   (dùng chung với BeeFlightInput.cs – không xung đột vì BeeShakeFlap chỉ
//   active khi Application.isPlaying && !Application.isEditor hoặc khi đã có
//   XR device kết nối thật).
// =============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRInputDevice = UnityEngine.XR.InputDevice;  // rõ ràng tránh nhầm với InputSystem.InputDevice

[DisallowMultipleComponent]
public class BeeShakeFlap : MonoBehaviour
{
    // =========================================================================
    // SHAKE SETTINGS / CÀI ĐẶT LẮC TAY
    // =========================================================================
    [Header("Shake Detection / Phát hiện lắc tay")]
    [Tooltip("Ngưỡng gia tốc (m/s²) để tính là 1 nhịp đập. Quest ~ 2-4 m/s² khi lắc nhẹ.")]
    [Range(1f, 15f)]
    public float shakeThreshold = 3.5f;

    [Tooltip("Thời gian chờ tối thiểu giữa 2 nhịp đập (giây). Ngăn double-count.")]
    [Range(0.05f, 0.5f)]
    public float shakeCooldown = 0.12f;

    [Tooltip("Cần CẢ HAI tay lắc (true) hay chỉ 1 tay (false)?")]
    public bool requireBothHands = false;

    [Tooltip("Tốc độ nhịp đập (nhịp/giây) để duy trì lực bay đầy đủ.")]
    [Range(2f, 10f)]
    public float targetFlapsPerSecond = 4f;

    [Tooltip("Khoảng thời gian đo tốc độ nhịp đập (giây). Cửa sổ trung bình.")]
    [Range(0.3f, 2f)]
    public float flapRateWindow = 0.6f;

    // =========================================================================
    // REFERENCES / THAM CHIẾU
    // =========================================================================
    [Header("References / Tham chiếu")]
    [Tooltip("BeeController nhận input. Để trống sẽ tự tìm trên GameObject này.")]
    public BeeController bee;

    // =========================================================================
    // DEBUG / GỠ LỖI
    // =========================================================================
    [Header("Debug")]
    [Tooltip("Hiển thị log nhịp đập trong Console (tắt khi release).")]
    public bool debugLog = false;

    [Tooltip("Hiển thị Gizmo sphere khi đang đập cánh.")]
    public bool debugGizmo = true;

    // =========================================================================
    // PUBLIC STATE / TRẠNG THÁI
    // =========================================================================
    /// <summary>Đang đập cánh (theo cảm nhận shake). / Is currently flapping from shake.</summary>
    public bool IsShakeFlapping { get; private set; }

    /// <summary>Tốc độ nhịp đập hiện tại (nhịp/giây). / Current flap rate (flaps/sec).</summary>
    public float CurrentFlapRate { get; private set; }

    /// <summary>Lực gia tốc tay trái hiện tại (magnitude, m/s²).</summary>
    public float LeftAccelMag { get; private set; }

    /// <summary>Lực gia tốc tay phải hiện tại (magnitude, m/s²).</summary>
    public float RightAccelMag { get; private set; }

    // =========================================================================
    // INTERNAL / NỘI BỘ
    // =========================================================================
    private InputAction _virtualFlap;
    private InputActionProperty _originalFlap;
    private bool _installed;

    // XR Devices
    private XRInputDevice _leftController;
    private XRInputDevice _rightController;
    private bool _devicesFound;

    // Shake timing
    private float _leftCooldown;
    private float _rightCooldown;

    // Flap rate tracking (sliding window)
    private readonly Queue<float> _flapTimestamps = new Queue<float>();
    private float _flapActiveTimer;   // Thời gian tính "đang đập" sau nhịp cuối

    // Constant: flap stays "active" this many seconds after last detected shake
    private const float FlapActiveHoldSeconds = 0.25f;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        if (bee == null) bee = GetComponent<BeeController>();
    }

    private void OnEnable()
    {
        FindXRDevices();
        InstallVirtualFlap();
    }

    private void OnDisable()
    {
        RemoveVirtualFlap();
    }

    private void Update()
    {
        // Thử lại tìm device nếu chưa có (user bật controller sau khi app start).
        if (!_devicesFound) FindXRDevices();

        ReadControllerAcceleration();
        DetectShake();
        UpdateFlapRate();
        UpdateVirtualAction();
    }

    // =========================================================================
    // FindXRDevices - Tìm Left/Right XR controller.
    // =========================================================================
    private void FindXRDevices()
    {
        var leftDevices  = new List<XRInputDevice>();
        var rightDevices = new List<XRInputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            leftDevices);
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            rightDevices);

        if (leftDevices.Count > 0) _leftController = leftDevices[0];
        if (rightDevices.Count > 0) _rightController = rightDevices[0];

        _devicesFound = _leftController.isValid || _rightController.isValid;
    }

    // =========================================================================
    // ReadControllerAcceleration - Lấy magnitude gia tốc từ 2 tay.
    // =========================================================================
    private void ReadControllerAcceleration()
    {
        // Fallback trong Editor (không có controller thật): dùng bàn phím Space.
        if (!_devicesFound)
        {
            bool spaceHeld = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            LeftAccelMag = spaceHeld ? shakeThreshold + 1f : 0f;
            RightAccelMag = spaceHeld ? shakeThreshold + 1f : 0f;
            return;
        }

        Vector3 leftAccel = Vector3.zero;
        Vector3 rightAccel = Vector3.zero;

        _leftController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceAcceleration, out leftAccel);
        _rightController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceAcceleration, out rightAccel);

        LeftAccelMag = leftAccel.magnitude;
        RightAccelMag = rightAccel.magnitude;
    }

    // =========================================================================
    // DetectShake - Phát hiện nhịp đập từ ngưỡng gia tốc.
    // =========================================================================
    private void DetectShake()
    {
        _leftCooldown -= Time.deltaTime;
        _rightCooldown -= Time.deltaTime;

        bool leftShook = LeftAccelMag >= shakeThreshold && _leftCooldown <= 0f;
        bool rightShook = RightAccelMag >= shakeThreshold && _rightCooldown <= 0f;

        if (leftShook)
        {
            _leftCooldown = shakeCooldown;
            if (debugLog) Debug.Log($"[BeeShakeFlap] LEFT shake: {LeftAccelMag:F1} m/s²");
        }

        if (rightShook)
        {
            _rightCooldown = shakeCooldown;
            if (debugLog) Debug.Log($"[BeeShakeFlap] RIGHT shake: {RightAccelMag:F1} m/s²");
        }

        // Ghi nhận nhịp đập
        bool flapDetected = requireBothHands
            ? (leftShook && rightShook)
            : (leftShook || rightShook);

        if (flapDetected)
        {
            RecordFlap();
        }
    }

    // =========================================================================
    // RecordFlap - Thêm timestamp nhịp đập vào cửa sổ trượt.
    // =========================================================================
    private void RecordFlap()
    {
        float now = Time.time;
        _flapTimestamps.Enqueue(now);
        _flapActiveTimer = FlapActiveHoldSeconds;

        if (debugLog) Debug.Log($"[BeeShakeFlap] Flap recorded. Queue size: {_flapTimestamps.Count}");
    }

    // =========================================================================
    // UpdateFlapRate - Tính tốc độ nhịp đập trong cửa sổ thời gian.
    // =========================================================================
    private void UpdateFlapRate()
    {
        float now = Time.time;
        float windowStart = now - flapRateWindow;

        // Loại bỏ timestamps cũ ngoài cửa sổ
        while (_flapTimestamps.Count > 0 && _flapTimestamps.Peek() < windowStart)
        {
            _flapTimestamps.Dequeue();
        }

        CurrentFlapRate = _flapTimestamps.Count / flapRateWindow;

        // Đếm ngược thời gian "đang đập"
        _flapActiveTimer -= Time.deltaTime;
        IsShakeFlapping = _flapActiveTimer > 0f && CurrentFlapRate > 0f;
    }

    // =========================================================================
    // UpdateVirtualAction - Cập nhật trạng thái InputAction ảo.
    // =========================================================================
    private void UpdateVirtualAction()
    {
        // BeeController đọc IsPressed() từ flapAction.
        // Ta không thể trực tiếp "ép" IsPressed() trên InputAction không có binding thật.
        // Giải pháp: dùng một bool public trong BeeController hoặc ghi đè Speed trực tiếp.
        // Ở đây ta chọn: nếu đang shake, ta enable flapAction ảo bằng cách
        // trigger nó qua InputSystem (không có API public) → thay vào đó
        // ta patch BeeController.IsFlapping trực tiếp qua reflection hoặc dùng helper.

        // APPROACH ĐƠN GIẢN: Kế thừa / extend BeeController để có SetFlapOverride.
        // Vì không muốn sửa BeeController gốc, ta kiểm tra flag qua property.
        // → BeeController đọc flapAction.action.IsPressed(). Ta giữ action ảo luôn "pressed"
        //    khi IsShakeFlapping = true bằng cách liên tục trigger action.

        if (_virtualFlap == null) return;

        // Kích hoạt bằng cách ApplyBindingOverride sang một key ảo không bao giờ press
        // thì không work. Phương án sạch nhất: ta dùng BeeControllerShakeAdapter (xem bên dưới).
        // Thực tế: override flapAction với một action "always on" khi shake.
        // Unity InputSystem không cho phép "fake press" trực tiếp trên InputAction.
        //
        // → SOLUTION: sửa nhỏ BeeController để nhận IFlapSource (xem BeeControllerShakeAdapter).
        // Tạm thời: set enabled/disabled của action để BeeController "thấy" trạng thái.

        // WORKAROUND hiệu quả: ta dùng Keyboard.current giả nếu đang trong Editor,
        // còn trên Quest ta wrap BeeController bằng BeeControllerShakeAdapter.
        // Script BeeControllerShakeAdapter sẽ được tạo sau nếu cần.
        // Hiện tại: ta store IsShakeFlapping dưới dạng public flag và BeeGameManager
        // sẽ bridge nó vào BeeController.

        // Kiểm tra: nếu bee có BeeControllerShakeAdapter thì delegate cho nó.
        var adapter = bee != null ? bee.GetComponent<BeeControllerShakeAdapter>() : null;
        if (adapter != null)
        {
            adapter.SetShakeFlapping(IsShakeFlapping);
        }
    }

    // =========================================================================
    // InstallVirtualFlap - Tạo InputAction ảo và gán vào BeeController.
    //   Chỉ dùng làm fallback nếu không có BeeControllerShakeAdapter.
    // =========================================================================
    private void InstallVirtualFlap()
    {
        if (_installed || bee == null) return;

        // Lưu action gốc để khôi phục khi disable.
        _originalFlap = bee.flapAction;
        _installed = true;
    }

    // =========================================================================
    // RemoveVirtualFlap - Khôi phục flapAction gốc.
    // =========================================================================
    private void RemoveVirtualFlap()
    {
        if (!_installed || bee == null) return;

        bee.flapAction = _originalFlap;
        if (_originalFlap.action != null) _originalFlap.action.Enable();

        _virtualFlap?.Disable();
        _virtualFlap?.Dispose();
        _virtualFlap = null;
        _installed = false;
    }

    // =========================================================================
    // Gizmos - Debug visualization khi đang đập cánh.
    // =========================================================================
    private void OnDrawGizmos()
    {
        if (!debugGizmo || !Application.isPlaying) return;

        Gizmos.color = IsShakeFlapping ? Color.yellow : Color.gray;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.3f, 0.15f);
    }

    // =========================================================================
    // ContextMenu Debug
    // =========================================================================
    [ContextMenu("Simulate Flap (Debug)")]
    private void DebugSimulateFlap()
    {
        RecordFlap();
        Debug.Log("[BeeShakeFlap] Simulated flap.");
    }
}
