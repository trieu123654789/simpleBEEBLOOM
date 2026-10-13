// =============================================================================
// BeeController - Core flight for the bee avatar (single bee).
// BeeController - Điều khiển bay cốt lõi cho nhân vật ong.
//
// CONTROL MODEL / MÔ HÌNH ĐIỀU KHIỂN CHUẨN VR & PC
//   - Nhấn bay (Trigger / Button VR hoặc phím Space/Shift PC) -> BAY LÊN (Vertical Lift).
//   - Thả bay                                                -> Trọng lực hạ độ cao từ từ / lơ lửng / đáp đất.
//   - Joystick Y / Phím W, S (Đẩy tới / Kéo lui)             -> ĐI TỚI / ĐI LUI theo hướng ong đang quay.
//   - Joystick X / Phím A, D (Gạt trái / Gạt phải)           -> RẼ HƯỚNG (Yaw) + Nghiêng thân (Bank) + Strafe lách nhẹ.
//   - Thả cần / Thả phím                                     -> Dừng lại tại chỗ, KHÔNG tự động trôi một hướng!
// =============================================================================
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

public class BeeController : MonoBehaviour
{
    // =========================================================================
    // INPUT / ĐIỀU KHIỂN
    // =========================================================================
    [Header("Input Actions / Hành động đầu vào")]
    [Tooltip("Action 2D cho joystick trên tay cầm VR (Y = tới/lui, X = rẽ/lách). Gán trong Inspector.")]
    public InputActionProperty steerAction;

    [Tooltip("Action 2D cho cần xoay phụ trên tay cầm phải (tùy chọn).")]
    public InputActionProperty turnAction;

    [Tooltip("Action cho nút ĐẬP CÁNH / BAY LÊN. Dạng Button (giữ để bay lên).")]
    public InputActionProperty flapAction;

    [Tooltip("Cho phép điều khiển trực tiếp bằng bàn phím (W, A, S, D, Space).")]
    public bool enableKeyboardInput = true;

    [Header("VR Direct Input / Đọc trực tiếp tay cầm VR")]
    [Tooltip("Tự động đọc trực tiếp phần cứng VR Controller (Thumbstick, Trigger, Buttons) kể cả khi Action chưa bind.")]
    public bool enableDirectXrInput = true;

    [Tooltip("Cho phép đọc Joystick di chuyển (Thumbstick) từ VR Controller (luôn bật).")]
    public bool enableDirectXrMovement = true;

    [Tooltip("Cho phép Trigger/Grip bay trực tiếp (tắt khi dùng quẹt thanh ngang SwipeFlap).")]
    public bool enableDirectXrFlap = true;

    [Tooltip("Cho phép lắc/vung tay cầm để đập cánh bay (VR Shake Flap).")]
    public bool enableShakeToFlap = true;

    [Tooltip("Ngưỡng gia tốc lắc tay để đập cánh (m/s²). Lắc nhẹ trên Quest khoảng 2.5 - 3.5 m/s².")]
    public float shakeThreshold = 3.0f;

    // =========================================================================
    // FLIGHT SETTINGS / CÀI ĐẶT BAY
    // =========================================================================
    [Header("Vertical Flight / Bay lên - Rơi xuống")]
    [Tooltip("Lực nâng khi nhấn nút bay (m/s^2).")]
    public float flapLift = 14f;

    /// <summary>Tương thích ngược với thuộc tính cũ flapThrust.</summary>
    public float flapThrust
    {
        get => flapLift;
        set => flapLift = value;
    }

    [Tooltip("Tốc độ bay lên tối đa khi nhấn nút bay (m/s).")]
    public float maxAscendSpeed = 4.5f;

    [Tooltip("Trọng lực (m/s^2) kéo ong hạ độ cao khi thả nút bay.")]
    public float gravity = 4.5f;

    [Tooltip("Tốc độ rơi tối đa khi thả nút bay (m/s).")]
    public float maxFallSpeed = 4f;

    [Tooltip("Độ cao trần tối đa cho phép bay (m). 0 = không giới hạn.")]
    public float maxFlightAltitude = 20f;

    [Header("Bee Buoyancy & Hover / Độ nổi & Lơ lửng của ong")]
    [Tooltip("Bật cơ chế lơ lửng tự nhiên của loài ong (không rơi tụt xuống đất khi buông tay, ong tự cân bằng bồng bềnh).")]
    public bool enableBeeHover = true;

    [Tooltip("Tốc độ hạ độ cao nhẹ khi thả nút bay (m/s). 0.15 - 0.35m/s = lướt êm tự nhiên như côn trùng.")]
    [Range(0f, 2f)]
    public float hoverDescentRate = 0.22f;

    [Tooltip("Biên độ dao động thở bồng bềnh khi ong lơ lửng tại chỗ (m).")]
    [Range(0f, 0.05f)]
    public float hoverBobAmplitude = 0.015f;

    [Tooltip("Tần số dao động bồng bềnh khi lơ lửng (Hz).")]
    [Range(0.5f, 3f)]
    public float hoverBobFrequency = 1.5f;

    [Tooltip("Độ rập rình vi mô tự nhiên của loài ong (Micro-wander) tránh cảm giác đứng hình cơ học.")]
    [Range(0f, 0.01f)]
    public float organicJitterScale = 0.0025f;

