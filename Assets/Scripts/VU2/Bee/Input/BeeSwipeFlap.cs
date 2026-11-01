// =============================================================================
// BeeSwipeFlap - Quẹt ray qua thanh ngang cố định trong khung nhìn = vỗ cánh bay.
// Phiên bản nâng cấp:
//   1. Thanh vàng nối liền ngang (Single Continuous Bar) thay vì 2 thanh 2 bên.
//   2. Quẹt 2 chiều: từ DƯỚI LÊN hoặc TRÊN XUỐNG đều bay (Bidirectional Crossing).
//   3. Trợ lực bay (Flight Assist): Xung lực nâng tức thời + lướt êm (Glide buffer),
//      không cần quẹt điên cuồng, không mỏi tay, bay nhẹ nhàng tự nhiên.
//   4. Tay cầm thành tay ong (Bee Hand Controllers): Sọc vàng đen, đầy đủ nút bấm,
//      cần xoay rõ nét, kèm lông cổ tay và cánh nhỏ.
// =============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using ISDevice = UnityEngine.InputSystem.InputDevice;

[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(BeeController))]
public class BeeSwipeFlap : MonoBehaviour
{
    // =========================================================================
    // REFERENCES / THAM CHIẾU
    // =========================================================================
    [Header("References / Tham chiếu")]
    [Tooltip("Ong nhận override. Để trống sẽ tự tìm trên GameObject này.")]
    public BeeController bee;

    [Tooltip("Camera gắn khung nhìn (XR camera). Để trống sẽ tự tìm XROrigin.Camera.")]
    public Camera trackedCamera;

    [Tooltip("XR Origin để đổi pose tay cầm (device space → world). Để trống tự tìm.")]
    public XROrigin xrOrigin;

    [Header("Controller Anchors / Vị trí tay cầm trong scene")]
    [Tooltip("Transform tay TRÁI (Left Controller dưới XR Origin). Để trống tự tìm theo tên.")]
    public Transform leftControllerAnchor;

    [Tooltip("Transform tay PHẢI (Right Controller dưới XR Origin). Để trống tự tìm theo tên.")]
    public Transform rightControllerAnchor;

    [Header("Bee Hand Visual / Da tay ong")]
    [Tooltip("Tự động áp kiểu dáng tay ong lên tay cầm VR và đảm bảo hiển thị 100% trong VR.")]
    public bool autoSkinControllers = true;

    [Tooltip("Hiện quả cầu proxy tay thô (tắt để không che khuất tay cầm).")]
    public bool showHandProxies = false;

    [Tooltip("Kích thước tay proxy nếu bật (m).")]
    public Vector3 handProxyScale = new Vector3(0.05f, 0.035f, 0.075f);

    [Header("Bee Hand Visual Materials / Vật liệu tay cầm & tia")]
    [Tooltip("Vật liệu thân tay cầm sọc vàng đen (M_BeeHand_Body).")]
    public Material beeHandBodyMaterial;

    [Tooltip("Vật liệu nút bấm & cò (M_BeeHand_Button).")]
    public Material beeHandButtonMaterial;

    [Tooltip("Vật liệu cần xoay joystick (M_BeeHand_Accent).")]
    public Material beeHandAccentMaterial;

    [Tooltip("Vật liệu laser vàng kim neon (M_GoldenLaser).")]
    public Material goldenLaserMaterial;

    [Tooltip("Vật liệu vòng tròn mục tiêu (M_GoldenRing_Reticle).")]
    public Material goldenReticleMaterial;

    // =========================================================================
    // CONTROLLER & RAY MANUAL TUNING / TINH CHỈNH TAY CẦM & TIA BẰNG TAY
    // =========================================================================
    [Header("Controller & Ray Tuning / Tinh chỉnh tay cầm & tia ray")]
    [Tooltip("Bù độ cao tia ray (m). Số ÂM để hạ tia xuống thấp; số DƯƠNG để nâng cao lên.")]
    [Range(-2f, 2f)]
    public float controllerHeightOffset = 0f;

    [Tooltip("Bù khoảng cách trước/sau tia ray (m). Số DƯƠNG = đẩy tia ra xa; số ÂM = kéo gần lại.")]
    [Range(-1f, 1f)]
    public float controllerForwardOffset = 0f;

    [Tooltip("Bù khoảng cách ngang sang 2 bên (m). Tăng để 2 tia mở rộng sang 2 bên sườn; giảm để thu vào giữa.")]
    [Range(-0.5f, 0.5f)]
    public float controllerSideOffset = 0f;

    [Tooltip("Bù góc nghiêng của tia raycast (độ). Dương = chúc tia xuống; Âm = ngửa tia lên trên.")]
    [Range(-90f, 90f)]
    public float rayPitchOffset = 0f;

    [Tooltip("Tự động khắc phục lỗi OpenXR khi đeo kính VR khiến tay cầm bị lệch lên trần nhà (Ceiling Controllers bug). Luôn BẬT để đảm bảo tay cầm ở vị trí tự nhiên.")]
    public bool autoFixCeilingControllers = true;

    [Header("VR Ergonomic Hands / Công thái học tay cầm VR")]
    [Tooltip("Tự động nâng nhẹ vị trí hiển thị và ngắm của tay cầm khi tay thật buông lỏng ở hông/đùi, giúp tay luôn xuất hiện tự nhiên trong tầm nhìn ngoại vi dưới của kính VR.")]
    public bool enableErgonomicHands = true;

    [Tooltip("Độ nâng công thái học tối đa theo trục dọc của mắt (m) khi tay ở vị trí thấp.")]
    [Range(0f, 0.4f)]
    public float ergonomicLift = 0.22f;

    [Tooltip("Độ đẩy nhẹ ra phía trước theo trục nhìn của mắt (m) để tay không bị sát ngực.")]
    [Range(0f, 0.3f)]
    public float ergonomicForward = 0.08f;

    [Tooltip("Khoảng cách Y tối thiểu so với mắt người chơi (m). Nếu tay thấp hơn mức này sẽ được bù công thái học vào tầm nhìn ngoại vi.")]
    public float minComfortHandY = -0.28f;

    // =========================================================================
    // BAR PLACEMENT / VỊ TRÍ THANH
    // =========================================================================
    [Header("Bar Placement / Vị trí thanh")]
    [Tooltip("Sử dụng 1 thanh ngang nối liền duy nhất ở giữa thay vì 2 thanh 2 bên.")]
    public bool useSingleBar = true;

    [Tooltip("Khoảng cách thanh trước mắt (m). 0.5–0.7 dễ nhìn nhất.")]
    public float barDepth = 0.55f;

    [Tooltip("Độ rộng thanh (m). Khi dùng thanh nối liền, nên để 0.45 - 0.60m.")]
    public float barWidth = 0.52f;

