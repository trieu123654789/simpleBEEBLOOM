// =============================================================================
// BeeKeyboardAssist - Giup dap CANH bang Space/Shift khi dang test voi
// XR DEVICE SIMULATOR.
//
// VI SAO CAN
//   XR Device Simulator dieu khien tay cam ao (theo dong "Selected Device Input"),
//   con Space/Shift tren ban phim thi KHONG duoc simulator gui vao action nao.
//   Ong bay duoc (Move cua tay cam) nhung khong dap duoc canh de bay len.
//   Script nay CHI giu phim dap canh, KHONG dung vao movement -> khong tranh
//   input voi simulator.
//
// CACH DUNG
//   Gac component nay len cung object voi BeeController (BeeRoot).
//   Phim: Space = dap canh | Shift (giu) = dap canh lien tuc (leo nhanh)
// =============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Nguồn điều khiển ong. VRController = tay cầm thật (Quest): thumbstick lái,
/// trigger vỗ cánh. Keyboard = test nhanh trong Editor bằng WASD/Space.
/// </summary>
public enum ControlScheme
{
    VRController = 0,
    Keyboard = 1
}

[DisallowMultipleComponent]
public class BeeKeyboardAssist : MonoBehaviour
{
    [Header("References")]
    public BeeController bee;

    [Header("Keys / Phim")]
    [Tooltip("Space = dap canh.")]
    public bool useSpace = true;

    [Tooltip("Shift = dap canh lien tuc.")]
    public bool useShift = true;

    [Tooltip("Bat them Q/E de leo - xuong truc tiep (can simulator KHONG dung chung phim nay).")]
    public bool alsoQeVertical = false;

    [Tooltip("Luc day doc khi bat Q/E.")]
    public float verticalThrust = 7f;

    [Tooltip("Space/Shift CHI bay len (khong day toi truoc). Tat neu muon vua len vua toi.")]
    public bool flapLiftOnly = true;

    [Tooltip("Do nhung mui khi dap canh (do/giay) - 0 = giu nguyen huong mui.")]
    public float autoNoseUp = 0f;

    [Tooltip("Hien dong huong dan tren man hinh.")]
    public bool showHelp = true;

    [Tooltip("Tuu dong tim BeeFlapping de day cuong do voc canh khi dap.")]
    public bool autoFindFlapping = true;

    private BeeFlapping _flappingFx;

    [Tooltip("Tu nang ong bang cach dich transform o LateUpdate (khong phu thuoc BeeController).")]
    public bool directLift = false;

    [Tooltip("Toc do leo toi da khi giu Space (m/s) - ong that bay nhe, khong bay thut tren troi.")]
    public float maxLiftSpeed = 2.6f;

    [Tooltip("Toc do roi toi da (m/s) - roi nhanh hon bay.")]
    public float maxFallSpeed = 9f;

    [Tooltip("Trong luc: tha phim -> ong roi nhanh dan (khong ro doi lung).")]
    public float gravity = 14f;

    [Tooltip("Gia toc khi dap canh (m/s^2) - bam mot cai chi nhe len, giu moi bay cao.")]
    public float flapLift = 5.5f;

    [Header("Control scheme / Nguồn điều khiển")]
    [Tooltip("VRController = tay cam VR (thumbstick lai, trigger voc canh). " +
             "Keyboard = WASD/Space de test trong Editor.")]
    public ControlScheme controlScheme = ControlScheme.VRController;

    [Tooltip("Khi VR: dap canh co day ong TOI TRUOC (bay that su) hay chi bay len?")]
    public bool flapLiftOnlyInVR = true;

    [Tooltip("Ong luon 'giang' toi thieu khi bay. Tat neu muon dung yen khi tha phim.")]
    public float cruiseSpeed = 0f;

    [Tooltip("Cho phep Space/Shift van vao duoc khi dang che do VR (de test trong Editor).")]
    public bool allowKeyboardFallback = true;

    [Tooltip("Lai mui ong bang WASD khi tay cam XR ao KHONG phai nhan (simulator chon HMD / action rong).")]
    public bool keyboardSteer = true;

    [Tooltip("Toc do quay ngang khi bam A/D (do/giay).")]
    public float keyYawSpeed = 200f;

    [Tooltip("Toc do nhung len/xuong khi bam W/S (do/giay).")]
    public float keyPitchSpeed = 100f;

    private float _liftVel;
    private float _keyYaw;
    private float _keyPitch;
    private bool _keyYawInit;

    [Tooltip("In so lieu chan doan ra Console moi giay de kiem tra vi sao ong khong bay.")]
    public bool debugLog = true;

    private float _debugTimer;