    [Header("Horizontal Movement / Di chuyển tới - lui bằng Joystick")]
    [Tooltip("Tốc độ tiến tối đa khi đẩy joystick về phía trước (m/s).")]
    public float moveSpeed = 6f;

    /// <summary>Tương thích ngược với thuộc tính cũ maxSpeed.</summary>
    public float maxSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    [Tooltip("Tốc độ lùi tối đa khi kéo joystick về sau (m/s).")]
    public float reverseSpeed = 3.5f;

    [Tooltip("Tốc độ lách ngang sang hai bên khi gạt joystick (m/s).")]
    public float strafeSpeed = 3f;

    [Tooltip("Gia tốc tăng tốc khi gạt joystick / bấm phím (m/s^2).")]
    public float moveAcceleration = 12f;

    [Tooltip("Độ hãm phanh khi thả joystick / nhả phím (m/s^2). Càng lớn ong dừng càng dứt khoát như côn trùng thật.")]
    public float moveDeceleration = 14f;

    [Tooltip("Hệ số giảm tốc khi không di chuyển.")]
    [Range(0.1f, 5f)]
    public float glideDrag = 1.6f;

    [Header("Steering & Banking / Rẽ hướng & Nghiêng thân")]
    [Tooltip("Tốc độ xoay đầu (yaw) khi gạt joystick trái/phải hoặc phím A/D (độ/giây).")]
    public float yawSpeed = 140f;

    [Tooltip("Tốc độ nhìn lên/xuống tối đa khi chỉnh thủ công (độ/giây).")]
    public float pitchSpeed = 90f;

    [Tooltip("Giới hạn góc nhìn lên (độ).")]
    public float maxPitchUp = 40f;

    [Tooltip("Giới hạn góc nhìn xuống (độ).")]
    public float maxPitchDown = 60f;

    [Tooltip("Tự động nghiêng nhẹ mũi ong theo chuyển động tiến/lùi và bay lên/xuống.")]
    public bool dynamicPitch = true;

    [Tooltip("Góc nghiêng (bank) tối đa khi rẽ (độ).")]
    public float maxBankAngle = 25f;

    [Tooltip("Tốc độ xoay về phía cân bằng của góc nghiêng.")]
    public float bankSmoothSpeed = 6f;

    [Tooltip("Độ cong của hành động đầu vào. Lớn = nhạy hơn ở giữa cần.")]
    [Range(0.1f, 3f)]
    public float stickResponse = 1.2f;

    [Tooltip("Tốc độ quay giảm tối đa khi bay nhanh (0-0.9).")]
    [Range(0f, 0.9f)]
    public float speedTurnPenalty = 0.4f;

    // =========================================================================
    // GROUND COLLISION / VA CHẠM MẶT ĐẤT
    // =========================================================================
    [Header("Ground / Va chạm mặt đất")]
    [Tooltip("Bật để ong không rơi xuyên mặt đất.")]
    public bool groundCollision = true;

    [Tooltip("Khoảng cách ong giữ trên bề mặt đất (m).")]
    public float groundClearance = 0.35f;

    [Tooltip("Bán kính thân ong dùng cho quét hình cầu (m).")]
    public float bodyRadius = 0.25f;

    [Tooltip("Layer ong được đáp xuống. Để trống = dùng Physics.DefaultRaycastLayers.")]
    public LayerMask groundMask = 0;

    [Tooltip("Ma sát khi ong chạm đất: càng lớn ong dừng lại càng nhanh.")]
    public float groundFriction = 5f;

    // =========================================================================
    // STAMINA / THỂ LỰC
    // =========================================================================
    [Header("Stamina / Thể lực")]
    [Tooltip("Stamina tối đa.")]
    public float maxStamina = 100f;

    [Tooltip("Stamina tiêu hao mỗi giây khi đập cánh bay lên.")]
    public float staminaDrainPerSecond = 20f;

    [Tooltip("Stamina hồi mỗi giây khi thả nút bay.")]
    public float staminaRegenPerSecond = 15f;

    [Tooltip("Stamina tối thiểu cần có để đập cánh lại.")]
    public float minStaminaToFlap = 8f;

    [Tooltip("Thời gian phải nghỉ (giây) sau khi hết stamina.")]
    public float exhaustedLockDuration = 1.2f;

    // =========================================================================
    // EVENTS / SỰ KIỆN
    // =========================================================================
    /// <summary>Raised when stamina hits 0. / Phát khi hết stamina.</summary>
    public System.Action OnExhausted;

    /// <summary>Raised when stamina recovers. / Phát khi hồi lại stamina.</summary>
    public System.Action OnRecovered;

    // =========================================================================
    // PUBLIC STATE / TRẠNG THÁI
    // =========================================================================
    /// <summary>0 = hết, 1 = đầy. / 0 = cạn, 1 = đầy.</summary>
    public float StaminaNormalized => maxStamina > 0f ? _stamina / maxStamina : 0f;

    /// <summary>Bội số tốc độ di chuyển ngang (0 = đứng, 1 = tối đa).</summary>
    public float SpeedNormalized => moveSpeed > 0f ? Speed / moveSpeed : 0f;

    /// <summary>Đang nhấn nút bay để bay lên hay không.</summary>
    public bool IsFlapping { get; private set; }