    [Tooltip("Độ dày thanh (m).")]
    public float barThickness = 0.012f;

    [Tooltip("Visual thickness only; contact tolerance is unchanged.")]
    [Range(0.2f, 1f)] public float barVisualThicknessScale = 0.55f;

    [Tooltip("Vị trí tâm thanh nối liền (local camera, m). X = 0 (giữa), Y âm = dưới tầm mắt.")]
    public Vector2 barCenterOffset = new Vector2(0f, -0.21f);

    [Tooltip("Vị trí thanh TRÁI nếu dùng chế độ 2 thanh (local camera, m).")]
    public Vector2 leftBarOffset = new Vector2(-0.16f, -0.21f);

    [Tooltip("Vị trí thanh PHẢI nếu dùng chế độ 2 thanh (local camera, m).")]
    public Vector2 rightBarOffset = new Vector2(0.16f, -0.21f);

    // =========================================================================
    // DETECTION & BIDIRECTIONAL SWIPE / PHÁT HIỆN QUÉT 2 CHIỀU
    // =========================================================================
    [Header("Crossing Detection / Phát hiện quẹt")]
    [Tooltip("Cho phép quẹt cả 2 chiều: từ DƯỚI LÊN hoặc từ TRÊN XUỐNG đều tính nhịp bay.")]
    public bool bidirectionalCrossing = true;

    [Tooltip("Vùng chống rung (m): phải đi qua ±deadband mới tính 1 nhịp. Tránh rung mép thanh.")]
    public float crossDeadband = 0.025f;

    [Header("Flight V2 / Nhan nhip hai tay")]
    [Tooltip("Two accepted strokes this close in time count as one paired beat.")]
    [Range(0.05f, 0.4f)] public float pairedStrokeWindow = 0.18f;
    [Tooltip("Minimum interval per hand in Flight V2; reduces rewards for wrist jitter.")]
    [Range(0.12f, 0.6f)] public float minimumRhythmInterval = 0.28f;
    public int PairedFlapCount { get; private set; }
    private readonly BeeStrokePairWindow _strokePairing = new BeeStrokePairWindow();
    private bool RhythmFlight => bee != null && bee.enableRhythmFlight;
    private float HandCooldown => RhythmFlight ? Mathf.Max(cooldownPerHand, minimumRhythmInterval) : cooldownPerHand;


    [Tooltip("Extra lift when both independent rays touch the bar together.")]
    [Min(0f)] public float dualHandBonusImpulse = 0.85f;

    [Tooltip("Thời gian chờ giữa 2 nhịp của CÙNG một tay (giây).")]
    [Range(0.05f, 0.5f)] public float cooldownPerHand = 0.12f;

    [Tooltip("Thời gian duy trì vỗ cánh sau mỗi nhịp (giây). Nhỏ gọn (0.15s) để cảm giác vỗ thật tay, không bị bốc lên quá đà.")]
    [Range(0.08f, 0.5f)] public float flapHoldSeconds = 0.16f;

    [Tooltip("Nới rộng cửa sổ quẹt ngang: cho phép quẹt trúng quanh thanh.")]
    [Range(0.5f, 3f)] public float horizontalGate = 1.3f;

    [Header("Hands / Tay")]
    [Tooltip("Bật quẹt bằng tay trái.")]
    public bool enableLeftHand = true;

    [Tooltip("Bật quẹt bằng tay phải.")]
    public bool enableRightHand = true;

    // =========================================================================
    // FLIGHT ASSIST / TRỢ LỰC BAY
    // =========================================================================
    [Header("Flight Assist / Trợ lực bay (Chống mỏi tay, cân bằng vừa vặn)")]
    [Tooltip("Bật trợ lực: vỗ cánh vừa sức, giữ được cảm giác trọng lực và độ cao thật của ong.")]
    public bool enableFlightAssist = true;

    [Tooltip("Xung lực nâng mỗi nhịp quẹt (m/s). Nâng nhẹ ~0.35m vừa vặn tự nhiên, không bị phóng vọt lên.")]
    [Range(0.2f, 2.5f)] public float assistLiftImpulse = 0.85f;

    [Tooltip("Thời gian đệm chuyển tiếp ngắn sau nhịp vỗ (giây).")]
    [Range(0f, 1f)] public float glideFloatDuration = 0.15f;

    [Tooltip("Hệ số trọng lực (0.8 = rơi tự nhiên nhẹ nhàng, giữ trọn cảm giác bay thật).")]
    [Range(0.5f, 1f)] public float glideGravityScale = 0.82f;

    // =========================================================================
    // HAPTICS / ĐỘ RUNG TAY CẦM KHI QUẸT
    // =========================================================================
    [Header("Controller Haptics / Rung tay cầm")]
    [Tooltip("Bật rung tay cầm mỗi khi quẹt qua thanh.")]
    public bool enableHaptics = true;

    [Tooltip("Cường độ xung rung khi quẹt (0-1).")]
    [Range(0.1f, 1f)] public float swipeHapticStrength = 0.65f;

    [Tooltip("Thời gian xung rung (giây).")]
    [Range(0.02f, 0.2f)] public float swipeHapticDuration = 0.08f;
    // VISUAL / HIỂN THỊ
    // =========================================================================
    [Header("Visual / Hiển thị")]
    [Tooltip("Hiện thanh ngang trong khung nhìn.")]
    public bool showBars = true;

    [Tooltip("Vẽ ray từ tay cầm tới điểm giao thanh (tia vàng kim neon sáng rực ).")]
    public bool showRays = true;

    [Tooltip("Tự mờ thanh xuống khi không có tay cầm XR hoạt động (test trên PC).")]
    public bool fadeBarsWhenIdle = false;

    [Tooltip("Màu thanh khi ở trạng thái chờ.")]
    public Color barColor = new Color(1f, 0.76f, 0.12f, 0.75f);

    [Tooltip("Màu thanh khi quẹt trúng (flash nhịp).")]
    public Color flashColor = new Color(1f, 0.98f, 0.65f, 1f);

    [Tooltip("Màu ray vàng kim.")]
    public Color rayColor = new Color(1.0f, 0.82f, 0.18f, 0.95f);

    [Header("Debug")]
    [Tooltip("Log mỗi nhịp quẹt ra Console.")]
    public bool debugLog = false;

    // =========================================================================
    // PUBLIC STATE / TRẠNG THÁI CÔNG KHAI
    // =========================================================================
    /// <summary>Đang có hiệu lực vỗ cánh từ quẹt.</summary>
    public bool IsSwipeFlapping => _flapHoldTimer > 0f;

    /// <summary>Số nhịp quẹt đã tính (cả 2 tay).</summary>
    public int FlapCount { get; private set; }

