using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================================================
// TidalClockUI - Visual tidal clock with moon orbit display.
// TidalClockUI - Đồng hồ triều trực quan với hiển thị quỹ đạo Mặt trăng.
//
// DESIGN (matching the game screenshot):
// - Circular clock face with tick marks around the orbit
// - Moon icon orbiting the circle
// - Earth/reference icon at center
// - 4 labeled positions: 1-Không trăng, 2-Trăng khuyết, 3-Trăng tròn, 4-Trăng khuyết
// - Text label showing current tide state (Triều Cường / Triều Kém)
// - Arrow indicators showing "Tia sáng Mặt trời" direction
//
// THIẾT KẾ (khớp với screenshot game):
// - Mặt đồng hồ tròn với các vạch chia quanh quỹ đạo
// - Icon Mặt trăng quay quanh vòng tròn
// - Icon Trái đất ở trung tâm
// - 4 vị trí có nhãn: 1-Không trăng, 2-Trăng khuyết, 3-Trăng tròn, 4-Trăng khuyết
// - Chữ hiển thị trạng thái triều (Triều Cường / Triều Kém)
// - Mũi tên chỉ hướng "Tia sáng Mặt trời"
// =============================================================================
public class TidalClockUI : MonoBehaviour
{
    // =========================================================================
    // REFERENCES
    // THAM CHIẾU
    // =========================================================================
    [Header("Clock Components / Thành phần đồng hồ")]
    
    [Tooltip("The moon icon RectTransform that orbits the clock.\n"
           + "RectTransform của icon Mặt trăng quay quanh đồng hồ.")]
    public RectTransform moonIcon;

    [Tooltip("The center pivot point of the clock.\n"
           + "Điểm pivot trung tâm của đồng hồ.")]
    public RectTransform clockCenter;

    [Tooltip("Radius of the moon orbit in UI units.\n"
           + "Bán kính quỹ đạo Mặt trăng tính bằng đơn vị UI.")]
    public float orbitRadius = 60f;

    [Header("Moon Phase Icons / Icon pha Mặt trăng")]
    
    [Tooltip("Sprites for 4 moon phases: [0]=NewMoon, [1]=FirstQuarter, [2]=FullMoon, [3]=LastQuarter.\n"
           + "Sprite cho 4 pha: [0]=Không trăng, [1]=Trăng khuyết, [2]=Trăng tròn, [3]=Trăng khuyết.")]
    public Sprite[] moonPhaseSprites = new Sprite[4];

    [Tooltip("Image component on the moon icon (to swap sprites).\n"
           + "Component Image trên icon Mặt trăng (để đổi sprite).")]
    public Image moonImage;

    [Header("Tick Marks / Vạch chia")]
    
    [Tooltip("Parent transform containing all tick mark images.\n"
           + "Transform cha chứa tất cả vạch chia.")]
    public RectTransform tickMarkParent;

    [Tooltip("Number of tick marks around the orbit.\n"
           + "Số vạch chia quanh quỹ đạo.")]
    public int tickMarkCount = 16;

    [Tooltip("Prefab for a single tick mark.\n"
           + "Prefab cho 1 vạch chia.")]
    public GameObject tickMarkPrefab;

    [Header("Position Markers / Đánh dấu vị trí")]
    
    [Tooltip("4 marker images at fixed positions on the orbit: 0=Top(Pos1), 1=Right(Pos2), 2=Bottom(Pos3), 3=Left(Pos4).\n"
           + "4 marker tại vị trí cố định: 0=Trên(VT1), 1=Phải(VT2), 2=Dưới(VT3), 3=Trái(VT4).")]
    public Image[] positionMarkers = new Image[4];

    [Tooltip("Color for active (current) position marker.\n"
           + "Màu cho marker vị trí đang hoạt động.")]
    public Color activeMarkerColor = Color.yellow;

    [Tooltip("Color for inactive position markers.\n"
           + "Màu cho marker vị trí không hoạt động.")]
    public Color inactiveMarkerColor = new Color(1f, 1f, 1f, 0.4f);

    [Header("Text Labels / Nhãn chữ")]
    
    [Tooltip("Text showing current tide state: 'Triều Cường' or 'Triều Kém'.\n"
           + "Chữ hiển thị trạng thái triều: 'Triều Cường' hoặc 'Triều Kém'.")]
    public TextMeshProUGUI tideStateText;

    [Tooltip("Text showing moon phase name.\n"
           + "Chữ hiển thị tên pha Mặt trăng.")]
    public TextMeshProUGUI moonPhaseText;

    [Header("Tide Indicator / Chỉ báo triều")]
    
    [Tooltip("Fill image that shows tidal intensity (0-1).\n"
           + "Image fill hiển thị cường độ triều (0-1).")]
    public Image tidalIntensityFill;

    [Tooltip("Color gradient for tidal intensity: low (neap) → high (spring).\n"
           + "Gradient màu cho cường độ triều: thấp (kém) → cao (cường).")]
    public Gradient tidalIntensityGradient;