    [Tooltip("Tu tat BeeAltitudeHold (script ghim ong o cao 1.5m) - bat buoc de bay len duoc.")]
    public bool disableAltitudeHold = true;

    private readonly List<Behaviour> _disabledBehaviours = new List<Behaviour>();

    private void Awake()
    {
        ResolveBee();
    }

    /// <summary>Tim BeeController: cung object truoc, sau do tim ca scene.</summary>
    private void ResolveBee()
    {
        if (bee != null) return;
        bee = GetComponent<BeeController>();
        if (bee == null) bee = FindObjectOfType<BeeController>();
        if (bee == null)
            Debug.LogError("[BeeKeyboardAssist] KHONG tim thay BeeController trong scene! " +
                           "Script nay phai gan cung object voi BeeController (BeeRoot).");
        else if (bee.gameObject != gameObject)
            Debug.LogWarning("[BeeKeyboardAssist] BeeController nam tren object '" + bee.gameObject.name +
                             "' khac voi script nay (" + name + ") - da tu tim toan scene.");
    }

    private void OnEnable()
    {
        if (!disableAltitudeHold) return;

        foreach (MonoBehaviour mb in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (mb == null || mb == this) continue;
            if (mb.GetType().Name != "BeeAltitudeHold") continue;
            if (_disabledBehaviours.Contains(mb)) continue;

            _disabledBehaviours.Add(mb);
            mb.enabled = false;
            Debug.Log("[BeeKeyboardAssist] Da TAT BeeAltitudeHold tren '" + mb.gameObject.name +
                      "' - truoc day ong bi ghim o cao 1.5m nen Space khong bay len duoc.");
        }
    }

    private void OnDisable()
    {
        foreach (Behaviour b in _disabledBehaviours)
            if (b != null) b.enabled = true;
        _disabledBehaviours.Clear();
    }

    private void Update()
    {
        Keyboard k = Keyboard.current;

        // Luon in chan doan TRUOC, de biet chính xac o nao dang chan ong.
        if (debugLog && Time.unscaledTime - _debugTimer > 1f)
        {
            _debugTimer = Time.unscaledTime;
            LogDiag(k);
        }

        if (bee == null) { ResolveBee(); if (bee == null) return; }

        bool vr = (controlScheme == ControlScheme.VRController);

        // KHONG duoc return khi khong co ban phim: VR that (Quest standalone) khong co
        // Keyboard.current -> neu return thi khong sinh luc nang -> vao VR dap canh
        // chi chay toi roi roi xuong. Nguon dap canh lay ca tu tay cam VR.
        bool flap = IsFlapRequested(k, vr);

        // Quan trong: PHAI gai override nay, khong thi IsFlapping = false ->
        // BeeCameraRig.UpdateBob() dung phase (khong con "lac lac" khi bay) va
        // BeeFlapping khong biet dang dap manh.
        bee.ExternalFlapOverride = flap;

        // Day cuong do voc canh cho BeeFlapping (1 = dap het suc, 0 = dap thuong).
        //   Neu khong set, ExternalFlapBoost = -1 va BeeFlapping se khong nang
        //   bien do/nhip len khi bam -> canh thay nhu dap lu bi dong.
        if (autoFindFlapping && _flappingFx == null)
            _flappingFx = GetComponent<BeeFlapping>();
        if (_flappingFx == null && autoFindFlapping)
            _flappingFx = FindObjectOfType<BeeFlapping>();
        if (_flappingFx != null)
            _flappingFx.ExternalFlapBoost = flap ? 1f : 0f;

        // Luc nang khi dap canh (ca VR lan ban phim).
        bee.ExternalVerticalThrust = flap ? flapLift : 0f;

        // VR: dap canh day ong TOI TRUOC (bay that su) neu bat flapLiftOnlyInVR.
        // Keyboard: giu cau hinh cu de ong chi bay len khi bam Space.
        bool liftOnly = vr ? flapLiftOnlyInVR : flapLiftOnly;
        bee.ExternalFlapThrustScale = liftOnly ? 0f : 1f;

        // Nghieng nhe mui len khi dang dap canh (tuy chon).
        if (flap && autoNoseUp > 0f) bee.AddPitch(autoNoseUp * Time.deltaTime);

        // ===== LAI HUONG =====
        // BeeController tu xu ly tat ca dieu huong (ca tay cam VR lan ban phim WASD).
        // Khong khoa ExternalSteeringLocked de BeeController luon nhan input.
        if (bee.ExternalSteeringLocked)
            bee.ExternalSteeringLocked = false;

        if (k == null) return;

        if (alsoQeVertical)
        {
            float up = (k.eKey.isPressed ? 1f : 0f) - (k.qKey.isPressed ? 1f : 0f);
            bee.ExternalVerticalThrust = up * verticalThrust;
        }
    }