    // =========================================================================
    // INTERNAL / NỘI BỘ
    // =========================================================================
    private class HandState
    {
        // -1: đang ở dưới thanh, +1: đang ở trên thanh, 0: ở giữa deadband / ngoài tầm
        public int side;
        public bool touching;
        public float cooldown;
        public float flash;
        public bool hasDevice;
        public Vector2 barOffset;
        public Transform anchor;
        public LineRenderer rayLine;
        public Image barImage;
        public GameObject proxy;
        public Material proxyMaterial;
        public GameObject reticle;
        public bool skinned;
        public Transform modelTransform;
        public Vector3 originalModelLocalPos;
        public Quaternion originalModelLocalRot;
        public bool cachedModel;
        public Vector3 currentErgonomicShift;
    }

    private HandState _left = new HandState();
    private HandState _right = new HandState();

    private float _flapHoldTimer;
    private float _glideTimer;
    private float _mainBarFlashTimer;
    private bool _bothHandsTouching;
    private bool _built;

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private Image _mainBarImage;
    private Material _rayMaterial;
    private bool _ownsRayMaterial;
    private bool _rayMaterialFailed;

    private static readonly List<XRInputDevice> _deviceBuffer = new List<XRInputDevice>();

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        if (bee == null) bee = GetComponent<BeeController>();

