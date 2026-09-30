using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script gắn vào CÂY (không phải quả) để điều khiển hiệu ứng héo khi mùa khô
/// và nhận sát thương từ Enemy (nước mặn) giống Tree.cs.
///
/// EN: Attached to the tree root. Responds to season phases AND receives
///     damage from saltwater Enemy entities (implements IDamageable).
/// VI: Gắn vào gốc cây. Phản ứng theo mùa VÀ nhận sát thương từ Enemy nước mặn.
///
/// Health thresholds (tương đồng Tree.cs):
///   > wiltThreshold  → Tree_Good (khỏe)
///   ≤ wiltThreshold  → Tree_Bad  (héo / ApplyWilt)
///   ≤ 0              → Die()     (chết, xóa khỏi scene + thông báo GAMA)
/// </summary>
public class David_TreeWiltController : MonoBehaviour, IDamageable
{
    // =========================================================================
    // EN: Health system — mirrors Tree.cs so Enemy can damage David trees.
    // VI: Hệ thống máu — nhất quán với Tree.cs để Enemy có thể gây damage.
    // =========================================================================
    [Header("Health / Máu")]
    [Tooltip("EN: Starting HP. VI: Máu ban đầu.\n" +
             "EN: Enemy deals 1 dmg per 10s (attackDamage=1, throttle×2, interval=5s).\n" +
             "    1 Enemy kills tree in: maxHealth × 10 seconds.\n" +
             "VI: Enemy gây 1 dmg mỗi 10s → 1 Enemy giết cây sau: maxHealth × 10 giây.\n" +
             "    Ví dụ: maxHealth=20 → chết sau ~3 phút với 1 Enemy.")]
    public int maxHealth = 20;

    [Tooltip("EN: HP threshold below which tree enters wilt state.\n" +
             "VI: Ngưỡng máu để cây chuyển sang trạng thái héo.")]
    public int wiltThreshold = 10;

    [Header("Animator ")]
    [Tooltip("Kéo Animator của cây vào đây, hoặc để trống nếu dùng visual only")]
    public Animator treeAnimator;
    public string animGood = "Tree_Good";
    public string animBad = "Tree_Bad";
    public int animatorLayer = 0;
    
    [Header("Visual Effects (không cần Animator)")]
    [Tooltip("Bật nếu muốn đổi màu + scale thay vì dùng animation")]
    public bool useVisualEffects = true;
    
    [Tooltip("Màu khi cây héo")]
    public Color wiltColor = new Color(0.5f, 0.35f, 0.15f, 1f);  // Nâu héo
    
    [Tooltip("Tỷ lệ scale khi héo (0.8 = thu nhỏ 80%)")]
    [Range(0.5f, 1f)] 
    public float wiltScale = 0.85f;
    
    [Header("Shader Color Property")]
    [Tooltip("URP/HDRP: _BaseColor | Built-in: _Color")]
    public string colorProperty = "_BaseColor";
    public bool tryCommonColorProps = true;
    
    // Cache
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;
    private Vector3 _initialScale;
    private bool _isWilted = false;
    private Dictionary<Renderer, Color> _originalColors = new Dictionary<Renderer, Color>();

    // EN: Current HP — instance variable (not static, unlike Tree.cs bug).
    // VI: Máu hiện tại — biến từng instance (không dùng static như lỗi trong Tree.cs).
    private int _currentHealth;
    
    private static readonly string[] _fallbackProps = { "_BaseColor", "_Color", "_Tint", "_TintColor" };
    
    private void Awake()
    {
        // EN: Initialize HP. VI: Khởi tạo máu.
        _currentHealth = maxHealth;

        // Tìm animator nếu chưa gán
        if (treeAnimator == null)
            treeAnimator = GetComponentInChildren<Animator>();
        
        // Cache renderers
        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();
        _initialScale = transform.localScale;
        
        // Lưu màu gốc
        CacheOriginalColors();
    }
    
    private void OnEnable()
    {
        // Đăng ký lắng nghe event đổi mùa
        GameRulesProvider.OnPhaseChanged += OnSeasonChanged;
        
        // Áp trạng thái hiện tại
        CheckCurrentSeason();
    }
    