    [Header("Warning Icon / Icon cảnh báo")]
    
    [Tooltip("Warning/alarm icon shown during Spring Tide.\n"
           + "Icon cảnh báo/báo động hiển thị khi triều cường.")]
    public GameObject springTideWarningIcon;

    // =========================================================================
    // INTERNAL
    // NỘI BỘ
    // =========================================================================
    private TidalClockManager _manager;
    private bool _ticksGenerated = false;

    // =========================================================================
    // Vietnamese display strings / Chuỗi hiển thị tiếng Việt
    // =========================================================================
    private readonly string[] _phaseNames = new string[]
    {
        "Không trăng",      // NewMoon (Position 1)
        "Trăng khuyết",     // FirstQuarter (Position 2)
        "Trăng tròn",       // FullMoon (Position 3)
        "Trăng khuyết"      // LastQuarter (Position 4)
    };

    private const string SPRING_TIDE_TEXT = "Triều Cường";
    private const string NEAP_TIDE_TEXT = "Triều Kém";

    // =========================================================================
    // LIFECYCLE
    // VÒNG ĐỜI
    // =========================================================================
    private void Start()
    {
        _manager = TidalClockManager.Instance;
        if (_manager == null)
        {
            // TidalClockManager may not be ready yet — retry in 1s.
            // TidalClockManager có thể chưa sẵn sàng — thử lại sau 1s.
            Debug.Log("[TidalClockUI] TidalClockManager not yet found, retrying in 1s...");
            Invoke(nameof(RetryFindManager), 1f);
        }
        else
        {
            InitializeUI();
        }
    }

    /// <summary>
    /// Retry finding TidalClockManager after a delay.
    /// Thử lại tìm TidalClockManager sau delay.
    /// </summary>
    private void RetryFindManager()
    {
        _manager = TidalClockManager.Instance;
        if (_manager == null)
        {
            Debug.LogWarning("[TidalClockUI] TidalClockManager still not found after retry!");
            enabled = false;
            return;
        }
        Debug.Log("[TidalClockUI] Found TidalClockManager on retry.");
        InitializeUI();
    }

    /// <summary>
    /// Set up subscriptions, tick marks, and initial UI state.
    /// Thiết lập subscriptions, vạch chia, và trạng thái UI ban đầu.
    /// </summary>
    private void InitializeUI()
    {
        GenerateTickMarks();

        // Subscribe to events for discrete changes.
        // Đăng ký lắng nghe sự kiện cho thay đổi rời rạc.
        TidalClockManager.OnTidalPhaseChanged += OnPhaseChanged;
        TidalClockManager.OnTidalStateChanged += OnTideStateChanged;

        // Initialize warning icon state.
        // Khởi tạo trạng thái icon cảnh báo.
        if (springTideWarningIcon) 
            springTideWarningIcon.SetActive(false);

        // Initialize gradient if not set.
        // Khởi tạo gradient nếu chưa đặt.
        if (tidalIntensityGradient == null)
        {
            tidalIntensityGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.6f, 1f), 0f);    // Light blue (neap)
            colorKeys[1] = new GradientColorKey(new Color(0.1f, 0.3f, 0.8f), 0.5f); // Medium blue
            colorKeys[2] = new GradientColorKey(new Color(0.8f, 0.2f, 0.2f), 1f);   // Red (spring)
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            tidalIntensityGradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    private void OnDestroy()
    {
        TidalClockManager.OnTidalPhaseChanged -= OnPhaseChanged;
        TidalClockManager.OnTidalStateChanged -= OnTideStateChanged;
    }

    private void Update()
    {
        if (_manager == null) return;

        UpdateMoonPosition();
        UpdateMoonSprite();
        UpdatePositionMarkers();
        UpdateIntensityIndicator();
    }

    // =========================================================================
    // MOON ORBIT ANIMATION
    // ANIMATION QUỸ ĐẠO MẶT TRĂNG
    // =========================================================================
    
    /// <summary>
    /// Move moon icon around the circular orbit.
    /// Di chuyển icon Mặt trăng quanh quỹ đạo tròn.
    /// 
    /// Position mapping (matching the reference diagram):
    /// - Phase 0.00 (Position 1, NewMoon):      LEFT   (9 o'clock, angle = 180°)
    /// - Phase 0.25 (Position 2, FirstQuarter):  BOTTOM (6 o'clock, angle = 270°)
    /// - Phase 0.50 (Position 3, FullMoon):      RIGHT  (3 o'clock, angle = 0°)
    /// - Phase 0.75 (Position 4, LastQuarter):   TOP    (12 o'clock, angle = 90°)
    /// 
    /// Ánh xạ vị trí (khớp sơ đồ tham chiếu):
    /// - Pha 0.00 (VT1, Không trăng):   TRÁI   (9 giờ, góc = 180°)
    /// - Pha 0.25 (VT2, Trăng khuyết):  DƯỚI   (6 giờ, góc = 270°)
    /// - Pha 0.50 (VT3, Trăng tròn):    PHẢI   (3 giờ, góc = 0°)
    /// - Pha 0.75 (VT4, Trăng khuyết):  TRÊN   (12 giờ, góc = 90°)
    /// </summary>
    private void UpdateMoonPosition()
    {
        if (!moonIcon || !clockCenter) return;

        // Convert phase to angle: start at LEFT (180°), go clockwise.
        // Chuyển pha thành góc: bắt đầu từ TRÁI (180°), quay theo chiều kim đồng hồ.
        float angleRad = (Mathf.PI - _manager.MoonPhaseNormalized * 2f * Mathf.PI);

        float x = Mathf.Cos(angleRad) * orbitRadius;
        float y = Mathf.Sin(angleRad) * orbitRadius;

        moonIcon.anchoredPosition = clockCenter.anchoredPosition + new Vector2(x, y);
    }