        // Tự động hạ thanh xuống -0.21f nếu đang lưu giá trị cũ -0.14f để giải phóng 100% tầm nhìn chính diện
        if (Mathf.Approximately(barCenterOffset.y, -0.14f))
            barCenterOffset.y = -0.21f;
        if (Mathf.Approximately(leftBarOffset.y, -0.14f))
            leftBarOffset.y = -0.21f;
        if (Mathf.Approximately(rightBarOffset.y, -0.14f))
            rightBarOffset.y = -0.21f;
    }

    private void OnDisable()
    {
        _bothHandsTouching = false;
        _strokePairing.Clear();
        if (bee != null) bee.ClearStrokeLift();
        _left.touching = _right.touching = false;
        _flapHoldTimer = 0f;
        _glideTimer = 0f;
        if (bee != null)
        {
            bee.ExternalFlapOverride = false;
            bee.ExternalGravityScale = 1.0f;
        }

        if (_canvas != null) _canvas.gameObject.SetActive(false);
        if (_left.rayLine != null) _left.rayLine.enabled = false;
        if (_right.rayLine != null) _right.rayLine.enabled = false;
        if (_left.reticle != null) _left.reticle.SetActive(false);
        if (_right.reticle != null) _right.reticle.SetActive(false);
        if (_left.proxy != null) _left.proxy.SetActive(false);
        if (_right.proxy != null) _right.proxy.SetActive(false);
    }

    private void OnEnable()
    {
        if (_canvas != null) _canvas.gameObject.SetActive(showBars);
    }

    private void OnDestroy()
    {
        SafeDestroy(_canvas != null ? _canvas.gameObject : null);
        SafeDestroy(_left.rayLine != null ? _left.rayLine.gameObject : null);
        SafeDestroy(_right.rayLine != null ? _right.rayLine.gameObject : null);
        SafeDestroy(_left.reticle);
        SafeDestroy(_right.reticle);
        SafeDestroy(_left.proxy);
        SafeDestroy(_right.proxy);
        SafeDestroy(_left.proxyMaterial);
        SafeDestroy(_right.proxyMaterial);
        if (_ownsRayMaterial)
        {
            SafeDestroy(_rayMaterial);
        }
        _rayMaterial = null;
    }

    private static void SafeDestroy(Object obj)
    {
        if (obj == null) return;
#if UNITY_EDITOR
        if (UnityEditor.EditorUtility.IsPersistent(obj)) return;
        if (UnityEditor.AssetDatabase.Contains(obj)) return;
#endif
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    // =========================================================================
    // UPDATE
    // =========================================================================
    private void Update()
    {
        if (!_built)
        {
            TryBuild();
            if (!_built) return;
        }

        // Hết hạn giữ vỗ cánh → ong bắt đầu lướt hoặc rơi
        if (_flapHoldTimer > 0f) _flapHoldTimer -= Time.deltaTime;
        if (bee != null)
        {
            bee.ExternalFlapOverride = !RhythmFlight && _flapHoldTimer > 0f;
            if (bee.IsPositionHolding || bee.ExternalMovementLocked)
            {
                _flapHoldTimer = _glideTimer = 0f;
                _strokePairing.Clear();
            }
        }

        // Trợ lực bay: đệm lướt êm, giảm trọng lực sau nhịp vỗ
        if (enableFlightAssist && bee != null && !RhythmFlight)
        {
            if (_glideTimer > 0f)
            {
                _glideTimer -= Time.deltaTime;
                bee.ExternalGravityScale = glideGravityScale;
                bee.SoftUpwardDamping = true;
            }
            else
            {
                bee.ExternalGravityScale = 1.0f;
            }
        }

        if (_mainBarFlashTimer > 0f) _mainBarFlashTimer -= Time.deltaTime;

        if (_left.anchor == null || _right.anchor == null) AssignAnchors();

        // Tự động skin tay cầm thành tay ong nếu cần
        if (autoSkinControllers)
        {
            if (!_left.skinned && _left.anchor != null) ApplyBeeControllerSkin(_left.anchor, true, _left);
            if (!_right.skinned && _right.anchor != null) ApplyBeeControllerSkin(_right.anchor, false, _right);
        }
    }

    /// <summary>
    /// Đọc pose tay cầm, giao ray với mặt phẳng thanh ngang,
    /// phát hiện quẹt 2 chiều (từ dưới lên HOẶC từ trên xuống) và kích hoạt vỗ cánh có trợ lực.
    /// </summary>
    private void UpdateHand(HandState hand, bool isLeft)
    {
        bool wasTouching = hand.touching;
        hand.touching = false;
        bool enabledHand = isLeft ? enableLeftHand : enableRightHand;
        hand.barOffset = useSingleBar ? barCenterOffset : (isLeft ? leftBarOffset : rightBarOffset);

        if (hand.cooldown > 0f) hand.cooldown -= Time.deltaTime;
        if (hand.flash > 0f) hand.flash -= Time.deltaTime;

        if (trackedCamera == null)
        {
            trackedCamera = Camera.main;
            if (trackedCamera == null) trackedCamera = FindObjectOfType<Camera>();
        }

        if (!enabledHand || trackedCamera == null)
        {
            hand.hasDevice = false;
            hand.side = 0;
            if (hand.rayLine != null) hand.rayLine.enabled = false;
            if (hand.proxy != null) hand.proxy.SetActive(false);
            return;
        }

        // --- 1. Pose tay cầm ---
        XRInputDevice xrDev = GetDevice(isLeft ? XRNode.LeftHand : XRNode.RightHand);
        ISDevice isDev = GetInputSystemDevice(isLeft);
        bool deviceFound = xrDev.isValid || isDev != null;

        Transform anchor = hand.anchor;
        if (anchor != null && anchor.gameObject.activeInHierarchy)
        {
            hand.hasDevice = true;
        }
        else
        {
            hand.hasDevice = deviceFound;
        }

        if (!hand.hasDevice)
        {
            hand.side = 0;
            if (hand.rayLine != null) hand.rayLine.enabled = false;
            if (hand.proxy != null) hand.proxy.SetActive(false);
            return;
        }

        bool gotPose = false;
        Vector3 worldPos = Vector3.zero;
        Quaternion worldRot = Quaternion.identity;

        if (anchor != null && anchor.gameObject.activeInHierarchy)
        {
            // Tắt các LineRenderer / XRInteractorLineVisual trùng lặp trên tay cầm gốc
            DisableConflictingLineVisuals(anchor);

            Transform rayTip = anchor.Find("RayTip");
            Transform model = anchor.Find("Model");
            if (model == null)
            {
                // Tìm model prefab do ActionBasedController instantiate (XR Controller Left/Right(Clone))
                for (int i = 0; i < anchor.childCount; i++)
                {
                    Transform child = anchor.GetChild(i);
                    if (child.name != "RayTip" && child.GetComponentInChildren<Renderer>(true) != null)
                    {
                        model = child;
                        break;
                    }
                }
            }

            // Cache Model gốc nếu chưa lưu
            if (model != null && !hand.cachedModel)
            {
                hand.modelTransform = model;
                hand.originalModelLocalPos = model.localPosition;
                hand.originalModelLocalRot = model.localRotation;
                hand.cachedModel = true;
            }

            Vector3 camLocalAnchor = trackedCamera != null
                ? trackedCamera.transform.InverseTransformPoint(anchor.position)
                : anchor.localPosition;

            bool isCeilingDisplaced = autoFixCeilingControllers && camLocalAnchor.y > 0.20f;

            // 1. TRIỆT TIÊU LỖI LỆCH SÀN OPENXR (Ceiling Controllers Fix):
            // Chỉ can thiệp khi OpenXR bị lệch sàn đẩy tay cầm lên trần nhà (cao hơn đầu > 0.20m).
            if (isCeilingDisplaced)
            {
                camLocalAnchor.y -= 1.18f;
                if (camLocalAnchor.y > 0.02f) camLocalAnchor.y = -0.18f;
                if (camLocalAnchor.z < 0.25f) camLocalAnchor.z = 0.28f;

                if (!Mathf.Approximately(controllerHeightOffset, 0f)) camLocalAnchor.y += controllerHeightOffset;
                if (!Mathf.Approximately(controllerForwardOffset, 0f)) camLocalAnchor.z += controllerForwardOffset;
                if (!Mathf.Approximately(controllerSideOffset, 0f)) camLocalAnchor.x += (isLeft ? -controllerSideOffset : controllerSideOffset);

                if (trackedCamera != null)
                {
                    anchor.position = trackedCamera.transform.TransformPoint(camLocalAnchor);
                }
            }

            // 2. Đảm bảo Model luôn hiển thị và render chuẩn xác:
            if (model != null)
            {
                if (!model.gameObject.activeSelf) model.gameObject.SetActive(true);
                var rends = model.GetComponentsInChildren<Renderer>(true);
                for (int ri = 0; ri < rends.Length; ri++)
                {
                    if (rends[ri] != null && !rends[ri].enabled) rends[ri].enabled = true;
                }
            }

            // Điểm xuất phát của ray: mũi tay cầm (phần đầu phát laser trên controller)
            if (rayTip != null)
            {
                worldPos = rayTip.position;
                worldRot = rayTip.rotation;
            }
            else
            {
                worldPos = anchor.TransformPoint(new Vector3(0f, 0.01f, 0.04f));
                worldRot = anchor.rotation;
            }

            if (!Mathf.Approximately(rayPitchOffset, 0f))
            {
                worldRot = worldRot * Quaternion.Euler(rayPitchOffset, 0f, 0f);
            }

            gotPose = true;
        }
        if (!gotPose && xrDev.isValid
            && xrDev.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3 legacyPos)
            && xrDev.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion legacyRot))
        {
            worldPos = TrackingPoint(legacyPos);
            worldRot = TrackingRot(legacyRot);
            gotPose = true;
        }
        if (!gotPose && isDev != null)
        {
            var posCtrl = isDev.TryGetChildControl<InputControl<Vector3>>("devicePosition");
            var rotCtrl = isDev.TryGetChildControl<InputControl<Quaternion>>("deviceRotation");
            if (posCtrl != null && rotCtrl != null)
            {
                worldPos = TrackingPoint(posCtrl.ReadValue());
                worldRot = TrackingRot(rotCtrl.ReadValue());
                gotPose = true;
            }
        }
        if (!gotPose)
        {
            hand.side = 0;
            if (hand.rayLine != null) hand.rayLine.enabled = false;
            if (hand.reticle != null) hand.reticle.SetActive(false);
            if (hand.proxy != null) hand.proxy.SetActive(false);
            return;
        }

        // Nếu tư thế lấy từ fallback device (không qua anchor), áp dụng bù khoảng cách vào worldPos
        if (anchor == null || !anchor.gameObject.activeInHierarchy)
        {
            if (trackedCamera != null)
            {
                Vector3 camLocal = trackedCamera.transform.InverseTransformPoint(worldPos);
                if (autoFixCeilingControllers && camLocal.y > 0.05f)
                {
                    camLocal.y -= 1.18f;
                    if (camLocal.y > 0.02f) camLocal.y = -0.18f;
                }
                if (camLocal.z < 0.25f) camLocal.z = 0.28f;
                if (enableErgonomicHands && camLocal.y < minComfortHandY)
                {
                    float deficit = minComfortHandY - camLocal.y;
                    float lift = Mathf.Min(deficit, ergonomicLift);
                    camLocal.y += lift;
                    camLocal.z += ergonomicForward;
                }
                if (!Mathf.Approximately(controllerHeightOffset, 0f)) camLocal.y += controllerHeightOffset;
                if (!Mathf.Approximately(controllerForwardOffset, 0f)) camLocal.z += controllerForwardOffset;
                if (!Mathf.Approximately(controllerSideOffset, 0f)) camLocal.x += (isLeft ? -controllerSideOffset : controllerSideOffset);

                worldPos = trackedCamera.transform.TransformPoint(camLocal);

                if (!Mathf.Approximately(rayPitchOffset, 0f))
                {
                    worldRot = worldRot * Quaternion.Euler(rayPitchOffset, 0f, 0f);
                }
            }
        }

        // Hướng bắn của tia: chuẩn 100% theo hướng chỉ của mũi tay cầm
        UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor menuRay = null;
        if (LetItBee.UI.BeeMenuInput.Blocked && anchor != null) {
            menuRay = anchor.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
            if (menuRay != null) {
                Transform origin = menuRay.rayOriginTransform != null ? menuRay.rayOriginTransform : menuRay.transform;
                worldPos = origin.position; worldRot = origin.rotation;
            }
        }
        Vector3 forward = worldRot * Vector3.forward;

        // Điểm xuất phát của ray proxy nếu bật
        if (hand.proxy != null && showHandProxies)
        {
            hand.proxy.SetActive(true);
            hand.proxy.transform.SetPositionAndRotation(worldPos + forward * 0.03f, worldRot);
        }

        // --- 2. Tính điểm đích của tia ray trong không gian 3D (như ảnh mẫu) ---
        float maxRayDistance = 15f;
        float defaultRayDistance = 7f;
        Vector3 endPoint = worldPos + forward * defaultRayDistance;

        int raycastMask = ~LayerMask.GetMask("UI", "Ignore Raycast");
        if (Physics.Raycast(worldPos, forward, out RaycastHit hitInfo, maxRayDistance, raycastMask, QueryTriggerInteraction.Ignore))
        {
            // Bỏ qua nếu trúng chính thân ong hoặc player rig
            bool hitSelf = false;
            if (bee != null && (hitInfo.transform.root == bee.transform.root || hitInfo.transform.IsChildOf(bee.transform))) hitSelf = true;
            if (xrOrigin != null && (hitInfo.transform.root == xrOrigin.transform.root || hitInfo.transform.IsChildOf(xrOrigin.transform))) hitSelf = true;

            if (!hitSelf)
            {
                endPoint = hitInfo.point;
            }
        }

        // --- 3. Giao ray với mặt phẳng của thanh ngang để tính vị trí quẹt qua thanh ---
        if (menuRay != null && menuRay.TryGetCurrentUIRaycastResult(out UnityEngine.EventSystems.RaycastResult menuHit))
            endPoint = menuHit.worldPosition;

        Vector3 planePoint = _canvas != null ? _canvas.transform.position : (transform.position + transform.forward * barDepth);
        Vector3 planeNormal = _canvas != null ? -_canvas.transform.forward : -transform.forward;
        Plane plane = new Plane(planeNormal, planePoint);
        Ray aimRay = new Ray(worldPos, forward);

        Vector3 barHitPoint;
        bool validPlaneHit = plane.Raycast(aimRay, out float enter) && enter > 0f && enter < maxRayDistance;
        if (validPlaneHit)
        {
            barHitPoint = aimRay.GetPoint(enter);
        }
        else
        {
            barHitPoint = worldPos + forward * barDepth;
        }

        Vector3 local = _canvas != null ? _canvas.transform.InverseTransformPoint(barHitPoint) : transform.InverseTransformPoint(barHitPoint);
        // InverseTransformPoint already removes the complete XR hierarchy scale.
        // Convert canvas pixels to authored metres exactly once.
        if (_canvas != null) local *= 0.001f;
        var embodiment = GetComponent<BeeEmbodiment>();
        float targetY = useSingleBar ? 0f : hand.barOffset.y;
        float targetX = useSingleBar ? 0f : hand.barOffset.x;

        float dx = local.x - targetX;
        float dy = local.y - targetY;

        float effectiveHalfWidth = (barWidth * 0.5f) * horizontalGate;
        bool withinGate = validPlaneHit && showBars && Mathf.Abs(dx) <= effectiveHalfWidth;
        hand.touching = withinGate && Mathf.Abs(dx) <= barWidth * 0.5f
            && Mathf.Abs(dy) <= Mathf.Max(barThickness * 0.5f, 0.01f);
        if (hand.touching)
        {
            // Each hand owns its contact edge, cooldown, reticle and haptic pulse.
            endPoint = barHitPoint;
            hand.side = 0;
            if (!wasTouching && hand.cooldown <= 0f)
            {
                hand.cooldown = HandCooldown;
                PulseFlap(isLeft, isUpstroke: true);
            }
        }
        else if (withinGate)
        {
            // PHÁT HIỆN QUÉT 2 CHIỀU:
            // 1) Quẹt TỪ DƯỚI LÊN (Upstroke)
            if (hand.side == -1 && dy > crossDeadband)
            {
                if (hand.cooldown <= 0f)
                {
                    hand.side = 1;
                    hand.cooldown = HandCooldown;
                    PulseFlap(isLeft, isUpstroke: true);
                }
            }
            // 2) Quẹt TỪ TRÊN XUỐNG (Downstroke)
            else if (bidirectionalCrossing && hand.side == 1 && dy < -crossDeadband)
            {
                if (hand.cooldown <= 0f)
                {
                    hand.side = -1;
                    hand.cooldown = HandCooldown;
                    PulseFlap(isLeft, isUpstroke: false);
                }
            }
            // 3) Khởi tạo side ban đầu khi ray mới đi vào vùng thanh
            else if (hand.side == 0)
            {
                if (dy > crossDeadband) hand.side = 1;
                else if (dy < -crossDeadband) hand.side = -1;
            }
        }
        else
        {
            // Ra ngoài phạm vi thanh ngang
            hand.side = 0;
        }

        // --- 4. Vẽ tia ray vàng kim neon + Vòng tròn mục tiêu (Reticle) ---
        if (hand.rayLine != null)
        {
            if (_rayMaterial == null && !_rayMaterialFailed)
            {
                _rayMaterial = CreateRayMaterial();
            }

            bool drawRay = showRays && _rayMaterial != null;
            hand.rayLine.enabled = drawRay;
            if (drawRay)
            {
                hand.rayLine.SetPosition(0, worldPos);
                hand.rayLine.SetPosition(1, endPoint);
            }
            if (hand.reticle != null)
            {
                hand.reticle.SetActive(drawRay);
                if (drawRay && trackedCamera != null)
                {
                    hand.reticle.transform.position = endPoint;
                    hand.reticle.transform.rotation = Quaternion.LookRotation(trackedCamera.transform.forward);

                    // Tự động scale kích thước vòng vàng theo khoảng cách để luôn rõ nét
                    float dist = Vector3.Distance(trackedCamera.transform.position, endPoint);
                    float playerScale = embodiment != null ? embodiment.Scale : 1f;
                    float reticleScale = Mathf.Clamp(dist / playerScale * 0.035f, 0.07f, 0.28f) * playerScale;
                    hand.reticle.transform.localScale = new Vector3(reticleScale, reticleScale, reticleScale);
                }
            }
        }
    }

    private void PulseFlap(bool isLeft, bool isUpstroke)
    {
        if (LetItBee.UI.BeeMenuInput.Blocked) return;
        if (bee != null && (bee.IsPositionHolding || bee.ExternalMovementLocked)) return;
        bool paired = false;
        if (RhythmFlight)
        {
            if (!enableFlightAssist || !bee.TryQueueRhythmStroke(assistLiftImpulse)) return;
            paired = _strokePairing.Register(isLeft, Time.time, pairedStrokeWindow);
            if (paired)
            {
                bee.QueueRhythmPairBonus(dualHandBonusImpulse);
                PairedFlapCount++;
            }
        }
        else if (enableFlightAssist && bee != null)
        {
            bee.AddVerticalImpulse(assistLiftImpulse);
        }
        _flapHoldTimer = flapHoldSeconds;
        _glideTimer = glideFloatDuration;

        HandState hand = isLeft ? _left : _right;
        hand.flash = 0.16f;
        _mainBarFlashTimer = paired ? 0.28f : 0.16f;
        if (paired) _left.flash = _right.flash = 0.28f;
        FlapCount++;

        // HAPTICS: Rung dứt khoát trên tay vừa quẹt qua thanh
        if (enableHaptics)
        {
            TriggerSwipeHaptic(isLeft, swipeHapticStrength, swipeHapticDuration);
        }

        if (debugLog)
        {
            string dir = isUpstroke ? "DƯỚI LÊN ⬆️" : "TRÊN XUỐNG ⬇️";
            Debug.Log($"[BeeSwipeFlap] Nhịp quẹt tay {(isLeft ? "trái" : "phải")} ({dir}) " +
                      $"#{FlapCount} (giữ {flapHoldSeconds:F2}s, xung lực {assistLiftImpulse:F1}m/s)");
        }
    }

    private void TriggerSwipeHaptic(bool isLeft, float amplitude, float duration)
    {
        XRNode primaryNode = isLeft ? XRNode.LeftHand : XRNode.RightHand;
        XRInputDevice dev = GetDevice(primaryNode);
        if (dev.isValid)
        {
            dev.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), duration);
        }

        // Rung nhẹ đối xứng sang tay kia để cảm giác hai cánh cùng vỗ
        XRNode secondaryNode = isLeft ? XRNode.RightHand : XRNode.LeftHand;
        XRInputDevice dev2 = GetDevice(secondaryNode);
        if (dev2.isValid)
        {
            dev2.SendHapticImpulse(0u, Mathf.Clamp01(amplitude * 0.3f), duration * 0.7f);
        }
    }

    // =========================================================================
    // LATEUPDATE - Fit canvas theo FOV + cập nhật màu visual
    // =========================================================================
    private void LateUpdate()
    {
        if (!_built) return;
        FitCanvas();
        UpdateHand(_left, true);
        UpdateHand(_right, false);
        bool bothTouching = _left.touching && _right.touching;
        if (!LetItBee.UI.BeeMenuInput.Blocked && !RhythmFlight && bothTouching && !_bothHandsTouching && enableFlightAssist && bee != null)
            bee.AddVerticalImpulse(dualHandBonusImpulse);
        _bothHandsTouching = bothTouching;
        UpdateVisuals();
    }

    private void FitCanvas()
    {
        if (_canvasRect == null) return;
        if (trackedCamera != null)
        {
            float cameraScale = Mathf.Max(0.0001f, trackedCamera.transform.lossyScale.z);
            float distance = Mathf.Max(0.75f, barDepth, trackedCamera.nearClipPlane / cameraScale + 0.2f);
            _canvasRect.localPosition = useSingleBar
                ? new Vector3(barCenterOffset.x, barCenterOffset.y, distance)
                : new Vector3(0f, 0f, distance);
        }

        const float scale = 0.001f;
        // The camera is already below the scaled XR origin.
        _canvasRect.localScale = new Vector3(scale, scale, scale);

        float canvasWorldW = 1920f * scale;
        float canvasWorldH = 1080f * scale;
        float widthPx = barWidth / scale;
        float thicknessPx = barThickness * barVisualThicknessScale / scale;

        if (useSingleBar)
        {
            if (_mainBarImage != null)
                FitBar(_mainBarImage, Vector2.zero, canvasWorldW, canvasWorldH, widthPx, thicknessPx);
        }
        else
        {
            FitBar(_left.barImage, leftBarOffset, canvasWorldW, canvasWorldH, widthPx, thicknessPx);
            FitBar(_right.barImage, rightBarOffset, canvasWorldW, canvasWorldH, widthPx, thicknessPx);
        }
    }

    private static void FitBar(Image bar, Vector2 offset, float worldW, float worldH,
                                float widthPx, float thicknessPx)
    {
        if (bar == null) return;
        Vector2 anchor = new Vector2(0.5f, 0.5f);
        bar.rectTransform.anchorMin = anchor;
        bar.rectTransform.anchorMax = anchor;
        bar.rectTransform.anchoredPosition = new Vector2(offset.x / 0.001f, offset.y / 0.001f);
        bar.rectTransform.sizeDelta = new Vector2(widthPx, thicknessPx);
    }

    private void UpdateVisuals()
    {
        bool anyDevice = _left.hasDevice || _right.hasDevice;
        bool flashing = _mainBarFlashTimer > 0f || _left.flash > 0f || _right.flash > 0f;

        Color targetColor;
        if (!showBars || (fadeBarsWhenIdle && !anyDevice))
            targetColor = new Color(barColor.r, barColor.g, barColor.b, 0f);
        else if (flashing)
            targetColor = flashColor;
        else
            targetColor = barColor;

        if (useSingleBar)
        {
            if (_mainBarImage != null)
            {
                _mainBarImage.gameObject.SetActive(showBars);
                _mainBarImage.color = Color.Lerp(_mainBarImage.color, targetColor, Mathf.Clamp01(Time.deltaTime * 12f));
            }
            if (_left.barImage != null) _left.barImage.gameObject.SetActive(false);
            if (_right.barImage != null) _right.barImage.gameObject.SetActive(false);
        }
        else
        {
            if (_mainBarImage != null) _mainBarImage.gameObject.SetActive(false);
            UpdateSplitBarVisual(_left);
            UpdateSplitBarVisual(_right);
        }
    }

    private void UpdateSplitBarVisual(HandState hand)
    {
        if (hand.barImage == null) return;
        hand.barImage.gameObject.SetActive(showBars);

        bool anyDevice = _left.hasDevice || _right.hasDevice;
        Color target;
        if (!showBars || (fadeBarsWhenIdle && !anyDevice))
            target = new Color(barColor.r, barColor.g, barColor.b, 0f);
        else if (hand.flash > 0f)
            target = flashColor;
        else
            target = barColor;

        hand.barImage.color = Color.Lerp(hand.barImage.color, target, Mathf.Clamp01(Time.deltaTime * 10f));
    }

    // =========================================================================
    // BUILD
    // =========================================================================
    private void TryBuild()
    {
        Resolve();
        if (trackedCamera == null) return;

        GameObject canvasGO = new GameObject("BeeSwipeBars");
        canvasGO.transform.SetParent(trackedCamera.transform, false);
        RectTransform rect = canvasGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1920f, 1080f);
        // Keep the interaction target below the centre of the tracked view.
        rect.localPosition = new Vector3(barCenterOffset.x, barCenterOffset.y, Mathf.Max(0.75f, barDepth));
        rect.localRotation = Quaternion.identity;
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvasRect = rect;

        // Thanh ngang nối liền duy nhất
        _mainBarImage = CreateBar("Bar_Continuous");

        // Hai thanh phụ cho chế độ legacy split
        _left.barImage = CreateBar("Bar_L");
        _right.barImage = CreateBar("Bar_R");

        _left.rayLine = CreateRayLine("Ray_L");
        _right.rayLine = CreateRayLine("Ray_R");

        _left.reticle = CreateGoldenReticle("Reticle_L");
        _right.reticle = CreateGoldenReticle("Reticle_R");

        AssignAnchors();
        if (showHandProxies)
        {
            CreateHandProxy(_left, "BeeHand_L");
            CreateHandProxy(_right, "BeeHand_R");
        }

        _built = true;
        Debug.Log("[BeeSwipeFlap] Đã dựng thanh quẹt ngang buồng lái cố định trên thân ong (parent = " +
                  transform.name + "). Người chơi tự do nhìn ngắm 360 độ thế giới!");
    }

    private Image CreateBar(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(_canvasRect, false);
        go.AddComponent<RectTransform>();
        Image image = go.AddComponent<Image>();
        image.sprite = BeeHudSprites.RoundedRect(96, 24);
        image.color = barColor;
        image.raycastTarget = false;
        return image;
    }

    private LineRenderer CreateRayLine(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = 0.009f;
        lr.endWidth = 0.005f;
        var embodiment = GetComponent<BeeEmbodiment>();
        if (embodiment != null) { lr.startWidth *= embodiment.Scale; lr.endWidth *= embodiment.Scale; }
        lr.textureMode = LineTextureMode.Stretch;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.enabled = false;

        // Gradient vàng kim neon (như ảnh mẫu)
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1.0f, 0.98f, 0.82f), 0.0f),  // Lõi trắng vàng sáng rực
                new GradientColorKey(new Color(1.0f, 0.82f, 0.18f), 0.4f),  // Vàng hổ phách neon
                new GradientColorKey(new Color(1.0f, 0.65f, 0.08f), 1.0f)   // Viền vàng cam ấm
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.95f, 0.7f),
                new GradientAlphaKey(0.85f, 1.0f)
            }
        );
        lr.colorGradient = grad;

        if (!_rayMaterialFailed && _rayMaterial == null)
        {
            _rayMaterial = CreateRayMaterial();
            if (_rayMaterial == null) _rayMaterialFailed = true;
        }
        if (_rayMaterial != null) lr.sharedMaterial = _rayMaterial;
        return lr;
    }

    private GameObject CreateGoldenReticle(string name)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.transform.SetParent(transform, false);
        go.name = name;
        Collider col = go.GetComponent<Collider>();
        if (col != null) SafeDestroy(col);

        var mr = go.GetComponent<MeshRenderer>();
        Material mat = goldenReticleMaterial;
        if (mat == null) mat = Resources.Load<Material>("Art/Bee/Materials/M_GoldenRing_Reticle");