    /// <summary>Override từ bên ngoài (shake adapter hoặc test script).</summary>
    public bool ExternalFlapOverride { get; set; }

    /// <summary>Nút đập cánh đang được nhấn hay không (bao gồm cả ExternalFlapOverride).</summary>
    public bool FlapPressed => ReadCombinedFlapInput();

    /// <summary>
    /// Đọc tín hiệu nút bay trực tiếp từ phần cứng / Input Action / Phím, KHÔNG bao gồm ExternalFlapOverride.
    /// Hỗ trợ cả Input Action, phần cứng VR Controller (Trigger/Button), lắc tay (Shake), và bàn phím.
    /// </summary>
    public bool RawHardwareFlapPressed
    {
        get
        {
            // 1. Input Action nếu đã cấu hình binding
            if (flapAction.action != null && flapAction.action.enabled && flapAction.action.IsPressed())
                return true;

            // 2. Đọc trực tiếp phần cứng VR Controller (Trigger / Grip / Nút A, B, X, Y)
            if (enableDirectXrInput && enableDirectXrFlap)
            {
                UpdateXrDevices();
                if (CheckXrFlapInput(_rightHand) || CheckXrFlapInput(_leftHand))
                    return true;

                // 3. Vung tay / Lắc tay đập cánh (Shake Flapping)
                if (CheckXrShakeFlap())
                    return true;
            }

            // 4. Bàn phím máy tính (dùng test trên Editor / PC)
            if (enableKeyboardInput && Keyboard.current != null)
            {
                var k = Keyboard.current;
                if (k.spaceKey.isPressed || k.leftShiftKey.isPressed || k.rightShiftKey.isPressed)
                    return true;
            }

            return false;
        }
    }

    /// <summary>Đẩy thẳng lên/xuống theo trục Y (m/s²).</summary>
    public float ExternalVerticalThrust { get; set; }

    /// <summary>Hệ số điều chỉnh trọng lực từ bên ngoài (cho trợ lực bay / lướt êm).</summary>
    public float ExternalGravityScale { get; set; } = 1.0f;

    /// <summary>Bật hãm phanh mềm khi dừng vỗ cánh (tránh rơi đột ngột).</summary>
    public bool SoftUpwardDamping { get; set; } = true;

    /// <summary>Thêm xung lực bay lên tức thời theo trục Y (dành cho vỗ cánh dạng nhịp / swipe flap trợ lực vừa phải).</summary>
    public void AddVerticalImpulse(float impulse)
    {
        if (impulse <= 0f) return;
        // Triệt tiêu đà rơi vừa phải, tạo lực nhấc tự nhiên theo từng nhịp đập cánh
        if (_verticalVelocity < 0f)
            _verticalVelocity = Mathf.MoveTowards(_verticalVelocity, 0.4f, impulse * 1.6f);
        else
            _verticalVelocity = Mathf.Min(maxAscendSpeed * 0.7f, _verticalVelocity + impulse * 0.7f);
    }

    /// <summary>Nâng/cúi mũi ong thêm góc (độ).</summary>
    public void AddPitch(float degrees)
    {
        _pitch = Mathf.Clamp(_pitch + degrees, -maxPitchDown, maxPitchUp);
    }

    /// <summary>Tự điều khiển hướng nhìn của ong từ script khác.</summary>
    public void SetOrientation(float yawDegrees, float pitchDegrees)
    {
        _yaw = Mathf.Repeat(yawDegrees, 360f);
        _pitch = Mathf.Clamp(pitchDegrees, -maxPitchDown, maxPitchUp);
        ApplyRotation();
    }

    /// <summary>Góc pitch hiện tại (độ).</summary>
    public float CurrentPitch => _pitch;

    /// <summary>Khóa di chuyển vật lý của BeeController (dùng cho test mode ngoài).</summary>
    public bool ExternalMovementLocked { get; set; }

    /// <summary>Khóa xoay hướng của BeeController (dùng cho test mode ngoài).</summary>
    public bool ExternalSteeringLocked { get; set; }

    /// <summary>Hệ số lực nâng khi đập cánh.</summary>
    public float ExternalFlapThrustScale { get; set; } = 1f;

    /// <summary>Giữ tốc độ tối thiểu khi đang bay có chủ đích.</summary>
    public void EnsureMinSpeed(float minSpeed)
    {
        if (ExternalMovementLocked) return;
        if (Mathf.Abs(_currentForwardSpeed) > 0.1f)
        {
            _currentForwardSpeed = Mathf.Max(_currentForwardSpeed, minSpeed);
        }
    }

    /// <summary>Đặt góc nghiêng (bank) mục tiêu khi rẽ.</summary>
    public void SetBank(float targetDegrees, float smoothSpeed)
    {
        float target = Mathf.Clamp(targetDegrees, -maxBankAngle, maxBankAngle);
        _bank = Mathf.Lerp(_bank, target,
                           1f - Mathf.Exp(-Mathf.Max(0f, smoothSpeed) * Time.deltaTime));
    }

    /// <summary>Tốc độ di chuyển ngang hiện tại (m/s).</summary>
    public float Speed { get; private set; }

    /// <summary>Vận tốc đứng hiện tại theo trục Y (m/s).</summary>
    public float VerticalVelocity => _verticalVelocity;

    /// <summary>Đang chạm đất hay không.</summary>
    public bool IsGrounded { get; private set; }