    private void OnDisable()
    {
        GameRulesProvider.OnPhaseChanged -= OnSeasonChanged;
    }
    
    /// <summary>
    /// Kiểm tra mùa hiện tại khi script được enable
    /// </summary>
    private void CheckCurrentSeason()
    {
        bool isDry = GameRulesProvider.Saltwater_Intrusion >= 1f;
        
        if (isDry && !_isWilted)
            ApplyWilt();
        else if (!isDry && _isWilted)
            ClearWilt();
    }
    
    /// <summary>
    /// Được gọi khi mùa thay đổi — dùng intrusion để robust với cả VU2_1 và VU2_2 (enum không đồng nhất giữa hai level).
    /// </summary>
    private void OnSeasonChanged(SeasonPhase newPhase)
    {
        // High salinity → wilt tree. Độ mặn cao → cây héo.
        bool isDry = GameRulesProvider.Saltwater_Intrusion >= 1f;

        if (isDry && !_isWilted)
        {
            ApplyWilt();
        }
        else if (!isDry && _isWilted)
        {
            ClearWilt();
        }
    }
    
    /// <summary>
    /// Áp hiệu ứng héo cho cây
    /// </summary>
    public void ApplyWilt()
    {
        if (_isWilted) return;
        _isWilted = true;
        
        if (treeAnimator != null && !string.IsNullOrEmpty(animBad))
        {
            treeAnimator.Play(animBad, animatorLayer, 0f);
        }
        
        if (useVisualEffects)
        {
            ApplyWiltVisuals();
        }
    }
    
    /// <summary>
    /// Khôi phục cây về trạng thái tươi
    /// </summary>
    public void ClearWilt()
    {
        if (!_isWilted) return;
        _isWilted = false;
        
        if (treeAnimator != null && !string.IsNullOrEmpty(animGood))
        {
            treeAnimator.Play(animGood, animatorLayer, 0f);
        }
        if (useVisualEffects)
        {
            ClearWiltVisuals();
        }
    }
    
    /// <summary>
    /// Lưu màu gốc của tất cả renderers
    /// </summary>
    private void CacheOriginalColors()
    {
        _originalColors.Clear();
        
        foreach (var r in _renderers)
        {
            if (r == null || r.sharedMaterial == null) continue;
            
            string prop = GetColorProperty(r);
            if (string.IsNullOrEmpty(prop)) continue;
            
            Color original = r.sharedMaterial.GetColor(prop);
            _originalColors[r] = original;
        }
    }
    
