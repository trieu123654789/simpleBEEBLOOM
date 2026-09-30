using UnityEngine;

[ExecuteAlways]
public class PlantBadLookApplier : MonoBehaviour
{
    [Header("Animator states")]
    public Animator animator;
    public string animBad  = "Tree_Bad";
    public string animGood = "Tree_Good";
    public int animatorLayer = 0;

    [Header("Bad look override")]
    public Color badTint = Color.black;
    [Range(0.1f, 1f)] public float badScale = 0.2f;
    [Tooltip("URP/HDRP: _BaseColor | Builtin/Standard: _Color")]
    public string colorProperty = "_BaseColor";
    public bool tryCommonColorProps = true;
    public bool includeInactiveChildren = true;

    // cache
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;
    private Vector3 _initialScale;
    private bool _badApplied;
    private string _lastState = "";
    private Coroutine _waitCo;
    private static readonly string[] _fallbackProps = { "_BaseColor", "_Color", "_Tint", "_TintColor" };

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        _renderers = GetComponentsInChildren<Renderer>(includeInactiveChildren);
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        _initialScale = transform.localScale;
    }

    void OnEnable()
    {
        // refresh khi bật lại
        _renderers = GetComponentsInChildren<Renderer>(includeInactiveChildren);
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        if (animator) _lastState = GetCurrentStateName();
    }

    void OnDisable()
    {
        if (_waitCo != null) { StopCoroutine(_waitCo); _waitCo = null; }
    }

    void Update()
    {
        if (!animator) return;

        var st = GetCurrentStateName();
        if (st == _lastState) return;
        _lastState = st;

        // Hễ vào state bad/good thì chờ clip xong rồi áp/clear
        if (st == animBad)
        {
            if (_waitCo != null) StopCoroutine(_waitCo);
            _waitCo = StartCoroutine(WaitClipFinishThen(ApplyBadLook));
        }
        else if (st == animGood)
        {
            if (_waitCo != null) StopCoroutine(_waitCo);
            _waitCo = StartCoroutine(WaitClipFinishThen(ClearBadLook));
        }
    }

    private string GetCurrentStateName()
    {
        var info = animator.GetCurrentAnimatorStateInfo(animatorLayer);
        // Lưu ý: IsName muốn "Layer.State". Ở đây mình so sánh theo tên ngắn (tên clip/state).
        // Ta trả ra hashToName tạm thời bằng cách so với các tên đã cấu hình.
        if (info.IsName(animBad))  return animBad;
        if (info.IsName(animGood)) return animGood;
        // fallback: để trống nếu không khớp
        return "";
    }

    private System.Collections.IEnumerator WaitClipFinishThen(System.Action action)
    {
        // chờ đến khi state đã ổn định (đúng state vừa vào)
        yield return null;

        // chờ hết một vòng của state hiện tại
        while (true)
        {
            var info = animator.GetCurrentAnimatorStateInfo(animatorLayer);
            // Nếu state đổi giữa chừng, coi như không làm gì
            if (!info.IsName(_lastState)) yield break;
            if (info.normalizedTime >= 1f) break;
            yield return null;
        }

        action?.Invoke();
        _waitCo = null;
    }

    // === Áp bad look cho tất cả Renderer con ===
    public void ApplyBadLook()
    {
        if (_badApplied) return;

        foreach (var r in _renderers)
        {
            if (!r || !r.sharedMaterial) continue;

            string prop = colorProperty;
            if (!r.sharedMaterial.HasProperty(prop) && tryCommonColorProps)
            {
                foreach (var p in _fallbackProps)
                    if (r.sharedMaterial.HasProperty(p)) { prop = p; break; }
            }
            if (!r.sharedMaterial.HasProperty(prop)) continue;

            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(prop, badTint);
            r.SetPropertyBlock(_mpb);
        }

        transform.localScale = _initialScale * badScale;
        _badApplied = true;
    }

    // === Trả lại màu & scale gốc ===
    public void ClearBadLook()
    {
        if (!_badApplied) return;

        foreach (var r in _renderers)
        {
            if (!r) continue;
            r.SetPropertyBlock(null); // clear MPB → về màu material gốc
        }

        transform.localScale = _initialScale;
        _badApplied = false;
    }

    // Tiện ích: gọi tay từ code khác/Inspector
    [ContextMenu("Apply Bad Look Now")]
    private void ContextApply() => ApplyBadLook();

    [ContextMenu("Clear Bad Look Now")]
    private void ContextClear() => ClearBadLook();
}