    /// <summary>
    /// Có tay cầm XR nào đang hoạt động không (Quest thật / XR Device Simulator)?
    /// Không có -> script tự chuyển sang lái WASD để vẫn test được trong Editor.
    /// </summary>
    private bool HasXrController()
    {
        // 1) Tay cam XR (that HOAC simulator) - qua Input System.
        //    Ca Quest that va XR Device Simulator deu tao device ke thua XRController,
        //    nen check bang type la du (GetDevice<T>() tra null neu khong co).
        UnityEngine.InputSystem.XR.XRController ctrl =
            UnityEngine.InputSystem.InputSystem.GetDevice<
                UnityEngine.InputSystem.XR.XRController>();
        if (ctrl != null && ctrl.added) return true;

        // 2) Thiet bi XR that - qua subsystem XR Legacy (du phong).
        //    InputDevice chi co isValid (khong co isAttached).
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(
            UnityEngine.XR.XRNode.LeftHand, _xrDevices);
        for (int i = 0; i < _xrDevices.Count; i++)
            if (_xrDevices[i].isValid) return true;

        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(
            UnityEngine.XR.XRNode.RightHand, _xrDevices);
        for (int i = 0; i < _xrDevices.Count; i++)
            if (_xrDevices[i].isValid) return true;

        return false;
    }

    private readonly System.Collections.Generic.List<UnityEngine.XR.InputDevice> _xrDevices =
        new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();