    /// <summary>Bán kính thân ong.</summary>
    public float BodyRadius => Mathf.Max(0.01f, bodyRadius);

    /// <summary>Đang kiệt sức hay không.</summary>
    public bool IsExhausted => _exhausted || _stamina <= 0f;

    /// <summary>Góc nhìn ngang của ong (độ, 0-360).</summary>
    public float Yaw => _yaw;

    /// <summary>Góc ngước lên / cúi xuống (độ, dương = ngước lên).</summary>
    public float Pitch => _pitch;

    /// <summary>Góc nghiêng khi rẽ (độ).</summary>
    public float Bank => _bank;

    /// <summary>Đọc giá trị input điều hướng 2D hiện tại.</summary>
    public Vector2 ReadSteerInput()
    {
        return ReadCombinedMovementInput();
    }

    // =========================================================================
    // INPUT READING / ĐỌC TÍN HIỆU ĐẦU VÀO TỔNG HỢP (VR + KEYBOARD)
    // =========================================================================
    private readonly System.Collections.Generic.List<XRInputDevice> _leftDevices = new System.Collections.Generic.List<XRInputDevice>();
    private readonly System.Collections.Generic.List<XRInputDevice> _rightDevices = new System.Collections.Generic.List<XRInputDevice>();
    private XRInputDevice _leftHand;
    private XRInputDevice _rightHand;
    private float _deviceSearchTimer;
    private float _shakeHoldTimer;