#if UNITY_EDITOR
        if (mat == null) mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Bee/Materials/M_GoldenRing_Reticle.mat");
#endif
        if (mat == null)
        {
            mat = CreateUnlitMaterial(new Color(1f, 0.85f, 0.2f, 0.95f));
        }

        if (mr != null)
        {
            if (mat != null) mr.sharedMaterial = mat;
            else mr.enabled = false;
        }

        go.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        go.SetActive(false);
        return go;
    }

    private Material CreateRayMaterial()
    {
        if (goldenLaserMaterial != null)
        {
            _ownsRayMaterial = false;
            return goldenLaserMaterial;
        }

        Material mat = Resources.Load<Material>("Art/Bee/Materials/M_GoldenLaser");
#if UNITY_EDITOR
        if (mat == null)
            mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Bee/Materials/M_GoldenLaser.mat");
#endif
        if (mat != null)
        {
            _ownsRayMaterial = false;
            return mat;
        }

        _ownsRayMaterial = true;
        Material material = CreateUnlitMaterial(rayColor);
        return material;
    }

    private Material CreateUnlitMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) return null;

        Material material = new Material(shader);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        SetMaterialColor(material, color);
        return material;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
    }

    private void CreateHandProxy(HandState hand, string name)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) SafeDestroy(collider);

        hand.proxyMaterial = CreateUnlitMaterial(barColor);
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (hand.proxyMaterial != null) renderer.sharedMaterial = hand.proxyMaterial;
            else renderer.enabled = false;
        }

        go.transform.localScale = handProxyScale;
        go.SetActive(false);
        hand.proxy = go;
    }

    // =========================================================================
    // BEE HAND CONTROLLER SKINNING / THIẾT KẾ TAY ONG
    // =========================================================================
    private void ApplyBeeControllerSkin(Transform anchor, bool isLeft, HandState state)
    {
        if (anchor == null) return;

        Material matBody = beeHandBodyMaterial;
        if (matBody == null) matBody = Resources.Load<Material>("Art/Bee/Materials/M_BeeHand_Body");
#if UNITY_EDITOR
        if (matBody == null) matBody = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Bee/Materials/M_BeeHand_Body.mat");
#endif

        Material matBtn = beeHandButtonMaterial;
        if (matBtn == null) matBtn = Resources.Load<Material>("Art/Bee/Materials/M_BeeHand_Button");
#if UNITY_EDITOR
        if (matBtn == null) matBtn = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Bee/Materials/M_BeeHand_Button.mat");
#endif

        Material matAccent = beeHandAccentMaterial;
        if (matAccent == null) matAccent = Resources.Load<Material>("Art/Bee/Materials/M_BeeHand_Accent");
#if UNITY_EDITOR
        if (matAccent == null) matAccent = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Bee/Materials/M_BeeHand_Accent.mat");
#endif

        if (matBody == null) return;

        var renderers = anchor.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length == 0) return;

        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.gameObject.SetActive(true);
            r.enabled = true;

            string n = r.gameObject.name;
            if (n.Contains("Controller_Base") || n.Contains("BeeArm_Cuff"))
            {
                r.sharedMaterial = matBody;
            }
            else if (n.Contains("Button") || n.Contains("TouchPad") || n.Contains("Trigger") || n.Contains("Bumper"))
            {
                r.sharedMaterial = matBtn != null ? matBtn : matBody;
            }
            else if (n.Contains("ThumbStick"))
            {
                r.sharedMaterial = (n == "ThumbStick" && matAccent != null) ? matAccent : (matBtn != null ? matBtn : matBody);
            }
            else
            {
                r.sharedMaterial = matBody;
            }
        }

        // Đảm bảo không có SortingGroup 2D can thiệp làm ẩn mesh 3D trong VR
        var sg = anchor.GetComponent<UnityEngine.Rendering.SortingGroup>();
        if (sg != null) SafeDestroy(sg);

        // Đảm bảo Model luôn active
        Transform modelObj = anchor.Find("Model");
        if (modelObj != null) modelObj.gameObject.SetActive(true);

        // Tạo chóp vàng phát sáng (Bee Muzzle Glow) ở đầu mũi tay cầm làm điểm định hướng ngắm
        Transform tip = anchor.Find("RayTip");
        if (tip != null && matAccent != null && tip.Find("BeeMuzzleGlow") == null)
        {
            GameObject glowGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glowGO.name = "BeeMuzzleGlow";
            glowGO.transform.SetParent(tip, false);
            glowGO.transform.localPosition = Vector3.zero;
            glowGO.transform.localRotation = Quaternion.identity;
            glowGO.transform.localScale = new Vector3(0.016f, 0.016f, 0.016f);
            Collider col = glowGO.GetComponent<Collider>();
            if (col != null) SafeDestroy(col);
            var glowRend = glowGO.GetComponent<MeshRenderer>();
            if (glowRend != null) glowRend.sharedMaterial = matAccent;
        }

        state.skinned = true;
    }

    // =========================================================================
    // RESOLVE & POSE HELPERS
    // =========================================================================
    private void Resolve()
    {
        if (xrOrigin == null) xrOrigin = FindObjectOfType<XROrigin>();

        if (trackedCamera == null && xrOrigin != null && xrOrigin.Camera != null)
            trackedCamera = xrOrigin.Camera;

        if (trackedCamera == null)
        {
            foreach (Camera cam in FindObjectsOfType<Camera>())
            {
                if (!cam.isActiveAndEnabled) continue;
                if (cam.gameObject.name.Contains("UI Camera")) continue;
                trackedCamera = cam;
                break;
            }
        }

        if (bee == null) bee = GetComponent<BeeController>();
    }

    private static XRInputDevice GetDevice(XRNode node)
    {
        InputDevices.GetDevicesAtXRNode(node, _deviceBuffer);
        return _deviceBuffer.Count > 0 ? _deviceBuffer[0] : default;
    }

    private static ISDevice GetInputSystemDevice(bool isLeft)
    {
        string usage = isLeft ? "LeftHand" : "RightHand";
        string nameHint = isLeft ? "Left Controller" : "Right Controller";
        ISDevice nameFallback = null;

        foreach (ISDevice dev in InputSystem.devices)
        {
            var usages = dev.usages;
            for (int i = 0; i < usages.Count; i++)
            {
                if (string.Equals(usages[i].ToString(), usage, System.StringComparison.OrdinalIgnoreCase))
                    return dev;
            }

            if (nameFallback == null && !string.IsNullOrEmpty(dev.name))
            {
                bool nameMatch =
                    dev.name.IndexOf(usage, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dev.name.IndexOf(nameHint, System.StringComparison.OrdinalIgnoreCase) >= 0;
                bool looksLikeController =
                    dev.name.IndexOf("Controller", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dev.layout.IndexOf("XRController", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (nameMatch && looksLikeController) nameFallback = dev;
            }
        }
        return nameFallback;
    }

    private void FindControllerAnchors()
    {
        Transform root = xrOrigin != null ? xrOrigin.transform : null;
        if (root == null) return;

        // Ưu tiên GameObject có ActionBasedController hoặc XRBaseController trực tiếp
        var controllers = root.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.XRBaseController>(true);
        foreach (var c in controllers)
        {
            string n = c.name.ToLowerInvariant();
            if (n.Contains("left") && leftControllerAnchor == null) leftControllerAnchor = c.transform;
            else if (n.Contains("right") && rightControllerAnchor == null) rightControllerAnchor = c.transform;
        }

        if (leftControllerAnchor != null && rightControllerAnchor != null) return;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            bool isLeft = (n == "left controller" || n == "leftcontroller" || (n.Contains("left") && n.Contains("controller")));
            bool isRight = (n == "right controller" || n == "rightcontroller" || (n.Contains("right") && n.Contains("controller")));
            if (isLeft && leftControllerAnchor == null) leftControllerAnchor = t;
            else if (isRight && rightControllerAnchor == null) rightControllerAnchor = t;
        }
    }

    private void AssignAnchors()
    {
        FindControllerAnchors();

        if (_left.anchor == null && leftControllerAnchor != null)
        {
            _left.anchor = leftControllerAnchor;
        }
        if (_right.anchor == null && rightControllerAnchor != null)
        {
            _right.anchor = rightControllerAnchor;
        }

        DisableConflictingLineVisuals(leftControllerAnchor);
        DisableConflictingLineVisuals(rightControllerAnchor);
    }

    private void DisableConflictingLineVisuals(Transform anchor)
    {
        if (anchor == null) return;
        var monos = anchor.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < monos.Length; i++)
        {
            var m = monos[i];
            if (m != null && m.GetType().Name.Contains("LineVisual") && m.enabled)
            {
                m.enabled = false;
            }
        }

        var lrs = anchor.GetComponentsInChildren<LineRenderer>(true);
        for (int i = 0; i < lrs.Length; i++)
        {
            var lr = lrs[i];
            if (lr != null && lr != _left.rayLine && lr != _right.rayLine && lr.enabled)
            {
                lr.enabled = false;
            }
        }
    }

    private Transform TrackingParent =>
        xrOrigin != null && xrOrigin.CameraFloorOffsetObject != null
            ? xrOrigin.CameraFloorOffsetObject.transform
            : xrOrigin != null ? xrOrigin.transform : null;

    private Vector3 TrackingPoint(Vector3 trackingPos)
    {
        Transform parent = TrackingParent;
        return parent != null ? parent.TransformPoint(trackingPos) : trackingPos;
    }

    private Quaternion TrackingRot(Quaternion trackingRot)
    {
        Transform parent = TrackingParent;
        return parent != null ? parent.rotation * trackingRot : trackingRot;
    }
}