    /// <summary>
    /// Có đang yêu cầu đập cánh không? Nguồn chính = tay cầm VR (XRI "Activate"),
    /// nguồn phụ = Space/Shift (chỉ khi cho phép fallback).
    /// </summary>
    private bool IsFlapRequested(Keyboard k, bool vr)
    {
        // --- NGUON VR: trigger/ nut tren tay cam (XRI "Activate"). Uu tien khi choi VR. ---
        if (bee != null && bee.FlapPressed) return true;

        // Cho phep Space/Shift van hoat dong khi dang test trong Editor (khong co headset).
        if (k == null || (vr && !allowKeyboardFallback)) return false;

        // Chi doc phim THAT. KHONG doc lai ExternalFlapOverride vi day la GIA TRI
        // script nay tu ghi -> se tao vong lap kep: bam 1 lan la ong voc canh mai mai.
        return (useSpace && k.spaceKey.isPressed)
            || (useShift && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed));
    }

    /// <summary>
    /// Lái mũi ong bằng WASD. W/S = ngước/cúi, A/D = rẽ trái/phải.
    /// Chỉ 1 bên được lái: khi script này bật thì BeeController khoá phần lái XR.
    /// </summary>
    private void ApplyKeyboardSteering(Keyboard k)
    {
        float x = 0f, y = 0f;
        if (k.aKey.isPressed) x -= 1f;
        if (k.dKey.isPressed) x += 1f;
        if (k.sKey.isPressed) y -= 1f;
        if (k.wKey.isPressed) y += 1f;

        // Nghieng (bank) LUON duoc cap nhat (0 khi khong re) -> tu ve bay thang.
        SetBankSmooth(-x);

        float dt = Time.deltaTime;
        if (x == 0f && y == 0f)
        {
            // Khong bam gi -> giu nguyen huong hien tai (ong bay thang ong da chi).
            _keyYaw = bee.Yaw;
            _keyPitch = bee.Pitch;
            _keyYawInit = true;
            return;
        }

        // Doc goc hien tai lai moi frame (khong cache qua nhieu frame).
        if (!_keyYawInit)
        {
            _keyYaw = bee.Yaw;
            _keyPitch = bee.Pitch;
            _keyYawInit = true;
        }

        // Huong bay: goc moi frame = goc cu + nhan suot (khong giat, khong phich hoi).
        //   keyYawSpeed/keyPitchSpeed la toc do DO/GIAY.
        _keyYaw = Mathf.Repeat(_keyYaw + x * keyYawSpeed * dt, 360f);
        _keyPitch = Mathf.Clamp(_keyPitch + y * keyPitchSpeed * dt,
                                -bee.maxPitchDown, bee.maxPitchUp);

        bee.SetOrientation(_keyYaw, _keyPitch);
    }

    /// <summary>Nghiêng thân ong khi rẽ (độ) — mượt, tự về 0 khi thả phím.</summary>
    private void SetBankSmooth(float turnAxis)
    {
        float target = turnAxis * 30f;   // 0 = bay thẳng, ±30 = rẽ hết
        bee.SetBank(target, 6f);
    }

    /// <summary>
    /// Nang ong TRUC TIEP o LateUpdate (chay sau BeeController.Update va
    /// BeeAltitudeHold) nen khong con script nao ghi de duoc.
    /// </summary>
    private void LateUpdate()
    {
        if (!directLift) return;
        if (bee == null) return;

        // KHONG return khi k == null: o VR that khong co ban phim, van phai
        // tinh luc nang de tay cam VR dap canh la bay duoc len.
        Keyboard k = Keyboard.current;
        bool flap = IsFlapRequested(k, controlScheme == ControlScheme.VRController);

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (flap)
        {
            // Dap canh: gia toc len nhe, co gioi han nen bam mot cai chi nhe bay len.
            _liftVel = Mathf.Min(_liftVel + flapLift * dt, maxLiftSpeed);
        }
        else
        {
            // Tha phim: trong luc keo ong roi xuong, roi nhanh dan (khong 'roi lung').
            _liftVel = Mathf.Max(_liftVel - gravity * dt, -maxFallSpeed);
        }

        Vector3 motion = Vector3.up * (_liftVel * dt);
        Vector3 next = bee.transform.position + motion;

        // CHAN MAT DAT: bee khong co Rigidbody nen phai tu quet. Ong se 'dung yen
        // tren be mat dat' thay vi roi xuyen qua (cung dung khi bay xuong doc).
        next = bee.ResolveGroundCollision(next, motion);

        if (Mathf.Abs(_liftVel) > 0.0001f || next != bee.transform.position)
            bee.transform.position = next;

        // Khi dang dap xuong duoi day ma da cham dat -> dung lại, khong rut xuong mat.
        if (bee.IsGrounded && _liftVel < 0f) _liftVel = 0f;

        // Ong luon 'giang' ve phia mui nhu ong thay vi dung yen khi khong dap.
        // KHONG ap khi ong dang cham dat (se lam ong 'tro' tren be).
        if (cruiseSpeed > 0f && !bee.IsGrounded) bee.EnsureMinSpeed(cruiseSpeed);
    }

    private void LogDiag(Keyboard k)
    {
        if (bee == null)
        {
            Debug.Log("[BeeAssist] bee = NULL (chua gan / khong tim thay BeeController) | " +
                      "object dang gan: '" + gameObject.name + "' | enabled=" + enabled +
                      " | keyboard=" + (k != null));
            return;
        }

        Debug.Log(string.Format(
            "[BeeAssist] space={0} | flap={1} IsFlapping={2} Speed={3:F2} liftY={4:F1} " +
            "| pos.Y={5:F2} | liftVel={13:F2} | dt={6:F4} timeScale={7:F2} | beeGO='{8}' enabled={9} active={10} " +
            "| AltHold={11} lock={12} grounded={14} | LAI yaw={15:F1} pitch={16:F1} bank={17:F1} " +
            "| keySteer={18} lockSteer={19} | xR={20:F2} yR={21:F2}",
            k != null && k.spaceKey.isPressed,
            bee.ExternalFlapOverride, bee.IsFlapping, bee.Speed,
            bee.ExternalVerticalThrust,
            bee.transform.position.y, Time.deltaTime, Time.timeScale,
            bee.gameObject.name, bee.enabled, bee.gameObject.activeInHierarchy,
            IsAltitudeHoldActive() ? "BAT" : "tat", bee.ExternalMovementLocked, _liftVel,
            bee.IsGrounded,
            bee.Yaw, bee.Pitch, bee.Bank,
            keyboardSteer, bee.ExternalSteeringLocked,
            bee.ReadSteerInput().x, bee.ReadSteerInput().y));
    }

    private float GroundDistance()
    {
        RaycastHit hit;
        Vector3 o = bee.transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(o, Vector3.down, out hit, 500f)) return hit.distance;
        return -1f;
    }

    private bool IsAltitudeHoldActive()
    {
        foreach (MonoBehaviour mb in FindObjectsOfType<MonoBehaviour>(true))
            if (mb != null && mb.GetType().Name == "BeeAltitudeHold" && mb.enabled) return true;
        return false;
    }

    private void OnGUI()
    {
        if (!showHelp) return;

        if (controlScheme == ControlScheme.VRController)
        {
            GUI.Label(new Rect(10, 46, 760, 20),
                "VR: Trigger (tay cam) = voc canh de bay | Thumbstick = lai huong. " +
                (HasXrController() ? "" : "(Khong thay tay cam XR -> dang dung WASD/Space)"));
        }
        else
        {
            GUI.Label(new Rect(10, 46, 760, 20),
                "SPACE / SHIFT = dap canh. W/S = nguc/cui, A/D = re. " +
                "Chuyen sang 'VRController' de bay bang tay cam.");
        }
    }
}