    private void UpdateXrDevices()
    {
        if (_leftHand.isValid && _rightHand.isValid) return;
        _deviceSearchTimer -= Time.unscaledDeltaTime;
        if (_deviceSearchTimer > 0f) return;
        _deviceSearchTimer = 1.0f;

        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, _leftDevices);
        if (_leftDevices.Count > 0) _leftHand = _leftDevices[0];

        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, _rightDevices);
        if (_rightDevices.Count > 0) _rightHand = _rightDevices[0];
    }

    private bool CheckXrFlapInput(XRInputDevice device)
    {
        if (!device.isValid) return false;

        // Bóp cò (Trigger)
        if (device.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool trigBtn) && trigBtn) return true;
        if (device.TryGetFeatureValue(XRCommonUsages.trigger, out float trigVal) && trigVal > 0.25f) return true;

        // Nút chính A/X hoặc B/Y
        if (device.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool btnPrimary) && btnPrimary) return true;
        if (device.TryGetFeatureValue(XRCommonUsages.secondaryButton, out bool btnSecondary) && btnSecondary) return true;

        // Nút bóp hông (Grip)
        if (device.TryGetFeatureValue(XRCommonUsages.gripButton, out bool gripBtn) && gripBtn) return true;
        if (device.TryGetFeatureValue(XRCommonUsages.grip, out float gripVal) && gripVal > 0.4f) return true;

        return false;
    }

    private bool CheckXrShakeFlap()
    {
        if (!enableShakeToFlap) return false;

        bool shaking = false;
        if (_rightHand.isValid && _rightHand.TryGetFeatureValue(XRCommonUsages.deviceAcceleration, out Vector3 rightAcc))
        {
            if (rightAcc.magnitude >= shakeThreshold) shaking = true;
        }
        if (!shaking && _leftHand.isValid && _leftHand.TryGetFeatureValue(XRCommonUsages.deviceAcceleration, out Vector3 leftAcc))
        {
            if (leftAcc.magnitude >= shakeThreshold) shaking = true;
        }

        if (shaking)
        {
            _shakeHoldTimer = 0.25f;
        }
        else if (_shakeHoldTimer > 0f)
        {
            _shakeHoldTimer -= Time.deltaTime;
        }

        return _shakeHoldTimer > 0f;
    }

    /// <summary>
    /// Đọc input điều hướng 2D kết hợp từ:
    /// - Tay cầm VR (steerAction nếu có gán, hoặc trực tiếp từ Left Thumbstick phần cứng)
    /// - Cần xoay phụ (turnAction hoặc Right Thumbstick)
    /// - Bàn phím máy tính (phím W, A, S, D hoặc Mũi tên)
    /// Y = đi tới / đi lui, X = rẽ trái / rẽ phải / lách ngang.
    /// <summary>
    /// Đọc input điều hướng di chuyển 2D (Left Stick hoặc WASD).
    /// Y = đi tới / đi lui, X = lách ngang (Strafe) / bẻ lái phụ.
    /// </summary>
    public Vector2 ReadCombinedMovementInput()
    {
        Vector2 input = Vector2.zero;

        // 1. Đọc từ tay cầm VR (steerAction nếu đã bind)
        if (steerAction.action != null)
        {
            if (!steerAction.action.enabled)
            {
                try { steerAction.action.Enable(); } catch { }
            }
            if (steerAction.action.enabled)
            {
                input = steerAction.action.ReadValue<Vector2>();
            }
        }

        // 1b. Fallback: Đọc trực tiếp từ Input System thiết bị XRController (OpenXR, Simulator, Quest)
        if (input.sqrMagnitude < 0.01f)
        {
            var leftStick = InputSystem.FindControl("<XRController>{LeftHand}/thumbstick") as UnityEngine.InputSystem.Controls.Vector2Control;
            if (leftStick != null)
            {
                Vector2 val = leftStick.ReadValue();
                if (val.sqrMagnitude > 0.01f) input = val;
            }

            if (input.sqrMagnitude < 0.01f)
            {
                var anyStick = InputSystem.FindControl("<XRController>/thumbstick") as UnityEngine.InputSystem.Controls.Vector2Control;
                if (anyStick != null)
                {
                    Vector2 val = anyStick.ReadValue();
                    if (val.sqrMagnitude > 0.01f) input = val;
                }
            }

            if (input.sqrMagnitude < 0.01f && Gamepad.current != null)
            {
                Vector2 gpStick = Gamepad.current.leftStick.ReadValue();
                if (gpStick.sqrMagnitude > 0.01f) input = gpStick;
            }
        }

        // 1c. Fallback trực tiếp từ phần cứng VR Controller (Legacy XR subsystem)
        if ((enableDirectXrMovement || enableDirectXrInput) && input.sqrMagnitude < 0.01f)
        {
            UpdateXrDevices();
            if (_leftHand.isValid && _leftHand.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 stick))
            {
                input = stick;
            }
        }

        // 2. Fallback / Test trực tiếp bằng bàn phím (W, A, S, D):
        if (enableKeyboardInput && Keyboard.current != null)
        {
            var k = Keyboard.current;
            float kX = 0f;
            float kY = 0f;

            if (k.wKey.isPressed) kY += 1f;
            if (k.sKey.isPressed) kY -= 1f;
            if (k.aKey.isPressed) kX -= 1f;
            if (k.dKey.isPressed) kX += 1f;

            if (Mathf.Abs(kX) > 0.01f || Mathf.Abs(kY) > 0.01f)
            {
                input.x = kX;
                input.y = kY;
            }
        }

        return ApplyDeadzoneAndResponse(input);
    }

    /// <summary>
    /// Đọc input xoay góc nhìn tự do 360 độ (Yaw) từ:
    /// - Tay cầm VR phải (turnAction hoặc Right Thumbstick phần cứng)
    /// - Gamepad (Right Stick X)
    /// - Bàn phím (Phím Q/E hoặc Mũi tên Trái/Phải)
    /// </summary>
    public float ReadCombinedTurnInput()
    {
        float turn = 0f;

        // 1. Đọc từ turnAction (nếu đã bind vào XRI RightHand Turn / SnapTurn)
        if (turnAction.action != null)
        {
            if (!turnAction.action.enabled)
            {
                try { turnAction.action.Enable(); } catch { }
            }
            if (turnAction.action.enabled)
            {
                Vector2 val = turnAction.action.ReadValue<Vector2>();
                if (Mathf.Abs(val.x) > 0.05f) turn = val.x;
            }
        }

        // 2. Fallback trực tiếp Input System thiết bị XRController tay phải
        if (Mathf.Abs(turn) < 0.05f)
        {
            var rightStick = InputSystem.FindControl("<XRController>{RightHand}/thumbstick") as UnityEngine.InputSystem.Controls.Vector2Control;
            if (rightStick != null)
            {
                Vector2 val = rightStick.ReadValue();
                if (Mathf.Abs(val.x) > 0.05f) turn = val.x;
            }
        }

        // 3. Fallback Legacy XR InputDevice tay phải
        if (Mathf.Abs(turn) < 0.05f && (enableDirectXrMovement || enableDirectXrInput))
        {
            UpdateXrDevices();
            if (_rightHand.isValid && _rightHand.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 rStick))
            {
                if (Mathf.Abs(rStick.x) > 0.05f) turn = rStick.x;
            }
        }

        // 4. Fallback Gamepad rightStick
        if (Mathf.Abs(turn) < 0.05f && Gamepad.current != null)
        {
            float gpTurn = Gamepad.current.rightStick.x.ReadValue();
            if (Mathf.Abs(gpTurn) > 0.05f) turn = gpTurn;
        }

        // 5. Fallback Bàn phím: Q (quay trái), E (quay phải) hoặc phím Mũi tên Trái / Phải
        if (enableKeyboardInput && Keyboard.current != null)
        {
            var k = Keyboard.current;
            float kTurn = 0f;
            if (k.qKey.isPressed || k.leftArrowKey.isPressed) kTurn -= 1f;
            if (k.eKey.isPressed || k.rightArrowKey.isPressed) kTurn += 1f;

            if (Mathf.Abs(kTurn) > 0.05f) turn = kTurn;
        }

        // Áp dụng deadzone
        if (Mathf.Abs(turn) < 0.12f) return 0f;

        // Curve response
        float sign = Mathf.Sign(turn);
        float scaled = (Mathf.Abs(turn) - 0.12f) / (1f - 0.12f);
        return sign * Mathf.Pow(Mathf.Clamp01(scaled), 1f / stickResponse);
    }

    /// <summary>
    /// Đọc input nhấn nút bay (Trigger / Button trên VR hoặc Space/Shift trên bàn phím).
    /// </summary>
    public bool ReadCombinedFlapInput()
    {
        if (ExternalFlapOverride) return true;

        return RawHardwareFlapPressed;
    }

    // =========================================================================
    // INTERNAL STATE / TRẠNG THÁI NỘI BỘ
    // =========================================================================
    private float _yaw;
    private float _pitch;
    private float _bank;
    private float _stamina;
    private bool _exhausted;
    private float _exhaustedTimer;

    private float _currentForwardSpeed;
    private float _currentStrafeSpeed;
    private float _verticalVelocity;

    private void Awake()
    {
        // Chống độn thổ: nếu vị trí ban đầu bị lưu lệch xuống lòng đất hoặc lọt vào tường ngoài
        if (transform.position.y < 0.5f || transform.position.z < -2f)
        {
            transform.position = new Vector3(3.84f, 1.60f, 3.46f);
            transform.rotation = Quaternion.identity;
        }

        _stamina = maxStamina;
        _yaw = transform.eulerAngles.y;
        _pitch = 0f;
        _bank = 0f;
        IsFlapping = false;
        _currentForwardSpeed = 0f;
        _currentStrafeSpeed = 0f;
        _verticalVelocity = 0f;
        Speed = 0f;
    }

    private void OnEnable()
    {
        steerAction.action?.Enable();
        turnAction.action?.Enable();
        flapAction.action?.Enable();
    }

    private void OnDisable()
    {
        steerAction.action?.Disable();
        turnAction.action?.Disable();
        flapAction.action?.Disable();
    }

    private void Update()
    {
        UpdateSteering();
        UpdateStamina();
        UpdateMovement();
        ApplyRotation();
    }

    // =========================================================================
    // UpdateSteering - Cần xoay phải / Phím Q-E xoay góc nhìn 360 độ tự do
    // =========================================================================
    private void UpdateSteering()
    {
        if (ExternalSteeringLocked) return;

        float turnVal = ReadCombinedTurnInput();
        Vector2 moveStick = ReadCombinedMovementInput();

        // Ưu tiên cần xoay tay phải / phím Q-E để quay góc nhìn 360 độ tự do
        // Nếu không xoay cần phải, cần trái gạt ngang vẫn hỗ trợ rẽ hướng tự nhiên
        float effectiveTurn = Mathf.Abs(turnVal) > 0.05f ? turnVal : moveStick.x;

        // Giảm nhẹ tốc độ quay khi đang bay nhanh
        float speedRatio = Mathf.Clamp01(Speed / Mathf.Max(0.1f, moveSpeed));
        float turnPenalty = Mathf.Lerp(1f, 1f - speedTurnPenalty, speedRatio);

        // Xoay hướng nhìn ngang (Yaw)
        float yawDelta = effectiveTurn * yawSpeed * turnPenalty * Time.deltaTime;
        _yaw = Mathf.Repeat(_yaw + yawDelta, 360f);

        // Nghiêng thân ong (Bank) theo độ rẽ
        float targetBank = -effectiveTurn * maxBankAngle;
        _bank = Mathf.Lerp(_bank, targetBank, 1f - Mathf.Exp(-bankSmoothSpeed * Time.deltaTime));
    }

    // =========================================================================
    // UpdateStamina - Nhấn bay tốn stamina, thả thì hồi
    // =========================================================================
    private void UpdateStamina()
    {
        bool flapPressed = ReadCombinedFlapInput();
        IsFlapping = flapPressed && !IsExhausted;

        if (IsFlapping)
        {
            _stamina -= staminaDrainPerSecond * Time.deltaTime;
            if (_stamina <= 0f)
            {
                _stamina = 0f;
                _exhausted = true;
                _exhaustedTimer = exhaustedLockDuration;
                IsFlapping = false;
                OnExhausted?.Invoke();
            }
        }
        else
        {
            _stamina += staminaRegenPerSecond * Time.deltaTime;
            _stamina = Mathf.Min(_stamina, maxStamina);
        }

        if (_exhausted)
        {
            _exhaustedTimer -= Time.deltaTime;
            if (_exhaustedTimer <= 0f && _stamina >= minStaminaToFlap)
            {
                _exhausted = false;
                OnRecovered?.Invoke();
            }
        }
    }

    // =========================================================================
    // UpdateMovement - Bay lên bằng nút bay, Đi tới / lui / rẽ bằng Joystick & WASD
    // =========================================================================
    private void UpdateMovement()
    {
        if (ExternalMovementLocked) return;

        Vector2 stick = ReadCombinedMovementInput();

        // ---------------------------------------------------------------------
        // 1. DI CHUYỂN NGANG THEO JOYSTICK / BÀN PHÍM (Forward / Backward / Strafe)
        // ---------------------------------------------------------------------
        // stick.y: Đẩy tới / phím W (>0) = đi tới; Kéo lui / phím S (<0) = đi lui
        float targetForward = stick.y * (stick.y >= 0f ? moveSpeed : reverseSpeed);
        float forwardAccel = Mathf.Abs(targetForward) > Mathf.Abs(_currentForwardSpeed)
            ? moveAcceleration
            : moveDeceleration;
        _currentForwardSpeed = Mathf.MoveTowards(_currentForwardSpeed, targetForward, forwardAccel * Time.deltaTime);

        // stick.x: Lách nhẹ sang hai bên khi rẽ
        float targetStrafe = stick.x * strafeSpeed;
        float strafeAccel = Mathf.Abs(targetStrafe) > Mathf.Abs(_currentStrafeSpeed)
            ? moveAcceleration
            : moveDeceleration;
        _currentStrafeSpeed = Mathf.MoveTowards(_currentStrafeSpeed, targetStrafe, strafeAccel * Time.deltaTime);

        Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
        Vector3 forwardDir = yawRotation * Vector3.forward;
        Vector3 rightDir = yawRotation * Vector3.right;
        Vector3 horizontalVelocity = forwardDir * _currentForwardSpeed + rightDir * _currentStrafeSpeed;

        Speed = horizontalVelocity.magnitude;

        // ---------------------------------------------------------------------
        // 2. DI CHUYỂN DỌC TRỤC Y (Nhấn nút bay = bay lên; Thả = lơ lửng bồng bềnh như ong thật)
        // ---------------------------------------------------------------------
        if (IsFlapping)
        {
            float liftAcc = flapLift * ExternalFlapThrustScale;
            _verticalVelocity = Mathf.MoveTowards(_verticalVelocity, maxAscendSpeed, liftAcc * Time.deltaTime);
        }
        else if (enableBeeHover)
        {
            // Ong thật có độ nổi tự nhiên cực lớn: khi không vỗ cánh mạnh, ong lướt êm hoặc lơ lửng
            // Chỉ hạ độ cao nhanh khi cố ý chúi mũi xuống
            float intentionalDive = (_pitch > 12f) ? Mathf.Clamp01((_pitch - 12f) / 30f) : 0f;
            float targetSink = Mathf.Lerp(-hoverDescentRate, -maxFallSpeed * 0.7f, intentionalDive);

            float effectiveGravity = gravity * Mathf.Max(0f, ExternalGravityScale);
            float damping = (_verticalVelocity > 0f) ? (effectiveGravity * (SoftUpwardDamping ? 1.4f : 3.0f)) : (effectiveGravity * 0.8f);
            _verticalVelocity = Mathf.MoveTowards(_verticalVelocity, targetSink, damping * Time.deltaTime);
        }
        else
        {
            // Chế độ rơi tự do thông thường
            float effectiveGravity = gravity * Mathf.Max(0f, ExternalGravityScale);
            float dampingScale = SoftUpwardDamping ? 1.0f : 3.0f;
            float damping = (_verticalVelocity > 0f) ? (effectiveGravity * dampingScale) : effectiveGravity;
            _verticalVelocity = Mathf.MoveTowards(_verticalVelocity, -maxFallSpeed, damping * Time.deltaTime);
        }

        // Lực nâng bổ sung từ bên ngoài (phím test Q/E hoặc assist)
        if (Mathf.Abs(ExternalVerticalThrust) > 0.001f)
        {
            _verticalVelocity += ExternalVerticalThrust * Time.deltaTime;
        }

        // Kẹp chặt vận tốc đứng trong ngưỡng an toàn, KHÔNG để vọt quá tốc độ cho phép
        _verticalVelocity = Mathf.Clamp(_verticalVelocity, -maxFallSpeed, maxAscendSpeed);

        // Trần bay tối đa (nếu có giới hạn)
        if (maxFlightAltitude > 0f && transform.position.y >= maxFlightAltitude)
        {
            if (_verticalVelocity > 0f) _verticalVelocity = 0f;
        }

        // ---------------------------------------------------------------------
        // 3. TỔNG HỢP VẬN TỐC, RẬP RÌNH TỰ NHIÊN & KIỂM TRA VA CHẠM MẶT ĐẤT
        // ---------------------------------------------------------------------
        Vector3 velocity = horizontalVelocity + Vector3.up * _verticalVelocity;

        // Thở bồng bềnh khi ong lơ lửng tại chỗ
        if (enableBeeHover && Speed < 0.6f && Mathf.Abs(_verticalVelocity) < 0.4f)
        {
            float hoverFloat = Mathf.Sin(Time.time * hoverBobFrequency * Mathf.PI * 2f) * (hoverBobAmplitude * 0.6f);
            velocity += Vector3.up * hoverFloat;
        }

        Vector3 next = transform.position + velocity * Time.deltaTime;

        // Rập rình vi mô tự nhiên của côn trùng (organic micro-wander)
        if (organicJitterScale > 0.0001f)
        {
            float jX = (Mathf.PerlinNoise(Time.time * 3f, 0.1f) - 0.5f) * organicJitterScale;
            float jY = (Mathf.PerlinNoise(0.1f, Time.time * 3f) - 0.5f) * organicJitterScale;
            next += yawRotation * new Vector3(jX, jY, 0f);
        }

        if (groundCollision)
        {
            next = ResolveGroundCollision(next, velocity);
        }

        if (maxFlightAltitude > 0f && next.y > maxFlightAltitude)
        {
            next.y = maxFlightAltitude;
        }

        transform.position = next;


        // ---------------------------------------------------------------------
        // 4. DYNAMIC PITCH (Mũi ong tự động hơi chúi khi tiến, ngước khi bay lên)
        // ---------------------------------------------------------------------
        if (dynamicPitch && !ExternalSteeringLocked)
        {
            float movePitch = -(_currentForwardSpeed / Mathf.Max(0.1f, moveSpeed)) * 8f;
            float liftPitch = Mathf.Clamp(_verticalVelocity / Mathf.Max(0.1f, maxAscendSpeed), -1f, 1f) * 6f;
            float targetPitch = movePitch + liftPitch;
            _pitch = Mathf.Lerp(_pitch, targetPitch, 1f - Mathf.Exp(-5f * Time.deltaTime));
        }
    }

    // =========================================================================
    // GROUND COLLISION / VA CHẠM MẶT ĐẤT
    // =========================================================================
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];
    private readonly RaycastHit[] _hitBuffer2 = new RaycastHit[16];

    private bool IsIgnoredCollider(Collider col)
    {
        if (col == null) return true;
        Transform t = col.transform;
        // Bỏ qua chính con ong và toàn bộ các bộ phận con cháu của nó (Head, Body...)
        if (t == transform || t.IsChildOf(transform)) return true;
        return false;
    }

    private void PickGround(RaycastHit h, ref RaycastHit best, ref bool hasHit,
                            ref float bestDist, float ceiling)
    {
        if (h.collider == null) return;
        if (IsIgnoredCollider(h.collider)) return; // BỎ QUA CHÍNH MÌNH ĐỂ KHÔNG BỊ TỰ BẮN LÊN TRỜI!
        if (h.normal.y < 0.3f) return;
        if (h.point.y > ceiling) return;

        float d = Vector3.SqrMagnitude(h.point - transform.position);
        if (d < bestDist) { bestDist = d; best = h; hasHit = true; }
    }

    public Vector3 ResolveGroundCollision(Vector3 desiredPosition, Vector3 motion)
    {
        IsGrounded = false;

        int mask = (groundMask.value != 0) ? groundMask.value : Physics.DefaultRaycastLayers;
        float r = BodyRadius;
        float clear = Mathf.Max(0f, groundClearance);

        float dist = Mathf.Max(motion.magnitude, 0.1f) + r + clear;
        Vector3 from = new Vector3(desiredPosition.x, transform.position.y + r, desiredPosition.z);
        int downCount = Physics.SphereCastNonAlloc(
            from, r, Vector3.down, _hitBuffer, dist, mask, QueryTriggerInteraction.Ignore);

        int motionCount = 0;
        // Chỉ quét theo hướng chuyển động nếu đang hạ cánh / rơi xuống đất (motion.y <= 0)
        if (motion.y <= 0.05f && motion.sqrMagnitude > 0.0001f)
        {
            motionCount = Physics.SphereCastNonAlloc(
                transform.position, r, motion.normalized, _hitBuffer2, motion.magnitude + clear, mask,
                QueryTriggerInteraction.Ignore);
        }

        RaycastHit best = default;
        bool hasHit = false;
        float bestDist = float.MaxValue;
        float ceiling = transform.position.y + r;
        for (int i = 0; i < downCount; i++) PickGround(_hitBuffer[i], ref best, ref hasHit, ref bestDist, ceiling);
        for (int i = 0; i < motionCount; i++) PickGround(_hitBuffer2[i], ref best, ref hasHit, ref bestDist, ceiling);

        if (!hasHit) return desiredPosition;

        // Điểm chạm thực tế trên bề mặt đất + khoảng cách giữ an toàn clear
        float floorY = best.point.y + clear;

        if (desiredPosition.y < floorY)
        {
            desiredPosition.y = floorY;
            IsGrounded = true;

            if (_verticalVelocity < 0f) _verticalVelocity = 0f;

            // Ma sát mặt đất khi hạ cánh
            _currentForwardSpeed = Mathf.MoveTowards(_currentForwardSpeed, 0f, groundFriction * Time.deltaTime);
            _currentStrafeSpeed = Mathf.MoveTowards(_currentStrafeSpeed, 0f, groundFriction * Time.deltaTime);
            Speed = Mathf.Max(0f, Speed - Speed * groundFriction * Time.deltaTime);
        }

        return desiredPosition;
    }

    // =========================================================================
    // ApplyRotation - Quay con ong theo yaw/pitch/bank đã tính
    // =========================================================================
    private void ApplyRotation()
    {
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f)
                           * Quaternion.Euler(-_pitch, 0f, _bank);
    }

    // =========================================================================
    // ApplyDeadzoneAndResponse - Vùng chết + độ cong tay cầm
    // =========================================================================
    private Vector2 ApplyDeadzoneAndResponse(Vector2 raw)
    {
        const float deadzone = 0.15f;
        float magnitude = raw.magnitude;

        if (magnitude < deadzone)
        {
            return Vector2.zero;
        }

        float scaledMagnitude = (magnitude - deadzone) / (1f - deadzone);
        Vector2 direction = raw / magnitude;
        float curvedMagnitude = Mathf.Pow(scaledMagnitude, 1f / stickResponse);

        return direction * Mathf.Clamp01(curvedMagnitude);
    }

    // =========================================================================
    // PUBLIC API / HÀM CÔNG KHAI
    // =========================================================================
    public void Teleport(Vector3 position, float yawDegrees)
    {
        transform.position = position;
        _yaw = Mathf.Repeat(yawDegrees, 360f);
        _pitch = 0f;
        _bank = 0f;
        _currentForwardSpeed = 0f;
        _currentStrafeSpeed = 0f;
        _verticalVelocity = 0f;
        Speed = 0f;
        _stamina = maxStamina;
        _exhausted = false;
        _exhaustedTimer = 0f;
        ApplyRotation();
    }

    public void StopFlying()
    {
        _currentForwardSpeed = 0f;
        _currentStrafeSpeed = 0f;
        _verticalVelocity = 0f;
        Speed = 0f;
        IsFlapping = false;
    }
}
