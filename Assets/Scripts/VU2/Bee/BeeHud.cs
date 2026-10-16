// =============================================================================
// BeeHud - HUD goc nhin con ong, dung y nhu anh mau (Bee POV HUD).
// BeeHud - HUD góc nhìn con ong, dựng y như ảnh mẫu.
//
// CAU TRUC TREN MAN HINH (theo anh mau)
//   Goc tren trai : binh mat ong  "6 / 20" + thanh tien do
//                   panel nhiem vu "Thu thap mat hoa / Dua mat ve to"
//   Goc duoi trai : thanh "Mat hoa" + icon luc giac
//   Goc tren phai : la ban N/E/S/W (hoa la ban quay theo huong bay)
//   Ben phai      : banner do "Ke thu / Tranh xa ong dich"
//   Giua duoi     : thuoc toc do + chevron vang chay theo toc do
//   Hai goc duoi   : goi y tay cam "Vo canh de bay (trai)/(phai)"
//
// VI SAO DUNG BANG CODE (khong dung prefab)
//   Du an chua co art HUD cho phan nay; dung bang code thi khong can keo tha
//   Inspector, khong lo mat reference khi sua prefab, va chi ton vai sprite
//   sinh tu BeeHudSprites (vai KB VRAM).
//
// CACH HUD BAM VAO MAT NGUOI CHOI
//   Canvas World Space duoc parent vao camera XR, cach camera hudDistance (m),
//   va duoc scale lai moi frame theo FOV hien tai (BeeCameraRig co mo rong FOV
//   khi bay nhanh) -> HUD luon dung ti le khung nhin, khong bi "teo".
//
// DAT HUD O DAY / WHERE TO PUT IT
//   GameObject "BeeHUD" la con cua Main Camera (XR). Menu:
//   VU2/Bee/Add Bee HUD To Camera
// =============================================================================
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BeeHud : MonoBehaviour
{
    /// <summary>Nguon du lieu cho thanh "Mat hoa".</summary>
    public enum NectarSource
    {
        /// <summary>Lay tu BeeGameStats (khi da co he thong thu mat).</summary>
        Stats = 0,

        /// <summary>Lay tu the luc con ong (dung khi chua co gameplay).</summary>
        Stamina = 1
    }

    // =========================================================================
    // REFERENCES / THAM CHIEU
    // =========================================================================
    [Header("References / Tham chiếu")]
    [Tooltip("Camera XR (khung hinh HUD bam theo). De trong se tu tim.")]
    public Camera targetCamera;

    [Tooltip("Con ong - de doc toc do / huong bay / the luc. De trong se tu tim.")]
    public BeeController bee;

    [Tooltip("Du lieu gameplay (mat da giao, mat hoa, nguy hiem). Co the de trong.")]
    public BeeGameStats stats;

    // =========================================================================
    // PLACEMENT / VI TRI
    // =========================================================================
    [Header("Placement / Vị trí")]
    [Tooltip("Khoang cach canvas toi camera (m). 1.2-1.5m de doc nhat va thoang nhat trong VR.")]
    [Range(0.4f, 2.5f)]
    public float hudDistance = 1.3f;

    [Header("HUD Layout / Vi tri (0..1: trai-duoi den phai-tren)")]
    public Vector2 honeyPosition = new Vector2(0.12f, 0.30f);
    public Vector2 questPosition = new Vector2(0.12f, 0.18f);
    public Vector2 nectarPosition = new Vector2(0.12f, 0.07f);
    public Vector2 compassPosition = new Vector2(0.88f, 0.17f);
    public Vector2 altitudePosition = new Vector2(0.5f, 0.07f);
    [Range(0.5f, 1.2f)] public float panelScale = 0.85f;
    public bool showFlapHints = false;
    [Tooltip("Height gauge range in metres above the starting position.")]
    [Min(1f)] public float altitudeRange = 20f;
    [Tooltip("Optional height datum. Without this, height is relative to the bee's starting position.")]
    public Transform altitudeReference;
    [Tooltip("Optional real hive target. No marker is shown when unassigned.")]
    public Transform hiveTarget;
    private float _startingHeight;
    private bool _heightCaptured;
    private RectTransform _hiveMarker;


    [Tooltip("HUD chiem bao nhieu phan khung nhin (1 = sat mep nhu anh mau).")]
    [Range(0.6f, 1.05f)]
    public float viewportFill = 0.95f;

    [Tooltip("Do phan giai tham chieu de dung layout (khong lien quan man hinh that).")]
    public Vector2 referenceResolution = new Vector2(1920f, 1080f);

    [Tooltip("Font chu HUD. De trong se dung font mac dinh cua TextMeshPro.")]
    public TMP_FontAsset font;

    [Header("Swipe Hint / Chu goi y vo canh")]
    [Tooltip("Chu tren goi y goc duoi - co che vo canh moi: quet ray qua thanh (BeeSwipeFlap).")]
    public string flapHintMain = "Quẹt để bay";

    // =========================================================================
    // DATA / DU LIEU
    // =========================================================================
    [Header("Data / Dữ liệu")]
    [Tooltip("Thanh 'Mat hoa' lay du lieu tu dau.")]
    public NectarSource nectarSource = NectarSource.Stats;

    [Tooltip("Chi hien banner khi co ke thu that su.")]
    public bool alwaysShowDangerBanner = false;

    [Tooltip("Nhay banner do khi co nguy hiem (tao cam giac canh bao).")]
    public bool blinkDangerBanner = true;

    // =========================================================================
    // COLORS / MAU SAC (theo anh mau)
    // =========================================================================
    [Header("Colors / Màu sắc")]
    public Color panelColor = new Color(0f, 0f, 0f, 0.42f);
    public Color panelEdgeColor = new Color(1f, 1f, 1f, 0.10f);
    public Color amberColor = new Color(1f, 0.72f, 0.11f, 1f);
    public Color amberDimColor = new Color(1f, 0.72f, 0.11f, 0.22f);
    public Color textColor = new Color(1f, 1f, 1f, 0.95f);
    public Color textDimColor = new Color(1f, 1f, 1f, 0.62f);
    public Color dangerColor = new Color(1f, 0.22f, 0.16f, 1f);
    public Color trackColor = new Color(1f, 1f, 1f, 0.18f);

    // =========================================================================
    // INTERNAL / NOI BO
    // =========================================================================
    private Canvas _canvas;
    private RectTransform _canvasRect;
    private Material _hudMaterial;
    private bool _built;

    // Text
    private TextMeshProUGUI _honeyCountText;
    private TextMeshProUGUI _questTitleText;
    private TextMeshProUGUI _questSubText;
    private TextMeshProUGUI _questCounterText;
    private TextMeshProUGUI _nectarLabelText;
    private TextMeshProUGUI _dangerTitleText;
    private TextMeshProUGUI _dangerSubText;
    private TextMeshProUGUI _speedText;

    // Images / Rect
    private Image _honeyFill;
    private Image _nectarFill;
    private Image _speedMarker;
    private RectTransform _compassRose;
    private CanvasGroup _dangerGroup;
    private RectTransform _speedTrackRect;

    private float _blinkPhase;

    // =========================================================================
    // LIFECYCLE / VONG DOI
    // =========================================================================
    private void Awake()
    {
        Build();
    }

    /// <summary>Dung lai toan bo HUD (dung khi doi font/mau trong Editor).</summary>
    [ContextMenu("Rebuild HUD")]
    public void Rebuild()
    {
        if (_canvasRect != null) Destroy(_canvasRect.gameObject);
        _built = false;
        Build();
    }

    private void Build()
    {
        if (_built) return;
        Resolve();
        if (targetCamera == null)
        {
            Debug.LogWarning("[BeeHud] Khong tim thay camera. HUD se khong hien.");
            return;
        }

        if (bee != null && !_heightCaptured) { _startingHeight = bee.transform.position.y; _heightCaptured = true; }
        BuildCanvas();
        BuildHoneyPanel();
        BuildQuestPanel();
        BuildNectarPanel();
        BuildCompass();
        BuildDangerBanner();
        BuildSpeedGauge();
        BuildFlapHints();

        _built = true;
        Debug.Log("[BeeHud] Da dung HUD goc nhin con ong (kieu anh mau).");
    }

    /// <summary>Tim camera / ong / du lieu neu nguoi dung de trong.</summary>
    private void Resolve()
    {
        // Uu tien camera cua XR Origin (rig VR that). Neu khong, dung Camera.main
        // nhung tranh "UI Camera" (camera phang cua giao dien cu) - gan HUD vao
        // no thi HUD se hien tren man hinh may tinh chu khong hien trong kinh VR.
        if (targetCamera == null || IsUiCamera(targetCamera))
        {
            XROrigin origin = FindObjectOfType<XROrigin>();
            if (origin != null && origin.Camera != null) targetCamera = origin.Camera;
        }

        if (targetCamera == null || IsUiCamera(targetCamera))
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null || IsUiCamera(targetCamera))
        {
            foreach (Camera cam in FindObjectsOfType<Camera>())
            {
                if (!cam.isActiveAndEnabled) continue;
                if (IsUiCamera(cam)) continue;
                targetCamera = cam;
                break;
            }
        }

        if (bee == null) bee = FindObjectOfType<BeeController>();
        if (stats == null) stats = FindObjectOfType<BeeGameStats>();
    }

    private static bool IsUiCamera(Camera camera)
    {
        return camera != null && camera.gameObject.name.Contains("UI Camera");
    }

    private void LateUpdate()
    {
        if (!_built) Build();
        if (_canvasRect == null) return;

        FitToCamera();
        ApplyLayout();
        UpdateValues();
    }

    // =========================================================================
    // FitToCamera - Giu HUD dung ti le khung nhin du FOV thay doi khi bay nhanh.
    //   worldHeight = 2 * d * tan(fov/2)  ->  scale = fill * min(rong, cao)
    // =========================================================================
    private void FitToCamera()
    {
        if (targetCamera == null) return;

        // Use a comfortable central angular area, never the desktop mirror aspect.
        float distance = Mathf.Max(hudDistance, targetCamera.nearClipPlane + 0.2f);
        float halfWidth = distance * Mathf.Tan(30f * Mathf.Deg2Rad);
        float halfHeight = distance * Mathf.Tan(20f * Mathf.Deg2Rad);
        var corners = new Vector3[4];
        int eyeCount = targetCamera.stereoEnabled ? 2 : 1;
        for (int eye = 0; eye < eyeCount; eye++)
        {
            var eyeType = targetCamera.stereoEnabled
                ? (eye == 0 ? Camera.MonoOrStereoscopicEye.Left : Camera.MonoOrStereoscopicEye.Right)
                : Camera.MonoOrStereoscopicEye.Mono;
            targetCamera.CalculateFrustumCorners(new Rect(0f, 0f, 1f, 1f), distance, eyeType, corners);
            // Fit inside both eyes, including asymmetric headset projections.
            for (int i = 0; i < corners.Length; i++)
            {
                halfWidth = Mathf.Min(halfWidth, Mathf.Abs(corners[i].x) * 0.8f);
                halfHeight = Mathf.Min(halfHeight, Mathf.Abs(corners[i].y) * 0.8f);
            }
        }
        _canvasRect.localPosition = new Vector3(0f, 0f, distance);
        float worldHeight = 2f * halfHeight;
        float worldWidth = 2f * halfWidth;

        float scaleX = worldWidth / Mathf.Max(referenceResolution.x, 1f);
        float scaleY = worldHeight / Mathf.Max(referenceResolution.y, 1f);
        float scale = viewportFill * Mathf.Min(scaleX, scaleY);

        if (scale > 0f) _canvasRect.localScale = new Vector3(scale, scale, 1f);
    }

    // =========================================================================
    // UpdateValues - Day du lieu that vao HUD moi frame.
    // =========================================================================
    private void UpdateValues()
    {
        int delivered = stats != null ? stats.honeyDelivered : 0;
        int goal = stats != null ? stats.honeyGoal : 20;

        // --- Binh mat ong (goc tren trai) ---
        if (_honeyCountText != null) _honeyCountText.text = delivered + " / " + goal;
        if (_honeyFill != null) _honeyFill.fillAmount = stats != null ? stats.Progress01 : 0f;

        // --- Panel nhiem vu ---
        if (_questTitleText != null && stats != null) _questTitleText.text = stats.questTitle;
        if (_questSubText != null && stats != null) _questSubText.text = stats.questSubtitle;
        if (_questCounterText != null) _questCounterText.text = delivered + " / " + goal;

        // --- Thanh mat hoa (goc duoi trai) ---
        float nectar = nectarSource == NectarSource.Stats
            ? (stats != null ? stats.nectar01 : 0f)
            : (bee != null ? bee.StaminaNormalized : 0f);
        if (_nectarFill != null) _nectarFill.fillAmount = Mathf.Clamp01(nectar);

        // Compass follows horizontal headset heading; +Z is north, +X is east.
        if (_compassRose != null && targetCamera != null)
        {
            Vector3 forward = targetCamera.transform.forward;
            if (forward.x * forward.x + forward.z * forward.z > 0.0001f)
            {
                float heading = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                _compassRose.localEulerAngles = new Vector3(0f, 0f, heading);
            }
        }
        if (_hiveMarker != null)
        {
            _hiveMarker.gameObject.SetActive(hiveTarget != null);
            if (hiveTarget != null && targetCamera != null)
            {
                Vector3 delta = hiveTarget.position - targetCamera.transform.position;
                float bearing = Mathf.Atan2(delta.x, delta.z);
                _hiveMarker.anchoredPosition = new Vector2(Mathf.Sin(bearing), Mathf.Cos(bearing)) * 78f;
            }
        }

        // --- Banner nguy hiem ---
        if (_dangerGroup != null)
        {
            bool danger = stats != null && stats.dangerActive;
            float targetAlpha = (danger || alwaysShowDangerBanner) ? 1f : 0f;

            if (danger && blinkDangerBanner)
            {
                _blinkPhase += Time.deltaTime * 7f;
                targetAlpha *= Mathf.Lerp(0.5f, 1f, (Mathf.Sin(_blinkPhase) + 1f) * 0.5f);
            }

            _dangerGroup.alpha = Mathf.MoveTowards(_dangerGroup.alpha, targetAlpha, Time.deltaTime * 4f);
        }

        // --- Thuoc toc do (giua duoi) ---
        if (bee != null && !_heightCaptured)
        {
            _startingHeight = bee.transform.position.y;
            _heightCaptured = true;
        }
        float datum = altitudeReference != null ? altitudeReference.position.y : _startingHeight;
        float altitude = bee != null ? bee.transform.position.y - datum : 0f;
        float speedNorm = Mathf.Clamp01(altitude / Mathf.Max(1f, altitudeRange));

        if (_speedMarker != null && _speedTrackRect != null)
        {
            float halfWidth = Mathf.Max(_speedTrackRect.sizeDelta.x * 0.5f - 14f, 1f);
            Vector2 position = _speedMarker.rectTransform.anchoredPosition;
            position.x = Mathf.Lerp(-halfWidth, halfWidth, Mathf.Clamp01(speedNorm));
            _speedMarker.rectTransform.anchoredPosition = position;
        }

        if (_speedText != null)
        {
            _speedText.text = "Độ cao " + altitude.ToString("0.0") + " m";
        }
    }

    // =========================================================================
    // BUILD CANVAS / DUNG KHUNG HUD
    // =========================================================================
    private void ApplyLayout()
    {
        PositionPanel("Panel_Honey", honeyPosition);
        PositionPanel("Panel_Quest", questPosition);
        PositionPanel("Panel_Nectar", nectarPosition);
        PositionPanel("Panel_Compass", compassPosition);
        PositionPanel("Panel_Speed", altitudePosition);
        foreach (string name in new[] { "Hint_FlapLeft", "Hint_FlapRight" })
        {
            Transform hint = _canvasRect.Find(name);
            if (hint != null) hint.gameObject.SetActive(showFlapHints);
        }
    }

    private void PositionPanel(string name, Vector2 position)
    {
        RectTransform panel = _canvasRect.Find(name) as RectTransform;
        if (panel == null) return;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.localScale = Vector3.one * panelScale;
        Vector2 half = panel.sizeDelta * panelScale * 0.5f;
        Vector2 size = _canvasRect.rect.size;
        panel.anchoredPosition = new Vector2(
            Mathf.Clamp((position.x - 0.5f) * size.x, -size.x * 0.5f + half.x, size.x * 0.5f - half.x),
            Mathf.Clamp((position.y - 0.5f) * size.y, -size.y * 0.5f + half.y, size.y * 0.5f - half.y));
    }

    private void BuildCanvas()
    {
        var canvasGo = new GameObject("BeeHUD_Canvas", typeof(RectTransform));
        canvasGo.hideFlags = HideFlags.DontSave;

        _canvasRect = canvasGo.GetComponent<RectTransform>();
        _canvasRect.SetParent(targetCamera.transform, false);
        _canvasRect.sizeDelta = referenceResolution;
        _canvasRect.localPosition = new Vector3(0f, 0f, hudDistance);
        _canvasRect.localRotation = Quaternion.identity;
        _canvasRect.localScale = Vector3.one;

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 30000;

        // Vat lieu rieng voi renderQueue 3100 + ZTest LessEqual -> HUD ve tren canh vat
        // nhung khong che khuat tay cam VR o cu ly gan (0.3 - 0.6m).
        _hudMaterial = new Material(Canvas.GetDefaultCanvasMaterial())
        {
            hideFlags = HideFlags.DontSave
        };
        _hudMaterial.renderQueue = 3100;
        _hudMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
    }

    // =========================================================================
    // UI HELPERS / HAM DUNG UI
    // =========================================================================

    /// <summary>Node rong co RectTransform (dung lam khung/neo layout).</summary>
    private RectTransform NewRect(Transform parent, string name, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.hideFlags = HideFlags.DontSave;

        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    /// <summary>Anh UI (sprite bo goc -> tu chon Sliced cho kho meo goc).</summary>
    private Image NewImage(Transform parent, string name, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size, Sprite sprite, Color color)
    {
        RectTransform rect = NewRect(parent, name, anchor, pivot, position, size);

        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null && sprite.border != Vector4.zero
            ? Image.Type.Sliced
            : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        image.material = _hudMaterial;
        return image;
    }

    /// <summary>Chu TextMeshPro (mac dinh khong xuong dong de layout co dinh).</summary>
    private TextMeshProUGUI NewText(Transform parent, string name, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size, string value, float fontSize, Color color,
        TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
    {
        RectTransform rect = NewRect(parent, name, anchor, pivot, position, size);

        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        if (font != null) text.font = font;
        return text;
    }

    /// <summary>Panel nen bo goc + vien sang mong (kieu the kinh mo trong anh mau).</summary>
    private Image NewPanel(Transform parent, string name, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size, int radius = 20)
    {
        Image panel = NewImage(parent, name, anchor, pivot, position, size,
            BeeHudSprites.RoundedRect(96, radius), panelColor);

        // Vien = mot panel sang hon, KHONG vien tron, lon hon 4px nam ngay sau.
        Image edge = NewImage(panel.transform, "Edge", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, size + new Vector2(4f, 4f),
            BeeHudSprites.RoundedRect(96, Mathf.Min(radius + 3, 47)), panelEdgeColor);
        edge.rectTransform.SetAsFirstSibling();
        return panel;
    }

    /// <summary>Thanh nen (track) cua progress bar. Tra ve rect de gan fill vao.</summary>
    private RectTransform NewBarTrack(Transform parent, string name, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size)
    {
        Image track = NewImage(parent, name, anchor, pivot, position, size,
            BeeHudSprites.RoundedRect(64, Mathf.RoundToInt(size.y * 0.5f)), trackColor);
        return track.rectTransform;
    }

    /// <summary>Phan to mau cua thanh (Filled Horizontal, tu co gian theo track).</summary>
    private Image NewBarFill(Transform parent, string name, Color color)
    {
        RectTransform rect = NewRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(3f, 3f);
        rect.offsetMax = new Vector2(-3f, -3f);

        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = BeeHudSprites.RoundedRect(64, 28);
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 0f;
        image.color = color;
        image.raycastTarget = false;
        image.material = _hudMaterial;
        return image;
    }

    // =========================================================================
    // HONEY PANEL / BINH MAT ONG (goc tren trai)
    //   [luc giac]  6 / 20
    //               [============------]
    // =========================================================================
    private void BuildHoneyPanel()
    {
        Image panel = NewPanel(_canvasRect, "Panel_Honey", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(32f, -28f), new Vector2(300f, 104f), 24);

        // Icon: luc giac lon mo + luc giac nho dac mau mat ong.
        NewImage(panel.transform, "Icon", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -20f), new Vector2(64f, 64f),
            BeeHudSprites.Hexagon(96), amberDimColor);
        NewImage(panel.transform, "Icon_Fill", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(25f, -29f), new Vector2(46f, 46f),
            BeeHudSprites.Hexagon(96), amberColor);

        _honeyCountText = NewText(panel.transform, "Count", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(94f, -22f), new Vector2(190f, 48f), "0 / 20", 42f, textColor,
            TextAlignmentOptions.Left, FontStyles.Bold);

        // Thanh tien do mat ong da giao.
        RectTransform track = NewBarTrack(panel.transform, "Bar", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(94f, -78f), new Vector2(190f, 14f));
        _honeyFill = NewBarFill(track, "Fill", amberColor);
    }

    // =========================================================================
    // QUEST PANEL / PANEL NHIEM VU (duoi binh mat)
    // =========================================================================
    private void BuildQuestPanel()
    {
        Image panel = NewPanel(_canvasRect, "Panel_Quest", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(32f, -148f), new Vector2(330f, 122f), 20);

        NewImage(panel.transform, "Icon", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(18f, -22f), new Vector2(46f, 46f),
            BeeHudSprites.Hexagon(96), amberDimColor);
        NewImage(panel.transform, "Icon_Fill", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(25f, -29f), new Vector2(32f, 32f),
            BeeHudSprites.Hexagon(96), amberColor);

        _questTitleText = NewText(panel.transform, "Title", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(78f, -20f), new Vector2(240f, 34f), "Thu thập mật hoa", 26f, textColor,
            TextAlignmentOptions.Left, FontStyles.Bold);

        _questSubText = NewText(panel.transform, "Subtitle", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(78f, -54f), new Vector2(240f, 30f), "Đưa mật về tổ", 22f, textDimColor,
            TextAlignmentOptions.Left);

        _questCounterText = NewText(panel.transform, "Counter", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(78f, -84f), new Vector2(240f, 30f), "0 / 20", 24f, amberColor,
            TextAlignmentOptions.Left, FontStyles.Bold);
    }

    // =========================================================================
    // NECTAR PANEL / THANH MAT HOA (goc duoi trai, tren goi y tay cam)
    // =========================================================================
    private void BuildNectarPanel()
    {
        Image panel = NewPanel(_canvasRect, "Panel_Nectar", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(32f, 252f), new Vector2(300f, 86f), 20);

        NewImage(panel.transform, "Icon", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -20f), new Vector2(50f, 50f),
            BeeHudSprites.Hexagon(96), amberDimColor);
        NewImage(panel.transform, "Icon_Fill", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -28f), new Vector2(34f, 34f),
            BeeHudSprites.Hexagon(96), amberColor);

        _nectarLabelText = NewText(panel.transform, "Label", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(80f, -14f), new Vector2(200f, 30f), "Mật hoa", 24f, textColor,
            TextAlignmentOptions.Left, FontStyles.Bold);

        RectTransform track = NewBarTrack(panel.transform, "Bar", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(80f, -52f), new Vector2(196f, 14f));
        _nectarFill = NewBarFill(track, "Fill", amberColor);
    }

    // =========================================================================
    // COMPASS / LA BAN (goc tren phai)
    //   - Vanh tron + 4 chu N/E/S/W.
    //   - HOA LA BAN (_compassRose) quay nguoc huong bay -> chu huong hien tai
    //     luon nam o dinh la ban (kieu HUD GPS trong game).
    //   - Tam giac vang o tam = huong dang bay; cham do nho = huong to ong.
    // =========================================================================
    private void BuildCompass()
    {
        RectTransform root = NewRect(_canvasRect, "Panel_Compass", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-26f, -24f), new Vector2(200f, 200f));

        NewImage(root, "Ring", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(200f, 200f), BeeHudSprites.Ring(200, 5), new Color(1f, 1f, 1f, 0.30f));

        // Hoa la ban (quay theo huong bay).
        _compassRose = NewRect(root, "Rose", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(200f, 200f));

        const float letterRadius = 72f;
        NewCompassLetter(_compassRose, "N", new Vector2(0f, letterRadius), amberColor);
        NewCompassLetter(_compassRose, "E", new Vector2(letterRadius, 0f), textColor);
        NewCompassLetter(_compassRose, "S", new Vector2(0f, -letterRadius), textColor);
        NewCompassLetter(_compassRose, "W", new Vector2(-letterRadius, 0f), textColor);

        // Cham chi huong to ong (goc 35 do nhu anh mau).
        float bearing = 35f * Mathf.Deg2Rad;
        _hiveMarker = NewImage(_compassRose, "HiveMarker", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(Mathf.Sin(bearing) * 78f, Mathf.Cos(bearing) * 78f), new Vector2(16f, 16f),
            BeeHudSprites.Hexagon(64), new Color(1f, 0.35f, 0.45f, 0.95f)).rectTransform;

        // Mui ten "huong dang bay" co dinh o dinh la ban.
        NewImage(root, "HeadingPointer", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 104f), new Vector2(26f, 26f),
            BeeHudSprites.TriangleUp(64), amberColor);

        NewImage(root, "HeadingArrow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 16f), new Vector2(30f, 30f),
            BeeHudSprites.TriangleUp(64), textColor);
    }

    /// <summary>Mot chu huong tren hoa la ban (N / E / S / W).</summary>
    private void NewCompassLetter(Transform parent, string letter, Vector2 offset, Color color)
    {
        NewText(parent, "Dir_" + letter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            offset, new Vector2(44f, 44f), letter, 26f, color,
            TextAlignmentOptions.Center, FontStyles.Bold);
    }

    // =========================================================================
    // DANGER BANNER / BANNER KE THU (ben phai duoi la ban)
    // =========================================================================
    private void BuildDangerBanner()
    {
        Image panel = NewPanel(_canvasRect, "Panel_Danger", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-26f, -246f), new Vector2(304f, 88f), 20);
        panel.color = new Color(0.30f, 0.04f, 0.03f, 0.55f);

        _dangerGroup = panel.gameObject.AddComponent<CanvasGroup>();
        _dangerGroup.alpha = alwaysShowDangerBanner ? 1f : 0f;
        _dangerGroup.blocksRaycasts = false;
        _dangerGroup.interactable = false;

        // Icon do tron + dau cham than.
        NewImage(panel.transform, "Icon", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(18f, -17f), new Vector2(54f, 54f), BeeHudSprites.Circle(96), dangerColor);
        NewText(panel.transform, "IconMark", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(18f, -18f), new Vector2(54f, 54f), "!", 34f, Color.white,
            TextAlignmentOptions.Center, FontStyles.Bold);

        _dangerTitleText = NewText(panel.transform, "Title", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(86f, -14f), new Vector2(206f, 32f), "Kẻ thù", 26f,
            new Color(1f, 0.35f, 0.30f, 1f), TextAlignmentOptions.Left, FontStyles.Bold);

        _dangerSubText = NewText(panel.transform, "Subtitle", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(86f, -48f), new Vector2(206f, 30f), "Tránh xa ong dịch", 22f, textDimColor,
            TextAlignmentOptions.Left);
    }

    // =========================================================================
    // SPEED GAUGE / THUOC TOC DO (giua duoi)
    //   Thanh ngang mo + chevron vang chay tu trai (cham) sang phai (het toc).
    // =========================================================================
    private void BuildSpeedGauge()
    {
        RectTransform root = NewRect(_canvasRect, "Panel_Speed", new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(460f, 100f));

        _speedTrackRect = NewBarTrack(root, "Track", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 24f), new Vector2(420f, 10f));

        _speedMarker = NewImage(_speedTrackRect, "Marker", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 20f), new Vector2(26f, 26f), BeeHudSprites.TriangleUp(64), amberColor);

        _speedText = NewText(root, "SpeedText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 68f), new Vector2(300f, 30f), "Độ cao 0.0 m", 22f, textDimColor,
            TextAlignmentOptions.Center);
    }

    // =========================================================================
    // FLAP HINTS / GOI Y TAY CAM (hai goc duoi)
    //   Ve tay cam bang hinh hoc + mui ten cong + chu "Vo canh de bay (trai/phai)".
    // =========================================================================
    private void BuildFlapHints()
    {
        BuildFlapHint(true);
        BuildFlapHint(false);
    }

    private void BuildFlapHint(bool isLeft)
    {
        Vector2 corner = isLeft ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
        float sx = isLeft ? 1f : -1f;   // Lat truc X cho ben phai.

        RectTransform root = NewRect(_canvasRect, isLeft ? "Hint_FlapLeft" : "Hint_FlapRight",
            corner, corner, new Vector2(sx * 48f, 30f), new Vector2(212f, 202f));

        Color line = new Color(1f, 1f, 1f, 0.55f);

        // Than tay cam: hinh lon sang + hinh nho toi -> tao vien mong.
        NewImage(root, "Body_Outer", corner, corner, new Vector2(sx * 34f, 74f),
            new Vector2(74f, 118f), BeeHudSprites.RoundedRect(96, 36), line);
        NewImage(root, "Body_Inner", corner, corner, new Vector2(sx * 40f, 80f),
            new Vector2(62f, 106f), BeeHudSprites.RoundedRect(96, 30), panelColor);

        // Nut joystick.
        NewImage(root, "Stick", corner, corner, new Vector2(sx * 52f, 128f),
            new Vector2(38f, 38f), BeeHudSprites.Circle(64), line);
        NewImage(root, "Stick_Inner", corner, corner, new Vector2(sx * 57f, 133f),
            new Vector2(28f, 28f), BeeHudSprites.Circle(64), panelColor);

        // Mui ten cong (uoc le bang thanh nghieng + dau mui) = dong tac vo canh.
        Image arm = NewImage(root, "Swipe", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(sx * 14f, 46f), new Vector2(58f, 7f),
            BeeHudSprites.RoundedRect(96, 48), new Color(1f, 1f, 1f, 0.45f));
        arm.rectTransform.localEulerAngles = new Vector3(0f, 0f, sx * 22f);

        Image head = NewImage(root, "SwipeHead", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(sx * 40f, 60f), new Vector2(20f, 20f),
            BeeHudSprites.TriangleUp(64), new Color(1f, 1f, 1f, 0.45f));
        head.rectTransform.localEulerAngles = new Vector3(0f, 0f, sx * 68f);

        // Chu huong dan.
        NewText(root, "Label", corner, corner, new Vector2(0f, 34f), new Vector2(212f, 30f),
            flapHintMain, 22f, textDimColor, TextAlignmentOptions.Center);
        NewText(root, "Side", corner, corner, new Vector2(0f, 6f), new Vector2(212f, 28f),
            isLeft ? "(trái)" : "(phải)", 22f, textDimColor, TextAlignmentOptions.Center);
    }

    // =========================================================================
    // PUBLIC API / HAM CONG KHAI
    // =========================================================================

    /// <summary>Canvas cua HUD (de bat/tat hoac doi sorting layer).</summary>
    public Canvas HudCanvas => _canvas;

    /// <summary>Doi chu tren thanh mat hoa (dung khi doi ngon ngu).</summary>
    public void SetNectarLabel(string label)
    {
        if (_nectarLabelText != null) _nectarLabelText.text = label;
    }

    /// <summary>Doi noi dung banner ke thu (dung khi doi ngon ngu).</summary>
    public void SetDangerText(string title, string subtitle)
    {
        if (_dangerTitleText != null) _dangerTitleText.text = title;
        if (_dangerSubText != null) _dangerSubText.text = subtitle;
    }
}
