// =============================================================================
// BeeGameOverScreen - Màn hình kết thúc game (Win / Game Over) trong VR.
// BeeGameOverScreen - VR end screen: Win or Game Over with sustainability summary.
//
// LAYOUT
//   World Space Canvas xuất hiện trước mặt player khi:
//     - BeeQuestManager.OnAllQuestsComplete → ShowWin()
//     - BeeSustainabilityManager.OnCollapsed → ShowGameOver()
//
//   WIN SCREEN:
//     🌟 "Hệ sinh thái Bền vững!"
//     Biểu đồ 3 chỉ số cuối + điểm tổng + đánh giá
//     [Chơi lại] [Về menu]
//
//   GAME OVER SCREEN:
//     💀 "Hệ sinh thái Sụp đổ!"
//     Kết quả 3 chỉ số + lý do thất bại
//     [Thử lại] [Về menu]
//
// CƠ CHẾ BUTTON VR
//   Dùng BeeController.OnExhausted pause input → GazeButton timeout (nhìn 2 giây)
//   hoặc XR Grip để chọn nút.
// =============================================================================
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BeeGameOverScreen : MonoBehaviour
{
    // =========================================================================
    // SETTINGS / CÀI ĐẶT
    // =========================================================================
    [Header("Placement / Vị trí")]
    [Tooltip("Khoảng cách canvas trước mặt player (m).")]
    [Range(0.5f, 3f)]
    public float canvasDistance = 1.5f;

    [Tooltip("Camera XR để tính vị trí đặt canvas.")]
    public Camera targetCamera;

    [Header("Colors / Màu sắc")]
    public Color winColor      = new Color(0.1f, 0.8f, 0.3f, 0.95f);
    public Color loseColor     = new Color(0.8f, 0.1f, 0.1f, 0.95f);
    public Color panelColor    = new Color(0f,   0f,   0f,   0.85f);

    [Header("Font")]
    public TMP_FontAsset font;

    [Header("Animation")]
    [Tooltip("Thời gian (giây) screen xuất hiện (fade in).")]
    [Range(0.2f, 2f)]
    public float fadeInDuration = 0.8f;

    // =========================================================================
    // EVENTS / SỰ KIỆN
    // =========================================================================
    [Header("Button Events")]
    public UnityEvent OnRestartClicked;
    public UnityEvent OnMainMenuClicked;

    // =========================================================================
    // INTERNAL
    // =========================================================================
    private Canvas     _canvas;
    private GameObject _panel;
    private CanvasGroup _cg;
    private TextMeshProUGUI _titleText, _subtitleText;
    private TextMeshProUGUI _envValue, _ecoValue, _socValue, _totalValue;
    private Image _envBar, _ecoBar, _socBar;
    private Button _restartBtn, _menuBtn;

    private bool _visible = false;
    private float _fadeTimer = 0f;

    private const float PanelW = 480f;
    private const float PanelH = 340f;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        BuildScreen();
        SetVisible(false, instant: true);
    }

    private void Update()
    {
        if (_visible && _fadeTimer < fadeInDuration)
        {
            _fadeTimer += Time.deltaTime;
            if (_cg != null) _cg.alpha = Mathf.Clamp01(_fadeTimer / fadeInDuration);
        }
    }

    // =========================================================================
    // ShowWin - Hiện màn hình chiến thắng.
    // =========================================================================
    public void ShowWin(float total, float env, float eco, float soc)
    {
        PositionInFrontOfCamera();
        SetScores(env, eco, soc, total);

        SetTitleColor(winColor);
        _titleText.text    = "HỆ SINH THÁI BỀN VỮNG!";
        _subtitleText.text = GetWinRating(total);

        SetVisible(true);
        Debug.Log($"[BeeGameOverScreen] WIN! Tổng: {total:F0}");
    }

    // =========================================================================
    // ShowGameOver - Hiện màn hình thất bại.
    // =========================================================================
    public void ShowGameOver(float total, float env, float eco, float soc)
    {
        PositionInFrontOfCamera();
        SetScores(env, eco, soc, total);

        SetTitleColor(loseColor);
        _titleText.text    = "HỆ SINH THÁI SỤP ĐỔ!";
        _subtitleText.text = GetLoseReason(env, eco, soc);

        SetVisible(true);
        Debug.Log($"[BeeGameOverScreen] GAME OVER. Tổng: {total:F0}");
    }

    // =========================================================================
    // Hide
    // =========================================================================
    public void Hide() => SetVisible(false);

    // =========================================================================
    // BuildScreen - Tạo UI từ code (không cần prefab).
    // =========================================================================
    private void BuildScreen()
    {
        // Canvas
        var go = new GameObject("BeeGameOverCanvas");
        go.transform.SetParent(transform, false);
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 99;

        var cr = _canvas.GetComponent<RectTransform>();
        cr.sizeDelta = new Vector2(PanelW + 40f, PanelH + 40f);

        _cg = go.AddComponent<CanvasGroup>();
        go.AddComponent<GraphicRaycaster>();

        // Panel nền
        _panel = MakeRect("Panel", cr, PanelW, PanelH, Vector2.zero);
        var panelImg = _panel.AddComponent<Image>();
        panelImg.color = panelColor;
        var outline = _panel.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.15f);
        outline.effectDistance = new Vector2(2f, -2f);

        var pr = _panel.GetComponent<RectTransform>();

        // Title
        _titleText = MakeText("TitleLbl", pr, 0f, -20f, PanelW - 20f, 40f, 22f, FontStyles.Bold, Color.white);
        _titleText.alignment = TextAlignmentOptions.Center;

        // Subtitle
        _subtitleText = MakeText("SubLbl", pr, 0f, -66f, PanelW - 20f, 28f, 13f, FontStyles.Normal,
            new Color(0.8f, 0.8f, 0.8f, 1f));
        _subtitleText.alignment = TextAlignmentOptions.Center;

        // Score bars
        float barY = -108f;
        float barW = PanelW - 80f;

        (_envBar, _envValue) = MakeScoreRow("Môi trường",
            new Color(0.3f, 0.9f, 0.3f), pr, barY,       barW);
        (_ecoBar, _ecoValue) = MakeScoreRow("Kinh tế",
            new Color(1f, 0.75f, 0.1f),  pr, barY - 42f, barW);
        (_socBar, _socValue) = MakeScoreRow("Xã hội",
            new Color(0.4f, 0.7f, 1f),   pr, barY - 84f, barW);

        // Total
        _totalValue = MakeText("TotalLbl", pr, 0f, -224f, PanelW - 20f, 24f, 16f, FontStyles.Bold,
            Color.white);
        _totalValue.alignment = TextAlignmentOptions.Center;

        // Buttons
        MakeButton("BtnRestart", pr, -80f, -286f, 140f, 36f, "Thử lại",
            new Color(0.2f, 0.6f, 1f), () => { Hide(); OnRestartClicked?.Invoke(); });

        MakeButton("BtnMenu", pr, 80f, -286f, 140f, 36f, "Về menu",
            new Color(0.5f, 0.5f, 0.5f), () => { Hide(); OnMainMenuClicked?.Invoke(); });
    }

    // =========================================================================
    // SetScores
    // =========================================================================
    private void SetScores(float env, float eco, float soc, float total)
    {
        SetBar(_envBar, _envValue, env / 100f, $"{env:F0}");
        SetBar(_ecoBar, _ecoValue, eco / 100f, $"{eco:F0}");
        SetBar(_socBar, _socValue, soc / 100f, $"{soc:F0}");
        _totalValue.text = $"Điểm bền vững: {total:F0} / 100";
    }

    private static void SetBar(Image fill, TextMeshProUGUI val, float ratio, string text)
    {
        if (fill != null)
        {
            var rt = fill.GetComponent<RectTransform>();
            float pw = fill.transform.parent.GetComponent<RectTransform>().rect.width;
            rt.sizeDelta = new Vector2(pw * ratio, 0f);
        }
        if (val != null) val.text = text;
    }

    // =========================================================================
    // Helpers
    // =========================================================================
    private void SetVisible(bool v, bool instant = false)
    {
        _visible = v;
        _fadeTimer = instant ? fadeInDuration : 0f;
        if (_cg != null) _cg.alpha = instant ? (v ? 1f : 0f) : 0f;
        _canvas.gameObject.SetActive(v);
    }

    private void PositionInFrontOfCamera()
    {
        if (targetCamera == null) return;
        Transform cam = targetCamera.transform;

        float scale = 0.001f; // WorldSpace canvas: 1px = 1mm
        _canvas.transform.position = cam.position + cam.forward * canvasDistance;
        _canvas.transform.rotation = cam.rotation;
        _canvas.transform.localScale = Vector3.one * scale;
    }

    private void SetTitleColor(Color c)
    {
        if (_titleText != null) _titleText.color = c;
        var panelImg = _panel?.GetComponent<Image>();
        if (panelImg != null)
            panelImg.color = new Color(c.r * 0.15f, c.g * 0.15f, c.b * 0.15f, 0.9f);
    }

    private static string GetWinRating(float total) => total switch
    {
        >= 90f => "Xuất sắc — ĐBSCL xanh tươi mãi mãi!",
        >= 70f => "Tốt — Hệ sinh thái phát triển rực rỡ.",
        _      => "Hoàn thành — Tiếp tục cải thiện nhé!"
    };

    private static string GetLoseReason(float env, float eco, float soc)
    {
        if (env < eco && env < soc) return "Môi trường cạn kiệt — hoa chết quá nhiều!";
        if (eco < env && eco < soc) return "Kinh tế suy sụp — mật ong không đủ!";
        return "Xã hội rạn vỡ — đa dạng sinh học quá thấp!";
    }

    // =========================================================================
    // UI Builders
    // =========================================================================
    private static GameObject MakeRect(string name, RectTransform parent, float w, float h, Vector2 anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    private TextMeshProUGUI MakeText(string name, RectTransform parent,
        float x, float y, float w, float h, float size, FontStyles style, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = name;
        tmp.fontSize = size;
        tmp.color = col;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        if (font != null) tmp.font = font;
        return tmp;
    }

    private (Image bar, TextMeshProUGUI val) MakeScoreRow(string label, Color col, RectTransform parent,
        float y, float barW)
    {
        // Label
        MakeText($"Lbl_{label}", parent, -(barW * 0.5f + 20f), y, 100f, 20f, 11f, FontStyles.Normal, Color.white);

        // Bar bg
        var bgGo = new GameObject("BarBg");
        bgGo.transform.SetParent(parent, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 1f);
        bgRt.pivot = new Vector2(0.5f, 1f);
        bgRt.anchoredPosition = new Vector2(20f, y);
        bgRt.sizeDelta = new Vector2(barW, 16f);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(col.r, col.g, col.b, 0.2f);

        // Bar fill
        var fillGo = new GameObject("BarFill");
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot     = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition = Vector2.zero;
        fillRt.sizeDelta = new Vector2(0f, 0f);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = col;

        // Value
        var val = MakeText($"Val_{label}", parent, 20f + barW * 0.5f + 16f, y, 50f, 20f, 11f, FontStyles.Bold, col);

        return (fillImg, val);
    }

    private void MakeButton(string name, RectTransform parent, float x, float y, float w, float h,
        string label, Color col, System.Action onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);

        var img = go.AddComponent<Image>();
        img.color = col;

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());

        var lbl = MakeText($"Lbl_{name}", rt, 0f, 0f, w, h, 12f, FontStyles.Bold, Color.white);
        lbl.text = label;
        lbl.alignment = TextAlignmentOptions.Center;
    }
}
