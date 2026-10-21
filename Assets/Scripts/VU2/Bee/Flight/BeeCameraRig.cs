// =============================================================================
// BeeCameraRig - FIRST-PERSON VR rig cho vai con ong (giống ảnh mẫu: Bee POV).
// BeeCameraRig - Rig VR goc nhin thu nhat cho nhan vat ong.
//
// VI SAO KHONG DUNG VAO POSE DA TRACK CUA CAMERA
//   Camera trong XR Origin duoc XR dat vao moi frame tu head tracking.
//   Neu ta gan thang position/rotation cho camera thi head tracking bi pha vo
//   (chong mat, troi khoi dau). Cach DUNG la:
//     -> DICH CHUYEN XR Origin (player rig) sao cho camera toi dung vi tri
//        "mat" cua con ong. Camera giu nguyen local pose da track.
//     -> Xoay XR Origin theo yaw/pitch/bank cua con ong.
//   Ket qua: nguoi choi van nhin quanh dau tu do, ma the gioi van bay theo ong.
//
// HIERARCHY KHUYEN NGHI
//   XR Origin                  <- playerRig (gan vao day) + BeeController
//   |-- Camera Offset
//   |   `-- Main Camera        <- trackedCamera (KHONG bi script nay set transform)
//   `-- BeeVisual              <- visualRoot (Body + Wing_L + Wing_R)
//
//   HOAC gan BeeController + script nay len chinh XR Origin: script tu phat hien
//   (playerRig == transform) va khong dich chuyen gi them -> tranh vong lap.
//
// VE VIEC TAT XRI LOCOMOTION
//   ActionBasedContinuousMoveProvider / SnapTurn / Teleportation se giat nguoi
//   choi nguoc huong bay. Script tu tim va tat cac provider do, cung
//   CharacterController (dang bay thi khong can capsule).
// =============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class BeeCameraRig : MonoBehaviour
{
    /// <summary>Che do goc nhin. / Camera view mode.</summary>
    public enum ViewMode
    {
        /// <summary>Nguoi choi chinh la con ong (bee POV).</summary>
        FirstPersonBee = 0,

        /// <summary>Camera sau lung ong, dung de debug.</summary>
        ThirdPersonDrone = 1
    }

    // =========================================================================
    // VIEW MODE / CHE DO GOC NHIN
    // =========================================================================
    [Header("View Mode / Che do goc nhin")]
    [Tooltip("FirstPersonBee = mat trong than ong (nhu anh mau).")]
    public ViewMode viewMode = ViewMode.FirstPersonBee;

    [Tooltip("FOV cua camera khi choi. Mo rong ra het (100-110 do) cho goc nhin toan canh thoang dang.")]
    [Range(60f, 120f)]
    public float targetFov = 105f;

    [Tooltip("Mo rong toi da goc nhin VR theo thau kinh cua kinh (Quest 3 ~110 do), khong bi bop hep hay go bo FOV.")]
    public bool unlockWideFov = true;


    // =========================================================================
    // EYE OFFSET / VI TRI MAT CUA CON ONG
    // =========================================================================
    [Header("Eye Offset / Vi tri mat con ong")]
    [Tooltip("Vi tri 'mat' so voi BeeRoot (local). Cao hon than, hoi truoc mui.")]
    public Vector3 eyeOffset = new Vector3(0f, 0.08f, 0.16f);

    [Tooltip("Offset goc nhin (do) so voi huong bay. / Look angle offset.")]
    public Vector3 eyeEulerOffset = Vector3.zero;

    // =========================================================================
    // RIG MOTION / CACH XR ORIGIN THEO CON ONG
    // =========================================================================
    [Header("Rig Motion / Chuyen dong rig")]
    [Tooltip("Ap yaw cua ong len XR Origin. Luon nen bat.")]
    public bool applyYawToRig = true;

    [Tooltip("Ap pitch len XR Origin (the gioi nghieng khi ong len/xuong). " +
             "Tat neu thay chong mat - khi do chi nhin bang dau.")]
    public bool applyPitchToRig = false;

    [Tooltip("He so nhan pitch (0-1) de giam say khi bat ap pitch.")]
    [Range(0f, 1f)]
    public float pitchInfluence = 0.4f;

    [Tooltip("Ap bank (nghieng khi re) len XR Origin. Tat de chan troi luon bang phang, goc nhin tu do 100% khong bi nghieng lac.")]
    public bool applyBankToRig = false;

    [Range(0f, 1f)]
    public float bankInfluence = 0.55f;

    [Tooltip("Cui nhe xuong khi bay nhanh -> cam giac luot sat mat nuoc nhu anh mau.")]
    public bool applySpeedPitchDown = true;

    [Tooltip("Do cui xuong khi bay het toc (do). Nen <= 12 de khong gay say.")]
    [Range(0f, 25f)]
    public float speedPitchDown = 10f;

    [Tooltip("Bam mem vi tri rig. Tat = dich chuyen cung (khuyen nghi cho VR).")]
    public bool smoothRigPosition = false;

    [Range(1f, 60f)]
    public float rigSmoothSpeed = 40f;

    // =========================================================================
    // VR-SAFE HEAD & BODY VIBRATION / RUNG AN TOÀN TRONG VR (0% CHÓNG MẶT)
    // =========================================================================
    [Header("VR-Safe Head Vibration / Rung vi mo goc nhin (0% chong mat)")]
    [Tooltip("Bat dao dong vi mo cuc nhe cua camera theo nhip canh ong. Chi dao dong theo truc UP 2-3mm, KHONG lac ngang, KHONG nghieng chan troi.")]
    public bool enableHeadBob = true;

    [Tooltip("Bien do rung cua camera (m). 0.0028 = 2.8mm: nguoi choi cam nhan ro nhung hoan toan khong say.")]
    [Range(0f, 0.008f)]
    public float headBobAmplitude = 0.0028f;

    [Tooltip("Bien do rung visualRoot (than/canh ong) theo nhip canh (m). Chi hien o ria man hinh.")]
    [Range(0f, 0.04f)]
    public float bobAmplitude = 0.006f;

    [Tooltip("Tan so rung (Hz) khi bay het toc. Khop voi nhip canh cua BeeFlapping.")]
    [Range(1f, 30f)]
    public float bobFrequency = 14f;

    [Tooltip("Bien do xoay nhe (do) theo nhip canh cua visualRoot.")]
    [Range(0f, 3f)]
    public float bobRollAmplitude = 0.6f;

    [Tooltip("Micro-pulse FOV theo nhip canh (do). Tao cam giac gio luot ma khong chong mat.")]
    [Range(0f, 2f)]
    public float bobFovPulse = 0.45f;

    [Tooltip("Bien do rung ngang nhe cua visualRoot (m).")]
    [Range(0f, 0.01f)]
    public float bobLateralAmplitude = 0.001f;

    [Tooltip("Tan so rung ngang cao (Hz).")]
    [Range(10f, 40f)]
    public float bobLateralFrequency = 24f;

    // =========================================================================
    // VR CONTROLLER HAPTICS / DO RUNG TAY CAM VR (AN TOAN & CAM GIAC THUC NHAT)
    // =========================================================================
    [Header("VR Controller Haptics / Do rung tay cam VR")]
    [Tooltip("Bat rung tay cam khi bay (0% gay say, cam giac vo ve rat ro net tren hai tay).")]
    public bool enableControllerHaptics = true;

    [Tooltip("Do rung nhe khi bay lo lung / bay cham (0-0.3).")]
    [Range(0f, 0.3f)]
    public float hoverHapticAmplitude = 0.05f;

    [Tooltip("Do rung khi bay het toc (0-0.8).")]
    [Range(0f, 0.8f)]
    public float maxSpeedHapticAmplitude = 0.22f;

    [Tooltip("Do rung them khi dang dap canh.")]
    [Range(0f, 0.5f)]
    public float flapHapticBoost = 0.16f;

    // =========================================================================
    // FIRST PERSON VISUALS / HINH ANH GOC NHIN THU NHAT
    // =========================================================================
    [Header("First Person Visuals / Hinh anh goc nhin thu nhat")]
    [Tooltip("An than ong (chi hien canh o ria man hinh) nhu anh mau.")]
    public bool hideBodyInFirstPerson = true;

    [Tooltip("Giu canh hien o goc nhin (tat di de khong bi 2 canh placeholder noi lung lo duoi thanh quet).")]
    public bool showWingsInFirstPerson = false;

    [Tooltip("Offset vi tri than/canh so voi camera (local).")]
    public Vector3 visualOffset = new Vector3(0f, -0.13f, 0.30f);

    [Tooltip("Offset goc cua than so voi camera (do).")]
    public Vector3 visualEulerOffset = new Vector3(6f, 0f, 0f);

    [Tooltip("Tong do lech giua visualRoot va BeeRoot (m). Offset rig de bu chenh lech.")]
    [Range(0f, 2f)]
    public float visualFollowLag = 0.05f;

    [Tooltip("Ti le cua visualRoot bay theo rig (0 = dung hinh, 1 = bam chuyen dong).")]
    [Range(0f, 1f)]
    public float visualFollowScale = 1f;

    [Tooltip("Cho phep visualRoot bam rig ngay lap (khong noi suy).")]
    public bool visualInstantFollow = true;

    [Tooltip("Toc do noi suy FOV khi doi che do. / FOV lerp speed.")]
    [Range(1f, 20f)]
    public float fovSmoothSpeed = 6f;

    [Tooltip("FOM them khi bay het toc (mo rong gon nhin) -> cam giac toc do.")]
    [Range(0f, 25f)]
    public float fovSpeedBoost = 6f;

    // =========================================================================
    // REFERENCES / THAM CHIEU
    // =========================================================================
    [Header("References / Tham chieu")]
    [Tooltip("XR Origin (player rig). De trong se tu tim. / The XR Origin root.")]
    public Transform playerRig;

    [Tooltip("Main Camera da duoc XR track. Script KHONG ghi de transform cua no.")]
    public Camera trackedCamera;

    [Tooltip("BeeController dung de doc yaw/pitch/bank. De trong se tu tim.")]
    public BeeController bee;

    [Tooltip("Than/canh ong hien thi o goc nhin thu nhat. De trong se tu tim.")]
    public Transform visualRoot;

    // =========================================================================
    // INTERNAL STATE / TRANG THAI NOI BO
    // =========================================================================
    private readonly List<Behaviour> _disabledLocomotion = new List<Behaviour>();
    private Vector3 _cameraLocalOffset;
    private bool _hasTrackedHeadOrigin;
    private Transform _bodyVisual;
    private Renderer[] _bodyRenderers;
    private Renderer[] _wingRenderers;
    private float _bobPhase;
    private float _bobLateralPhase;
    private Vector3 _bobVisualOffset;
    private float _bobVisualRoll;
    private float _bobFovOffset;
    private float _bobHeadOffset;
    private float _hapticTimer;
    private float _currentFov;
    private Vector3 _rigVelocity;
    private bool _resolved;

    private void Awake()
    {
        ResolveReferences();
        _currentFov = trackedCamera != null ? trackedCamera.fieldOfView : targetFov;
        if (unlockWideFov && targetFov < 105f)
        {
            targetFov = 105f;
        }
    }

    private void Start()
    {
        SnapToBee();
    }

    private void OnEnable()
    {
        if (playerRig != null && playerRig != transform)
        {
            DisableLocomotion();
        }
    }

    // =========================================================================
    // PUBLIC API / HAM CONG KHAI
    // =========================================================================

    /// <summary>Dat nguoi choi (XR Origin) ve vi tri con ong.</summary>
    public void SnapToBee()
    {
        ResolveReferences();
        if (trackedCamera == null || playerRig == null) return;
        if (playerRig == transform) return;

        CaptureHeadOrigin();

        Quaternion targetRotation = GetTargetRigRotation();
        playerRig.rotation = targetRotation;

        playerRig.position = GetEyeWorldPosition() - playerRig.TransformVector(_cameraLocalOffset);
    }

    /// <summary>Chuyen doi nhanh giua first-person va third-person.</summary>
    public void SetViewMode(ViewMode mode)
    {
        viewMode = mode;
        ApplyBodyVisibility();
    }


    // =========================================================================
    // ResolveReferences - Tu tim moi thu thieu trong scene de test nhanh.
    // =============================================================================
    private void ResolveReferences()
    {
        if (bee == null) bee = GetComponent<BeeController>();

        if (trackedCamera == null)
        {
            trackedCamera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        }

        if (playerRig == null && trackedCamera != null)
        {
            playerRig = FindPlayerRig(trackedCamera.transform);
        }

        if (visualRoot == null) visualRoot = transform.Find("BeeVisual");

        // Cache body/wings de dung lai moi frame (tranh Find() lap lai).
        _bodyVisual = visualRoot != null
            ? (visualRoot.Find("Body") != null ? visualRoot.Find("Body") : visualRoot)
            : null;
        if (_bodyVisual == null) _bodyVisual = transform.Find("Body");

        _bodyRenderers = _bodyVisual != null ? _bodyVisual.GetComponentsInChildren<Renderer>() : null;

        // CANH: chi lay renderer cua 2 pivot canh. Khong lay tat ca renderer
        // trong visualRoot, neu khong se bao gio cung bat lai than ong dang bi an.
        _wingRenderers = CollectWingRenderers();

        // Loai renderer cua canh ra khoi danh sach THAN. Canh nam duoi Body nen
        // GetComponentsInChildren o tren da gom luon canh -> neu khong loai, than
        // bi an se keo canh an theo.
        if (_bodyRenderers != null && _wingRenderers != null && _wingRenderers.Length > 0)
        {
            var keep = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer r in _bodyRenderers)
            {
                bool isWing = false;
                foreach (Renderer w in _wingRenderers)
                {
                    if (r == w) { isWing = true; break; }
                }
                if (!isWing) keep.Add(r);
            }
            _bodyRenderers = keep.ToArray();
        }

        _currentFov = trackedCamera != null ? trackedCamera.fieldOfView : targetFov;
        _resolved = trackedCamera != null && playerRig != null;

        ApplyBodyVisibility();
    }

    private bool IsHeadTracked()
    {
        InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        return head.isValid && head.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked;
    }

    private void CaptureHeadOrigin()
    {
        // Capture once, then preserve all physical head movement relative to this origin.
        _cameraLocalOffset = playerRig.InverseTransformPoint(trackedCamera.transform.position);
        _hasTrackedHeadOrigin = IsHeadTracked();
    }

    /// <summary>Gom renderer cua rieng hai pivot canh (Wing_L / Wing_R).</summary>
    private Renderer[] CollectWingRenderers()
    {
        if (visualRoot == null) return null;

        // Phai tim SAU moi cap: canh thuong nam duoi Body (BeeRoot/Body/Wing_L),
        // khong phai con truc tiep cua visualRoot.
        Transform wingL = FindDeep(visualRoot, "Wing_L");
        Transform wingR = FindDeep(visualRoot, "Wing_R");
        if (wingL == null && wingR == null) return null;

        var list = new System.Collections.Generic.List<Renderer>();
        if (wingL != null) list.AddRange(wingL.GetComponentsInChildren<Renderer>(true));
        if (wingR != null) list.AddRange(wingR.GetComponentsInChildren<Renderer>(true));
        return list.ToArray();
    }

    /// <summary>Tim child theo ten o moi cap (khong de quy vo han).</summary>
    private static Transform FindDeep(Transform root, string childName)
    {
        if (root == null) return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName) return child;

            Transform deep = FindDeep(child, childName);
            if (deep != null) return deep;
        }
        return null;
    }

    /// <summary>Leo len cha de tim XR Origin. / Walk up to find the XR Origin.</summary>
    private static Transform FindPlayerRig(Transform camera)
    {
        Transform current = camera;
        while (current.parent != null)
        {
            current = current.parent;
            if (current.name.Contains("XR Origin") || current.name.Contains("XROrigin"))
            {
                return current;
            }
        }
        return camera.root;
    }

    /// <summary>Bat/tat hien thi than ong (dung khi vao menu).</summary>
    public void SetBodyVisible(bool visible)
    {
        if (_bodyRenderers == null) return;
        foreach (Renderer r in _bodyRenderers) r.enabled = visible;
    }

    private void LateUpdate()
    {
        if (!_resolved) ResolveReferences();
        if (trackedCamera == null || playerRig == null) return;

        // Bob phải chạy TRUOC vi GetTargetRigRotation dung _bobRollOffset.
        UpdateBob();
        UpdateRigPose();
        UpdateFov();
        UpdateFirstPersonVisual();
        UpdateControllerHaptics();
    }

    // =========================================================================
    // UpdateRigPose - Dua XR Origin sao cho CAMERA (khong phai transform cua
    // camera) den dung diem "mat" cua con ong.
    //
    //   rig.position = eyeWorld - rig.TransformVector(initialHeadOffset)
    //
    // Only the initial tracked pose is compensated. Subsequent head translation
    // and rotation belong to tracking, including crouching and leaning.
    // =============================================================================
    private void UpdateRigPose()
    {
        // Neu script nam tren chinh XR Origin thi BeeController da lo het.
        if (playerRig == transform) return;

        // XR tracking can become available after Start (for example when putting on the headset).
        if (!_hasTrackedHeadOrigin && IsHeadTracked()) CaptureHeadOrigin();

        Quaternion targetRotation = GetTargetRigRotation();
        playerRig.rotation = targetRotation;

        Vector3 targetPosition = GetEyeWorldPosition() - playerRig.TransformVector(_cameraLocalOffset);

        if (smoothRigPosition)
        {
            float t = 1f - Mathf.Exp(-rigSmoothSpeed * Time.deltaTime);
            targetPosition = Vector3.Lerp(playerRig.position, targetPosition, t);
        }

        // _rigVelocity giu lai de tien cho hieu ung rut lien tuong lai
        _rigVelocity = (targetPosition - playerRig.position) / Mathf.Max(Time.deltaTime, 0.0001f);
        playerRig.position = targetPosition;
    }

    /// <summary>Goc quay cua XR Origin: chi ap yaw ngang cua ong de giu the gioi va tay cam thang dung, khong bi nghieng lech.</summary>
    private Quaternion GetTargetRigRotation()
    {
        float yaw = bee != null ? bee.Yaw : transform.eulerAngles.y;
        if (!applyYawToRig) yaw = transform.eulerAngles.y;

        // XR Origin luon giu thang dung theo truc Y de 2 tay cam va camera khong bi lech khoi trong luc thuc.
        // Hieu ung nghieng khi re (bank) va cui khi bay nhanh (pitch) duoc the hien qua visualRoot (than/canh ong).
        if (!applyPitchToRig && !applyBankToRig)
        {
            return Quaternion.Euler(0f, yaw, 0f);
        }

        float pitch = 0f;
        float bank = 0f;

        if (applyPitchToRig && bee != null)
        {
            pitch = -bee.Pitch * pitchInfluence;
            if (applySpeedPitchDown) pitch += bee.SpeedNormalized * speedPitchDown;
        }

        if (applyBankToRig && bee != null)
        {
            bank = bee.Bank * bankInfluence;
            // Bob roll KHONG ap len XR Origin nua (gay chong mat).
            // Chi ap len visualRoot trong UpdateFirstPersonVisual.
        }

        return Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, bank);
    }

    // =========================================================================
    // GetEyeWorldPosition - Diem "mat" cua con ong trong world space.
    //   Ap dung rung vi mo doc (up) 2-3mm cua camera de tai hien do rung con trung
    //   ma khong gay say xe (khong lam lech mat phang chan troi).
    // =============================================================================
    private Vector3 GetEyeWorldPosition()
    {
        Vector3 eye = transform.TransformPoint(eyeOffset);
        if (enableHeadBob && trackedCamera != null && (bee == null || (!bee.enableLeftTriggerHold && !bee.comfortDescent && !bee.IsPositionHolding)))
        {
            eye += trackedCamera.transform.up * _bobHeadOffset;
        }
        return eye;
    }

    // =========================================================================
    // UpdateBob - Tinh pha rung theo nhip canh.
    //   1. Micro-bob nhe tren camera: chi 2-3mm theo truc UP, khong bao gio quay nghieng.
    //   2. Visual-bob tren visualRoot (than/canh ong o ngoai vi).
    //   3. Micro-pulse tren FOV tao cam giac gio thoi theo nhip canh.
    // =============================================================================
    private void UpdateBob()
    {
        bool flapping = bee != null && bee.IsFlapping;
        float speedNorm = bee != null ? bee.SpeedNormalized : 0f;

        // Tần số chính khớp với BeeFlapping
        float frequency = bobFrequency * Mathf.Lerp(0.6f, 1f, speedNorm);
        float dt = Time.deltaTime;

        if (flapping || speedNorm > 0.04f)
        {
            _bobPhase += frequency * dt * Mathf.PI * 2f;
            if (_bobPhase > Mathf.PI * 2f) _bobPhase -= Mathf.PI * 2f;
        }
        else
        {
            // Khi dung yen: giam dan ve 0 thay vi dung ngot
            _bobPhase = Mathf.Lerp(_bobPhase, 0f, dt * 3f);
        }

        // Lateral buzz: tan so cao hon, mo phong rung canh con trung
        _bobLateralPhase += bobLateralFrequency * dt * Mathf.PI * 2f;
        if (_bobLateralPhase > Mathf.PI * 2f) _bobLateralPhase -= Mathf.PI * 2f;

        float intensity = flapping ? 1f : Mathf.Clamp01(speedNorm * 2f);
        float wave = Mathf.Sin(_bobPhase);
        float lateralWave = Mathf.Sin(_bobLateralPhase);

        // === 1. VR-SAFE HEAD MICRO-BOB (dao dong phuong dung sieu nho 2.5mm, 0% chong mat) ===
        _bobHeadOffset = wave * headBobAmplitude * intensity;

        // === 2. VISUAL ROOT OFFSET (chi hien o ria man hinh, than/canh ong) ===
        float verticalBob = wave * bobAmplitude * intensity;
        float lateralBob = lateralWave * bobLateralAmplitude * intensity;
        _bobVisualOffset = new Vector3(lateralBob, verticalBob, 0f);
        _bobVisualRoll = wave * bobRollAmplitude * intensity;

        // === 3. FOV MICRO-PULSE (rat nhe, cam nhan o ngoai vi tao cam giac gio) ===
        _bobFovOffset = wave * bobFovPulse * intensity;
    }

    // =========================================================================
    // UpdateControllerHaptics - Rung tay cam VR theo nhip canh va toc do bay.
    //   Tay cam rung la cach CHONG SAY XE tot nhat trong VR: nao nhan duoc
    //   phan hoi xuc giac chan thuc ma he thong tien dinh tai trong khong bi anh huong.
    // =============================================================================
    private void UpdateControllerHaptics()
    {
        if (!enableControllerHaptics) return;

        _hapticTimer -= Time.deltaTime;
        if (_hapticTimer > 0f) return;
        _hapticTimer = 0.05f; // ~20 Hz nhip rung phu hop voi OpenXR

        float speedNorm = bee != null ? bee.SpeedNormalized : 0f;
        bool flapping = bee != null && bee.IsFlapping;

        if (speedNorm < 0.02f && !flapping) return;

        float amp = Mathf.Lerp(hoverHapticAmplitude, maxSpeedHapticAmplitude, speedNorm);
        if (flapping) amp = Mathf.Min(1f, amp + flapHapticBoost);

        if (amp > 0.01f)
        {
            SendHaptic(XRNode.LeftHand, amp, 0.045f);
            SendHaptic(XRNode.RightHand, amp, 0.045f);
        }
    }

    /// <summary>Gui xung rung haptic toi tay cam VR.</summary>
    public static void SendHaptic(XRNode node, float amplitude, float duration)
    {
        InputDevice dev = InputDevices.GetDeviceAtXRNode(node);
        if (dev.isValid)
        {
            dev.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), duration);
        }
    }


    // =========================================================================
    // UpdateFov - FOV rong hon khi bay nhanh, tạo cảm giác tốc độ.
    // =============================================================================
    private void UpdateFov()
    {
        if (trackedCamera == null) return;

        // Khi mở rộng góc nhìn VR tự do:
        // Kính thực tế ảo (Quest 3) có trường nhìn vật lý ~110 độ do thấu kính quyết định qua OpenXR.
        // Nếu thiết bị VR đang hoạt động, không ép ghi đè camera.fieldOfView để góc nhìn được mở rộng tối đa tự nhiên 100%.
        bool isVrActive = XRSettings.isDeviceActive ||
            (XRSettings.enabled && !string.IsNullOrEmpty(XRSettings.loadedDeviceName) &&
             !XRSettings.loadedDeviceName.ToLowerInvariant().Contains("mock"));

        if (unlockWideFov && isVrActive)
        {
            return;
        }

        float speedNorm = bee != null ? bee.SpeedNormalized : 0f;
        float baseFov = unlockWideFov ? Mathf.Max(targetFov, 105f) : targetFov;
        float desired = baseFov + speedNorm * fovSpeedBoost + _bobFovOffset;

        _currentFov = Mathf.Lerp(_currentFov, desired,
            1f - Mathf.Exp(-fovSmoothSpeed * Time.deltaTime));

        if (!Mathf.Approximately(trackedCamera.fieldOfView, _currentFov))
        {
            trackedCamera.fieldOfView = _currentFov;
        }
    }

    // =========================================================================
    // UpdateFirstPersonVisual - Dam than/canh ong o ria man hinh nhu anh mau.
    //   visualRoot duoc dat truoc + thap hon camera, nen nguoi choi thay
    //   khoi hai canh dap o goc nhin ma van thay duoc phan chan trung tinh.
    // =============================================================================
    private void UpdateFirstPersonVisual()
    {
        if (visualRoot == null) return;

        if (viewMode == ViewMode.ThirdPersonDrone)
        {
            // Third person: visualRoot di chuyen nhu mot object binh thuong.
            visualRoot.SetPositionAndRotation(transform.position + Vector3.up * (bee != null ? bee.HoldVisualBob : 0f), transform.rotation);
            return;
        }

        // First person: Giữ thân và cánh ong gắn cố định trên thân con ong (BeeRoot)
        // để người chơi có thể tự do nhìn ngắm 360 độ thế giới xung quanh một cách thoáng đãng,
        // không bị thân/cánh ong dán chặt xoay theo mắt kính làm cản trở tầm nhìn!
        Vector3 targetPosition = transform.TransformPoint(visualOffset);
        targetPosition += transform.TransformDirection(_bobVisualOffset);
        targetPosition += Vector3.up * (bee != null ? bee.HoldVisualBob : 0f);
        visualRoot.position = targetPosition;

        Quaternion baseRot = transform.rotation * Quaternion.Euler(visualEulerOffset);
        visualRoot.rotation = baseRot * Quaternion.Euler(0f, 0f, _bobVisualRoll);
    }

    // =========================================================================
    // ApplyBodyVisibility - First person: an than, chi giu canh o ria man hinh.
    // =============================================================================
    private void ApplyBodyVisibility()
    {
        bool firstPerson = viewMode == ViewMode.FirstPersonBee;

        if (_bodyRenderers != null)
        {
            bool visible = !firstPerson || !hideBodyInFirstPerson;
            foreach (Renderer r in _bodyRenderers) r.enabled = visible;
        }

        if (_wingRenderers != null)
        {
            bool visible = !firstPerson || showWingsInFirstPerson;
            foreach (Renderer r in _wingRenderers) r.enabled = visible;
        }
    }

    // =========================================================================
    // DisableLocomotion - Tat locomotion cua XRI de khong keo nguoi choi
    // nguoc huong bay (ContinuousMove / SnapTurn / Teleportation), va tat
    // CharacterController vi dang bay khong can capsule.
    // =============================================================================
    private void DisableLocomotion()
    {
        if (playerRig == null) return;

        MonoBehaviour[] behaviours = playerRig.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this) continue;
            if (behaviour is BeeController || behaviour is BeeFlapping) continue;

            string typeName = behaviour.GetType().Name;
            // Chỉ tắt di chuyển và dịch chuyển (tránh kéo người chơi ngược hướng bay).
            // KHÔNG tắt TurnProvider để người chơi luôn có quyền xoay góc nhìn tự do bằng tay cầm.
            bool isLocomotion =
                typeName.Contains("MoveProvider") ||
                typeName.Contains("TeleportationProvider") ||
                typeName.Contains("NavigationProvider") ||
                typeName.Contains("CharacterControllerDriver");

            if (isLocomotion)
            {
                behaviour.enabled = false;
                _disabledLocomotion.Add(behaviour);
            }
        }

        // Dang bay: bo capsule de tranh vam vao vat the.
        CharacterController controller = playerRig.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
    }

    // =========================================================================
    // ApplyBeePovPreset - Dat lai thong so cho "goc nhin con ong nhu anh mau".
    //   Anh mau: FOV rong, mat nuoc ngay duoi tam nhin, ong luot sat mat nuoc,
    //   nghien nhe khi re, cam giac toc do ro nhung KHONG gay say.
    //
    //   Goi tu menu VU2/Bee/Apply Bee POV Preset, hoac tu BeeFlightSceneSetup.
    // =============================================================================
    public void ApplyBeePovPreset()
    {
        // --- Che do nhin: chinh nguoi choi la con ong ---
        viewMode = ViewMode.FirstPersonBee;

        // --- Khung nhin: mo rong toi da cho goc nhin toan canh thoang dang ---
        targetFov = 105f;
        unlockWideFov = true;
        fovSpeedBoost = 6f;
        fovSmoothSpeed = 6f;

        // --- Rig theo ong: uu tien chong say, chan troi luon phang va tu do ---
        applyYawToRig = true;
        applyPitchToRig = false;   // chi nhin bang dau -> an toan nhat trong VR
        pitchInfluence = 0.35f;
        applyBankToRig = false;    // Tat nghieng rig de goc nhin tu do 100% khong bi nghieng lac
        bankInfluence = 0.5f;
        applySpeedPitchDown = false;
        speedPitchDown = 0f;
        smoothRigPosition = false;

        // --- Rung vi mo an toan trong VR (Head Bob + Controller Haptics) ---
        enableHeadBob = true;
        headBobAmplitude = 0.0028f;
        enableControllerHaptics = true;
        hoverHapticAmplitude = 0.05f;
        maxSpeedHapticAmplitude = 0.22f;
        flapHapticBoost = 0.16f;

        bobAmplitude = 0.006f;
        bobFrequency = 14f;
        bobRollAmplitude = 0.6f;
        bobFovPulse = 0.45f;
        bobLateralAmplitude = 0.001f;
        bobLateralFrequency = 24f;

        // --- An than, giu 2 canh o day khung nhin nhu anh mau ---
        hideBodyInFirstPerson = true;
        showWingsInFirstPerson = false;
        visualOffset = new Vector3(0f, -0.12f, 0.34f);
        visualEulerOffset = new Vector3(8f, 0f, 0f);
        visualFollowLag = 0.05f;
        visualFollowScale = 1f;
        visualInstantFollow = true;

        // --- Mat ong: hoi cao va truoc mui mot chut ---
        eyeOffset = new Vector3(0f, 0.06f, 0.16f);
        eyeEulerOffset = Vector3.zero;

        // --- Chinh BeeController: re nhe hon, giu cam giac "em" ---
        if (bee == null) bee = GetComponent<BeeController>();
        if (bee != null)
        {
            bee.maxBankAngle = 18f;
            bee.speedTurnPenalty = 0.55f;
        }
    }

    // =========================================================================
    // OnDestroy - Tra lai locomotion neu bi tat nham.
    // =============================================================================
    private void OnDestroy()
    {
        foreach (Behaviour behaviour in _disabledLocomotion)
        {
            if (behaviour != null) behaviour.enabled = true;
        }
        _disabledLocomotion.Clear();
    }
}
