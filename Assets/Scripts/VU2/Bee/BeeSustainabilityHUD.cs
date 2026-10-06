// =============================================================================
// BeeSustainabilityHUD - HUD hiển thị 3 chỉ số Phát triển Bền vững.
// BeeSustainabilityHUD - HUD showing the 3 Sustainability pillars in VR.
//
// LAYOUT (đặt cùng với BeeHud, hoặc thêm vào canvas của BeeHud)
//   Góc dưới phải màn hình (hoặc panel riêng):
//   ┌─────────────────────────────────┐
//   │ 🌿 Môi trường  [████████░░] 78 │
//   │ 💰 Kinh tế     [██████░░░░] 61 │
//   │ 👥 Xã hội      [█████░░░░░] 52 │
//   │ ⚡ Tổng        [███████░░░] 64 │
//   │ Trạng thái: Ổn định             │
//   └─────────────────────────────────┘
//
// CƠ CHẾ BÁM CAMERA / CAMERA FOLLOW
//   Dùng cùng pattern với BeeHud: WorldSpace Canvas parent vào camera XR.
//   Script tự tính size từ FOV để HUD không bị teo.
//
// CHẾ ĐỘ HIỂN THỊ
//   - Compact: chỉ 3 thanh nhỏ (mặc định khi đang bay)
//   - Full: panel đầy đủ với số liệu và label (khi dừng hoặc nhìn xuống)
// =============================================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BeeSustainabilityHUD : MonoBehaviour
{
    // =========================================================================
    // REFERENCES / THAM CHIẾU
    // =========================================================================
    [Header("References / Tham chiếu")]
    [Tooltip("Camera XR để HUD bám theo. Để trống sẽ tự tìm Main Camera.")]
    public Camera targetCamera;

    [Tooltip("BeeSustainabilityManager nguồn dữ liệu. Để trống sẽ tự tìm.")]
    public BeeSustainabilityManager sustainability;

    // =========================================================================
    // PLACEMENT / VỊ TRÍ
    // =========================================================================
    [Header("Placement / Vị trí")]
    [Tooltip("Khoảng cách canvas từ camera (m).")]
    [Range(0.4f, 2.5f)]
    public float hudDistance = 0.9f;

    [Tooltip("Offset vị trí HUD trong không gian screen (NDC, -1 to 1).")]
    public Vector2 screenPosition = new Vector2(0.38f, -0.3f); // góc dưới phải

    [Tooltip("Phân giải tham chiếu layout.")]
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);

    // =========================================================================
    // COLORS / MÀU SẮC
    // =========================================================================
    [Header("Colors / Màu sắc")]
    public Color envColor   = new Color(0.3f, 0.9f, 0.3f, 1f);   // Xanh lá
    public Color ecoColor   = new Color(1f,   0.75f, 0.1f, 1f);  // Vàng/Amber
    public Color socColor   = new Color(0.4f, 0.7f, 1f,   1f);   // Xanh dương
    public Color totalColor = new Color(1f,   1f,   1f,   1f);   // Trắng
    public Color panelColor = new Color(0f,   0f,   0f,   0.5f); // Đen mờ
    public Color thrivingColor  = new Color(0.3f, 1f,   0.3f, 1f);
    public Color stableColor    = new Color(1f,   1f,   0.3f, 1f);
    public Color decliningColor = new Color(1f,   0.5f, 0.1f, 1f);
    public Color collapsedColor = new Color(1f,   0.2f, 0.2f, 1f);

    [Header("Font")]
    public TMP_FontAsset font;

    // =========================================================================
    // ANIMATION
    // =========================================================================
    [Header("Animation / Hiệu ứng")]
    [Tooltip("Tốc độ thanh progress cập nhật (smooth). Cao = instant.")]
    [Range(1f, 20f)]
    public float barSmoothSpeed = 8f;

    [Tooltip("Panel nhấp nháy khi hệ sinh thái sụp đổ (collapsed).")]
    public bool blinkOnCollapse = true;

    // =========================================================================
    // INTERNAL CANVAS COMPONENTS
    // =========================================================================
    private Canvas _canvas;
    private RectTransform _canvasRect;

    // Panel chính
    private GameObject _panel;
    private Image _panelBg;

    // Thanh progress 4 pillar
    private Image _envBar, _ecoBar, _socBar, _totalBar;
    private float _envTarget, _ecoTarget, _socTarget, _totalTarget;

    // Labels
    private TextMeshProUGUI _envLabel, _ecoLabel, _socLabel, _totalLabel;
    private TextMeshProUGUI _statusLabel;
    private TextMeshProUGUI _envValue, _ecoValue, _socValue, _totalValue;

    // Blink state
    private float _blinkTimer;
    private bool _blink;

    // =========================================================================
    // CONSTANTS (UI sizing in reference pixels)
    // =========================================================================
    private const float PanelW    = 320f;
    private const float PanelH    = 170f;
    private const float BarH      = 14f;
    private const float RowSpacing = 36f;
    private const float PadX      = 14f;
    private const float PadY      = 12f;
    private const float FontSize   = 13f;
    private const float BarBgAlpha = 0.25f;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (sustainability == null)
            sustainability = FindObjectOfType<BeeSustainabilityManager>();

        BuildCanvas();
        BuildPanel();
    }

    private void OnEnable()
    {
        if (sustainability != null)
            sustainability.OnStatusChanged += HandleStatusChanged;
    }

    private void OnDisable()
    {
        if (sustainability != null)
            sustainability.OnStatusChanged -= HandleStatusChanged;
    }

    private void LateUpdate()
    {
        FollowCamera();
        PollData();
        SmoothBars();
        AnimateBlink();
    }

    // =========================================================================
    // BuildCanvas - Tạo WorldSpace Canvas bám camera.
    // =========================================================================
    private void BuildCanvas()
    {
        var go = new GameObject("BeeHUD_Sustainability");
        go.transform.SetParent(transform, false);

        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 10;

        _canvasRect = _canvas.GetComponent<RectTransform>();
        _canvasRect.sizeDelta = referenceResolution;
    }

    // =========================================================================
    // BuildPanel - Tạo panel với 4 thanh progress và labels.
    // =========================================================================
    private void BuildPanel()
    {
        _panel = CreateRect("SustPanel", _canvasRect, PanelW, PanelH);
        PositionPanel();

        // Nền panel
        _panelBg = _panel.AddComponent<Image>();
        _panelBg.color = panelColor;
        AddRoundedEdge(_panel);

        // Tiêu đề
        var title = CreateLabel("Bền Vững", _panel.GetComponent<RectTransform>(),
            PadX, PanelH - PadY - 18f, PanelW - PadX * 2, 20f, 14f, FontStyle.Bold, totalColor);

        // 4 hàng pillar
        float y = PanelH - PadY - 28f;

        (  _envBar,   _envLabel,   _envValue) = CreateBarRow("Môi trường",  envColor,  y -= RowSpacing);
        (  _ecoBar,   _ecoLabel,   _ecoValue) = CreateBarRow("Kinh tế",     ecoColor,  y -= RowSpacing);
        (  _socBar,   _socLabel,   _socValue) = CreateBarRow("Xã hội",      socColor,  y -= RowSpacing);
        (_totalBar, _totalLabel, _totalValue) = CreateBarRow("Tổng",        totalColor, y -= RowSpacing);

        // Label trạng thái
        _statusLabel = CreateLabel("Ổn định", _panel.GetComponent<RectTransform>(),
            PadX, PadY - 4f, PanelW - PadX * 2, 18f, 12f, FontStyle.Normal, stableColor);
        _statusLabel.alignment = TextAlignmentOptions.Left;
    }

    // =========================================================================
    // CreateBarRow - Tạo 1 hàng: label + thanh progress + số.
    // =========================================================================
    private (Image bar, TextMeshProUGUI label, TextMeshProUGUI value)
        CreateBarRow(string labelText, Color barColor, float y)
    {
        float barWidth = PanelW - PadX * 2 - 60f;

        // Label
        var lbl = CreateLabel(labelText, _panel.GetComponent<RectTransform>(),
            PadX, y + 2f, 120f, BarH + 2f, FontSize, FontStyle.Normal, Color.white);

        // Bar background
        var bgGo = CreateRect("BarBg", _panel.GetComponent<RectTransform>(), barWidth, BarH);
        bgGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(PadX + 115f, y);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(barColor.r, barColor.g, barColor.b, BarBgAlpha);

        // Bar fill
        var fillGo = CreateRect("BarFill", bgGo.GetComponent<RectTransform>(), barWidth, BarH);
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(barWidth, 0f);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = barColor;

        // Value label (bên phải)
        float valX = PadX + 115f + barWidth + 6f;
        var val = CreateLabel("50", _panel.GetComponent<RectTransform>(),
            valX, y, 36f, BarH, FontSize - 1f, FontStyle.Bold, barColor);
        val.alignment = TextAlignmentOptions.Left;

        return (fillImg, lbl, val);
    }

    // =========================================================================
    // PollData - Đọc điểm từ BeeSustainabilityManager và cập nhật targets.
    // =========================================================================
    private void PollData()
    {
        if (sustainability == null) return;

        _envTarget   = sustainability.EnvironmentScore / 100f;
        _ecoTarget   = sustainability.EconomicScore    / 100f;
        _socTarget   = sustainability.SocialScore      / 100f;
        _totalTarget = sustainability.TotalScore       / 100f;

        // Cập nhật số trực tiếp
        _envValue.text   = $"{sustainability.EnvironmentScore:F0}";
        _ecoValue.text   = $"{sustainability.EconomicScore:F0}";
        _socValue.text   = $"{sustainability.SocialScore:F0}";
        _totalValue.text = $"{sustainability.TotalScore:F0}";
    }

    // =========================================================================
    // SmoothBars - Cập nhật chiều rộng thanh progress có smooth.
    // =========================================================================
    private void SmoothBars()
    {
        float t = 1f - Mathf.Exp(-barSmoothSpeed * Time.deltaTime);
        SetBarFill(_envBar,   ref _envTarget,   t);
        SetBarFill(_ecoBar,   ref _ecoTarget,   t);
        SetBarFill(_socBar,   ref _socTarget,   t);
        SetBarFill(_totalBar, ref _totalTarget, t);
    }

    private void SetBarFill(Image fill, ref float target, float t)
    {
        if (fill == null) return;
        var rect = fill.GetComponent<RectTransform>();
        float parentW = fill.transform.parent.GetComponent<RectTransform>().rect.width;
        float currentW = rect.sizeDelta.x;
        float newW = Mathf.Lerp(currentW, parentW * target, t);
        rect.sizeDelta = new Vector2(newW, 0f);
    }

    // =========================================================================
    // AnimateBlink - Nhấp nháy khi collapsed.
    // =========================================================================
    private void AnimateBlink()
    {
        if (!blinkOnCollapse || sustainability == null) return;
        if (sustainability.Status != BeeSustainabilityManager.EcosystemStatus.Collapsed) return;

        _blinkTimer += Time.deltaTime * 3f;
        float alpha = (Mathf.Sin(_blinkTimer) + 1f) * 0.5f;
        if (_panelBg != null) _panelBg.color = new Color(0.8f, 0f, 0f, alpha * 0.7f);
    }

    // =========================================================================
    // HandleStatusChanged - Cập nhật label trạng thái.
    // =========================================================================
    private void HandleStatusChanged(BeeSustainabilityManager.EcosystemStatus status)
    {
        if (_statusLabel == null) return;

        (_statusLabel.text, _statusLabel.color) = status switch
        {
            BeeSustainabilityManager.EcosystemStatus.Thriving  => ("Bội thu — Hệ sinh thái phát triển!", thrivingColor),
            BeeSustainabilityManager.EcosystemStatus.Stable    => ("Ổn định",                             stableColor),
            BeeSustainabilityManager.EcosystemStatus.Declining => ("Suy thoái — Hãy thụ phấn nhiều hơn!", decliningColor),
            BeeSustainabilityManager.EcosystemStatus.Collapsed => ("SỤP ĐỔ — Hệ sinh thái đã chết!",     collapsedColor),
            _ => ("Không rõ", Color.white)
        };
    }

    // =========================================================================
    // FollowCamera - Giữ canvas trước mặt camera.
    // =========================================================================
    private void FollowCamera()
    {
        if (targetCamera == null) return;

        Transform cam = targetCamera.transform;
        float fov = targetCamera.fieldOfView;

        // Tính scale để HUD luôn chiếm đúng tỉ lệ khung hình
        float halfHeight = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * hudDistance;
        float scale = halfHeight * 2f / referenceResolution.y;

        var ct = _canvas.transform;
        ct.position = cam.position + cam.forward * hudDistance
                    + cam.right   * (screenPosition.x * halfHeight * targetCamera.aspect)
                    + cam.up      * (screenPosition.y * halfHeight);
        ct.rotation = cam.rotation;
        ct.localScale = Vector3.one * scale;
    }

    // =========================================================================
    // PositionPanel - Đặt panel trong canvas reference resolution.
    // =========================================================================
    private void PositionPanel()
    {
        var rt = _panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; // Đặt chính giữa canvas (FollowCamera sẽ dịch chuyển)
    }

    // =========================================================================
    // UI Helpers
    // =========================================================================
    private static GameObject CreateRect(string name, RectTransform parent, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // Top-left anchor
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        return go;
    }

    private TextMeshProUGUI CreateLabel(string text, RectTransform parent,
        float x, float y, float w, float h,
        float size, FontStyle style, Color color)
    {
        var go = new GameObject($"Lbl_{text.Replace(" ", "")}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        if (font != null) tmp.font = font;
        if (style == FontStyle.Bold) tmp.fontStyle = FontStyles.Bold;

        return tmp;
    }

    private static void AddRoundedEdge(GameObject panel)
    {
        // Thêm outline mảnh để panel trông sạch hơn
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.1f);
        outline.effectDistance = new Vector2(1f, -1f);
    }
}