    /// <summary>
    /// Swap moon sprite based on current phase.
    /// Đổi sprite Mặt trăng theo pha hiện tại.
    /// </summary>
    private void UpdateMoonSprite()
    {
        if (!moonImage || moonPhaseSprites == null || moonPhaseSprites.Length < 4) return;

        int phaseIndex = (int)_manager.CurrentPhase;
        if (phaseIndex >= 0 && phaseIndex < moonPhaseSprites.Length && moonPhaseSprites[phaseIndex] != null)
        {
            moonImage.sprite = moonPhaseSprites[phaseIndex];
        }
    }

    /// <summary>
    /// Highlight the active position marker on the orbit.
    /// Tô sáng marker vị trí đang hoạt động trên quỹ đạo.
    /// </summary>
    private void UpdatePositionMarkers()
    {
        if (positionMarkers == null || positionMarkers.Length < 4) return;

        int activeIndex = (int)_manager.CurrentPhase;
        for (int i = 0; i < 4; i++)
        {
            if (positionMarkers[i] != null)
            {
                positionMarkers[i].color = (i == activeIndex) ? activeMarkerColor : inactiveMarkerColor;
            }
        }
    }

    /// <summary>
    /// Update the tidal intensity fill bar and color.
    /// Cập nhật thanh fill cường độ triều và màu sắc.
    /// </summary>
    private void UpdateIntensityIndicator()
    {
        float intensity = _manager.TidalIntensity;

        if (tidalIntensityFill)
        {
            tidalIntensityFill.fillAmount = intensity;
            if (tidalIntensityGradient != null)
            {
                tidalIntensityFill.color = tidalIntensityGradient.Evaluate(intensity);
            }
        }
    }

    // =========================================================================
    // EVENT HANDLERS
    // XỬ LÝ SỰ KIỆN
    // =========================================================================
    
    private void OnPhaseChanged(TidalPhase phase)
    {
        // Update moon phase text.
        // Cập nhật chữ pha Mặt trăng.
        if (moonPhaseText)
        {
            int idx = (int)phase;
            moonPhaseText.text = (idx >= 0 && idx < _phaseNames.Length) ? _phaseNames[idx] : "";
        }
    }

    private void OnTideStateChanged(TidalState state)
    {
        // Update tide state text.
        // Cập nhật chữ trạng thái triều.
        if (tideStateText)
        {
            tideStateText.text = state == TidalState.SpringTide ? SPRING_TIDE_TEXT : NEAP_TIDE_TEXT;
            tideStateText.color = state == TidalState.SpringTide
                ? new Color(0.9f, 0.2f, 0.2f)  // Red for Spring Tide
                : new Color(0.2f, 0.6f, 1f);    // Blue for Neap Tide
        }

        // Toggle warning icon.
        // Bật/tắt icon cảnh báo.
        if (springTideWarningIcon)
        {
            springTideWarningIcon.SetActive(state == TidalState.SpringTide);
        }
    }

    // =========================================================================
    // TICK MARK GENERATION
    // TẠO VẠCH CHIA
    // =========================================================================
    
    /// <summary>
    /// Generate tick marks around the orbit circle.
    /// Tạo vạch chia quanh vòng tròn quỹ đạo.
    /// Call in Start() or manually in Editor.
    /// Gọi trong Start() hoặc tự gọi trong Editor.
    /// </summary>
    private void GenerateTickMarks()
    {
        if (_ticksGenerated || !tickMarkParent || !tickMarkPrefab) return;
        if (tickMarkCount <= 0) return;

        for (int i = 0; i < tickMarkCount; i++)
        {
            float angle = (float)i / tickMarkCount * 2f * Mathf.PI;
            float x = Mathf.Cos(angle) * orbitRadius;
            float y = Mathf.Sin(angle) * orbitRadius;

            GameObject tick = Instantiate(tickMarkPrefab, tickMarkParent);
            RectTransform rt = tick.GetComponent<RectTransform>();
            if (rt)
            {
                rt.anchoredPosition = new Vector2(x, y);
                rt.localRotation = Quaternion.Euler(0, 0, -angle * Mathf.Rad2Deg);
            }
            tick.SetActive(true);
        }

        _ticksGenerated = true;
    }
}