    /// <summary>
    /// Tìm property màu phù hợp cho material
    /// </summary>
    private string GetColorProperty(Renderer r)
    {
        if (r.sharedMaterial.HasProperty(colorProperty))
            return colorProperty;
        
        if (tryCommonColorProps)
        {
            foreach (var prop in _fallbackProps)
            {
                if (r.sharedMaterial.HasProperty(prop))
                    return prop;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Áp màu héo + thu nhỏ
    /// </summary>
    private void ApplyWiltVisuals()
    {
        foreach (var r in _renderers)
        {
            if (r == null || r.sharedMaterial == null) continue;
            
            string prop = GetColorProperty(r);
            if (string.IsNullOrEmpty(prop)) continue;
            
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(prop, wiltColor);
            r.SetPropertyBlock(_mpb);
        }
        
        // Thu nhỏ cây
        transform.localScale = _initialScale * wiltScale;
    }
    
    /// <summary>
    /// Khôi phục màu gốc + scale gốc
    /// </summary>
    private void ClearWiltVisuals()
    {
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            
            // Clear property block → về màu material gốc
            r.SetPropertyBlock(null);
        }
        
        // Khôi phục scale
        transform.localScale = _initialScale;
    }
    
    /// <summary>
    /// Kiểm tra cây có đang héo không
    /// </summary>
    public bool IsWilted => _isWilted;

    // =========================================================================
    // IDamageable — lets Enemy (saltwater) attack this tree like Tree.cs.
    // IDamageable — cho phép Enemy (nước mặn) tấn công cây này như Tree.cs.
    // =========================================================================

    /// <summary>
    /// EN: Current HP accessor.
    /// VI: Truy cập máu hiện tại.
    /// </summary>
    public int Health => _currentHealth;

    /// <summary>
    /// EN: Called by Enemy.DealDamage() each attack tick.
    ///     Health thresholds mirror Tree.cs logic:
    ///       ≤ wiltThreshold → ApplyWilt (Tree_Bad)
    ///       ≤ 0             → Die()
    /// VI: Được Enemy.DealDamage() gọi mỗi lần tấn công.
    ///     Ngưỡng máu nhất quán với Tree.cs:
    ///       ≤ wiltThreshold → ApplyWilt (Tree_Bad)
    ///       ≤ 0             → Die()
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (IsDead()) return;

        _currentHealth -= damage;
       // Debug.Log($"[David_TreeWiltController] '{gameObject.name}' TakeDamage({damage}) " + $"→ hp={_currentHealth}/{maxHealth}");

        // EN: Enter wilt state when health crosses the threshold.
        // VI: Chuyển sang héo khi máu vượt ngưỡng.
        if (_currentHealth <= wiltThreshold && !_isWilted)
            ApplyWilt();

        // EN: Die when health depleted.
        // VI: Chết khi hết máu.
        if (_currentHealth <= 0)
            Die();
    }

    /// <summary>
    /// EN: Destroy this tree, remove HUD marker, and notify GAMA.
    ///     Destroy() is guaranteed via try-finally so an exception in
    ///     HUD/GAMA calls never leaves a ghost tree in the scene.
    /// VI: Hủy cây, xóa marker HUD và thông báo GAMA.
    ///     Destroy() được đảm bảo qua try-finally để exception trong
    ///     HUD/GAMA không để lại cây ma trong scene.
    /// </summary>
    public void Die()
    {
        Debug.Log($"[David_TreeWiltController] Die() called on '{gameObject.name}' " +
                  $"(instanceID={gameObject.GetInstanceID()}, hp={_currentHealth})");

        // Thống kê: 1 cây ăn quả (durian wilt controller) chết.
        // Statistics: a fruit tree died.
        if (StatisticsManager.Instance != null)
            StatisticsManager.Instance.IncreaseFruitTreeDeathCount();

        try
        {
            // EN: Remove HUD marker.
            // VI: Xóa marker HUD.
            if (GameUI.Instance != null)
                GameUI.Instance.DeletePlayer(gameObject);

            // EN: Notify GAMA to remove this tree from simulation.
            // VI: Thông báo GAMA xóa cây này khỏi simulation.
            if (ConnectionManager.Instance != null)
            {
                var args = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "idP", ConnectionManager.Instance.GetConnectionId() },
                    { "idT", gameObject.GetInstanceID().ToString() }
                };
                ConnectionManager.Instance.SendExecutableAsk("delete_tree", args);
            }

            // EN: Count dead tree in UI (same as Tree.Die()).
            // VI: Đếm cây chết trong UI (giống Tree.Die()).
            if (GameUI.Instance != null)
                GameUI.Instance.CountDeadTree();
        }
        catch (System.Exception ex)
        {
            // EN: Log but do NOT rethrow — Destroy must always run.
            // VI: Log nhưng KHÔNG throw lại — Destroy phải luôn chạy.
            Debug.LogWarning($"[David_TreeWiltController] Exception in Die() side-effects " +
                             $"(tree will still be destroyed): {ex.Message}");
        }
        finally
        {
            // EN: Always destroy, regardless of what happened above.
            // VI: Luôn hủy, bất kể lỗi gì xảy ra ở trên.
            Debug.Log($"[David_TreeWiltController] Destroying '{gameObject.name}'");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// EN: Returns true when HP ≤ 0.
    /// VI: Trả về true khi máu ≤ 0.
    /// </summary>
    public bool IsDead() => _currentHealth <= 0;
    
    // Context menu để test trong Editor
    [ContextMenu("Test: Apply Wilt")]
    private void TestApplyWilt() => ApplyWilt();
    
    [ContextMenu("Test: Clear Wilt")]
    private void TestClearWilt() => ClearWilt();
}
