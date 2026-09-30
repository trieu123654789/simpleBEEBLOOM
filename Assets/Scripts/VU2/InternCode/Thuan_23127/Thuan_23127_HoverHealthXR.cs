using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class Thuan_23127_HoverHealthXR : MonoBehaviour
{   
    [Header("Scroll UI Sample panel")]
    public GameObject panel;            
    [Tooltip("Text tiêu đề (ví dụ 'Độ mặn')")]
    public Text headText;                   
    [Tooltip("Text thườngP")]
    public Text bodyText;        

    public Thuan_23127_JsonReader jsonReader; 

    private Thuan_23127_PlantGrowth _growth;
    private XRSimpleInteractable _xr;
    private bool _shown;
    private CanvasGroup _cg;

    /// <summary>
    /// Tự động tìm panel và text components nếu chưa được assign
    /// </summary>
    private void FindPanelAndTexts()
    {
        // Tìm panel nếu null - tìm theo tên phổ biến hoặc tag
        if (!panel)
        {
            // Tìm trong children với các tên phổ biến
            Transform panelTransform = transform.Find("Panel") 
                ?? transform.Find("HoverPanel") 
                ?? transform.Find("HealthPanel")
                ?? transform.Find("InfoPanel");
            
            if (panelTransform)
                panel = panelTransform.gameObject;
            else
            {
                // Tìm Canvas trong children
                Canvas canvas = GetComponentInChildren<Canvas>(true);
                if (canvas)
                    panel = canvas.gameObject;
            }
        }

        // Tìm headText nếu null
        if (!headText && panel)
        {
            headText = panel.GetComponentInChildren<Text>(true);
            // Nếu có nhiều Text, tìm theo tên
            if (headText == null)
            {
                Text[] texts = panel.GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    if (t.name.ToLower().Contains("head") || 
                        t.name.ToLower().Contains("title") ||
                        t.name.ToLower().Contains("salinity"))
                    {
                        headText = t;
                        break;
                    }
                }
                // Nếu vẫn null, lấy text đầu tiên
                if (headText == null && texts.Length > 0)
                    headText = texts[0];
            }
        }

        // Tìm bodyText nếu null
        if (!bodyText && panel)
        {
            Text[] texts = panel.GetComponentsInChildren<Text>(true);
            if (texts.Length > 1)
            {
                // Tìm text thứ 2 hoặc text có tên chứa "body", "desc", "info"
                foreach (var t in texts)
                {
                    if (t != headText && 
                        (t.name.ToLower().Contains("body") || 
                         t.name.ToLower().Contains("desc") ||
                         t.name.ToLower().Contains("info") ||
                         t.name.ToLower().Contains("description")))
                    {
                        bodyText = t;
                        break;
                    }
                }
                // Nếu vẫn null, lấy text thứ 2
                if (!bodyText && texts.Length > 1)
                    bodyText = texts[1];
            }
        }
    }

    private void Awake()
    {
        _growth = GetComponent<Thuan_23127_PlantGrowth>();
        _xr = GetComponent<XRSimpleInteractable>();
        
        if (_xr == null)
            _xr = GetComponentInChildren<XRSimpleInteractable>();
        
        // Nếu vẫn chưa có, tìm trong parent
        if (_xr == null)
            _xr = GetComponentInParent<XRSimpleInteractable>();

        FindPanelAndTexts();

        if (panel)
        {
            _cg = panel.GetComponent<CanvasGroup>();
            if (!_cg) _cg = panel.AddComponent<CanvasGroup>();

            _cg.blocksRaycasts = false;
            _cg.interactable   = false;

            foreach (var g in panel.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;

            // ẩn ban đầu
            SetVisible(false, instant:true);
        }
    }

    private void OnEnable()
    {
        // Tìm lại panel và texts nếu bị null (sau restart)
        if (panel == null || headText == null || bodyText == null)
            FindPanelAndTexts();

        // Đảm bảo lấy lại _xr nếu bị null (sau restart hoặc disable/enable)
        if (_xr == null)
        {
            _xr = GetComponent<XRSimpleInteractable>();
            if (_xr == null)
                _xr = GetComponentInChildren<XRSimpleInteractable>();
            if (_xr == null)
                _xr = GetComponentInParent<XRSimpleInteractable>();
        }

        // Đảm bảo _growth được tìm lại
        if (_growth == null)
            _growth = GetComponent<Thuan_23127_PlantGrowth>();

        // Đảm bảo _cg được setup lại
        if (panel != null && _cg == null)
        {
            _cg = panel.GetComponent<CanvasGroup>();
            if (!_cg) _cg = panel.AddComponent<CanvasGroup>();
            _cg.blocksRaycasts = false;
            _cg.interactable = false;
        }

        if (_growth != null)
        {
            _growth.OnSalinityChanged   += HandleSalinityChanged;
            _growth.OnHealthTextChanged += HandleDescriptionChanged;
        }

        if (_xr != null)
        {
            // Xóa listeners cũ trước khi thêm mới (tránh duplicate)
            _xr.hoverEntered.RemoveListener(OnHoverEntered);
            _xr.hoverExited.RemoveListener(OnHoverExited);
            
            // Thêm lại listeners
            _xr.hoverEntered.AddListener(OnHoverEntered);
            _xr.hoverExited.AddListener(OnHoverExited);
        }
    }

    private void OnDisable()
    {
        if (_growth != null)
        {
            _growth.OnSalinityChanged   -= HandleSalinityChanged;
            _growth.OnHealthTextChanged -= HandleDescriptionChanged;
        }

        if (_xr != null)
        {
            _xr.hoverEntered.RemoveListener(OnHoverEntered);
            _xr.hoverExited.RemoveListener(OnHoverExited);
        }
        StopAllCoroutines();
        SetVisible(false, instant:true);
    }

    private void OnHoverEntered(HoverEnterEventArgs _)
    {
        //  Kiểm tra game có đang active không
        if (!GameRulesProvider.GameActive) return;
        
        // Đảm bảo panel và growth tồn tại
        if (panel == null) FindPanelAndTexts();
        if (!_growth && panel != null) _growth = GetComponent<Thuan_23127_PlantGrowth>();
        
        if (!_growth || panel == null) return;
        
        _growth.UpdateSalinityEvent();
        SetVisible(true);
    }

    private void OnHoverExited(HoverExitEventArgs _)
    {
        SetVisible(false);
    }

    // ===== PC (Editor/Standalone) fallback =====
    void OnMouseEnter()
    {
        // Kiểm tra game có đang active không
        if (!GameRulesProvider.GameActive) return;
        
        // Đảm bảo panel và growth tồn tại
        if (panel == null) FindPanelAndTexts();
        if (!_growth && panel != null) _growth = GetComponent<Thuan_23127_PlantGrowth>();
        
        if (!_growth || panel == null) return;
        
        _growth.UpdateSalinityEvent();
        SetVisible(true);
    }

    private void OnMouseExit() => SetVisible(false);

    // ===== Nhận dữ liệu & cập nhật UI =====
    private void HandleSalinityChanged(float current, float threshold)
    {
        // Đảm bảo headText tồn tại
        if (!headText && panel)
            FindPanelAndTexts();
        
        if (!headText) return;

        // Lấy nhãn và đơn vị từ JSON 
        var salinityLabel = "Salinity";
        var unit = "‰"; // Mặc định là phần ngàn
        
        var jr = jsonReader ? jsonReader : Thuan_23127_GameManager.Instance?.jsonReader;
        var lang = jr ? jr.GetCurrentLangData() : null;
        if (lang?.interpretation?.fields != null)
        {
            salinityLabel = lang.interpretation.fields.salinity ?? salinityLabel;
            unit = lang.interpretation.fields.unit_ppt ?? unit; // Lấy đơn vị từ JSON
        }

        // Hiển thị với đơn vị phần ngàn (‰)
        headText.text = $"{salinityLabel}: {current:0.00} {unit} / {threshold:0.00} {unit}";
    }

    private void HandleDescriptionChanged(string desc)
    {
        // Đảm bảo bodyText tồn tại
        if (!bodyText && panel)
            FindPanelAndTexts();
        
        if (bodyText) bodyText.text = desc ?? string.Empty;
    }

    private void SetVisible(bool v, bool instant = false)
    {
        _shown = v;
        if (!panel || !_cg) return;

        StopAllCoroutines();

        if (instant)
        {
            panel.SetActive(v);
            _cg.alpha = v ? 1f : 0f;
            return;
        }

        if (v && !panel.activeSelf) panel.SetActive(true);
        StartCoroutine(FadeTo(v ? 1f : 0f, 0.15f, () =>
        {
            if (!v) panel.SetActive(false);
        }));
    }

    private IEnumerator FadeTo(float target, float dur, System.Action done)
    {
        float start = _cg.alpha;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            _cg.alpha = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        _cg.alpha = target;
        done?.Invoke();
    }
}
