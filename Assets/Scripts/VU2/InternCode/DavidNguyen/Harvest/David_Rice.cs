using UnityEngine;

// =============================================================================
// David_Rice - Individual rice stem that can be harvested.
// David_Rice - Cây lúa riêng lẻ có thể thu hoạch.
// Sickle hits rice → Score calculated → Rice hidden → Respawn after delay
// When affected by salinity (dry season) → Shrinks + tilts 45° to show stress
// Khi chịu mặn (mùa khô) → Thu nhỏ + nghiêng 45° để thể hiện cây yếu
//
// IDamageable: Enemy (saltwater) can damage rice directly.
//   HP > wiltThreshold  → healthy (upright)
//   HP ≤ wiltThreshold  → ApplyWilt()  (tilt 45°, still harvestable if season allows)
//   HP ≤ 0              → ApplyFall()  (tilt 90°, canHarvest = false)
// Rice is NOT destroyed on death — it stays wilted until respawned by FarmArea.
//
// IDamageable: Enemy (nước mặn) có thể gây damage trực tiếp lên lúa.
//   HP > wiltThreshold  → khỏe mạnh (đứng thẳng)
//   HP ≤ wiltThreshold  → ApplyWilt()  (nghiêng 45°, vẫn thu hoạch được nếu mùa cho phép)
//   HP ≤ 0              → ApplyFall()  (ngã 90°, canHarvest = false)
// Lúa KHÔNG bị Destroy khi chết — nằm chờ FarmArea respawn.
// =============================================================================
public class David_Rice : MonoBehaviour, IDamageable
{
    [Header("Rice Config / Cấu hình lúa")]
    
    [Tooltip("Điểm cơ bản khi thu hoạch")]
    public int baseScore = 10;
    
    [Tooltip("Icon cho lúa (hiển thị trong bảng điểm)")]
    public Sprite riceIcon;
    
    [Tooltip("Có thể thu hoạch không?")]
    public bool canHarvest = true;

    [Header("Zone Reference")]
    [Tooltip("Vùng FarmArea chứa lúa này")]
    public FarmArea ownerArea;

    [Header("Visual")]
    [Tooltip("GameObject hiển thị lúa (ẩn khi thu hoạch)")]
    public GameObject riceVisual;

    [Header("Audio")]
    public AudioClip harvestSound;

    // =========================================================================
    // SALINITY VISUAL EFFECTS / HIỆU ỨNG HÌNH ẢNH KHI CHỊU MẶN
    // =========================================================================

    // =========================================================================
    // EN: Health system — receives damage from Enemy (saltwater).
    // VI: Hệ thống máu — nhận sát thương từ Enemy (nước mặn).
    // EN: Rice is NOT destroyed on death; it just falls over (ApplyFall) and
    //     waits for FarmArea to respawn it next season.
    // VI: Lúa KHÔNG bị Destroy khi chết; chỉ ngã (ApplyFall) và chờ
    //     FarmArea respawn vào mùa tiếp theo.
    // =========================================================================
    [Header("Health / Máu (Enemy damage)")]
    [Tooltip(
        "EN: Max HP. Enemy deals 1 dmg per 10s (attackDamage=1, throttle×2, interval=5s).\n" +
        "    1 Enemy kills rice in: maxHealth × 10 seconds.\n" +
        "VI: Máu tối đa. 1 Enemy giết lúa sau: maxHealth × 10 giây.\n" +
        "    Ví dụ: maxHealth=10 → chết sau ~100 giây với 1 Enemy.")]
    public int maxHealth = 10;

    [Tooltip(
        "EN: HP at which rice starts wilting (visual only — still harvestable if season allows).\n" +
        "VI: Ngưỡng máu bắt đầu héo (chỉ visual — vẫn thu hoạch được nếu mùa cho phép).")]
    public int wiltThresholdHP = 5;

    [Header("Salinity Wilt Effect / Hiệu ứng héo khi mặn")]

    [Tooltip("Tỷ lệ thu nhỏ khi chịu mặn (0.6 = còn 60% kích thước gốc)\n" +
             "Scale ratio when affected by salinity")]
    [Range(0.3f, 1f)]
    public float wiltScale = 0.6f;

    [Tooltip("Góc nghiêng (độ) khi chịu mặn giai đoạn 2 (T2-T3)\n" +
             "Tilt angle (degrees) for Phase 2 salinity stress")]
    [Range(0f, 90f)]
    public float wiltTiltAngle = 45f;

    [Tooltip("Góc ngã (độ) khi mặn cao giai đoạn 3 (T4) — lúa ngã hẳn\n" +
             "Fall angle (degrees) for Phase 3 — rice falls flat")]
    [Range(0f, 90f)]
    public float fallTiltAngle = 90f;

    [Tooltip("Tốc độ chuyển đổi hiệu ứng (cao = nhanh hơn)\n" +
             "Transition speed for wilt effect (higher = faster)")]
    [Range(0.5f, 10f)]
    public float wiltTransitionSpeed = 2f;

    // =========================================================================
    // DAMAGE COLOR EFFECTS / HIỆU ỨNG MÀU KHI NHẬN DAMAGE
    // EN: Two-stage color change driven by HP and season phase:
    //   Stage 1 (HP ≤ 50%):  wiltDamageColor  — yellowing, still alive
    //   Stage 2 (Phase 3 or HP ≤ 0): fallDamageColor — brown/dead
    // VI: Đổi màu 2 giai đoạn theo HP và phase:
    //   Giai đoạn 1 (HP ≤ 50%): wiltDamageColor — vàng úa, còn sống
    //   Giai đoạn 2 (Phase 3 hoặc HP ≤ 0): fallDamageColor — nâu/chết
    // =========================================================================
    [Header("Damage Color Effects / Màu khi nhận damage")]
    [Tooltip(
        "EN: Color applied when HP drops to 50% or below (yellowing)\n" +
        "VI: Màu áp khi HP còn 50% hoặc ít hơn (vàng úa)")]
    public Color wiltDamageColor = new Color(0.85f, 0.75f, 0.10f, 1f); // vàng úa

    [Tooltip(
        "EN: Color applied when Phase 3 begins OR HP reaches 0 (brown/dead)\n" +
        "VI: Màu áp khi vào Giai đoạn 3 HOẶC HP = 0 (nâu/chết)")]
    public Color fallDamageColor = new Color(0.40f, 0.25f, 0.05f, 1f); // nâu chết

    [Tooltip(
        "EN: Shader property name for color. URP/HDRP: _BaseColor | Built-in: _Color\n" +
        "VI: Tên property shader cho màu.")]
    public string colorProperty = "_BaseColor";
    public bool tryCommonColorProps = true;

    private static readonly string[] _fallbackColorProps =
        { "_BaseColor", "_Color", "_Tint", "_TintColor" };

    // Renderer cache for color changes
    // Cache renderer để đổi màu
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;

    // EN: Tracks which color stage is currently applied to avoid redundant GPU calls.
    // VI: Theo dõi giai đoạn màu đang áp để tránh gọi GPU thừa.
    private enum ColorStage { None, Wilt, Fall }
    private ColorStage _colorStage = ColorStage.None;

    // State
    private bool _harvested = false;

    // EN: Current HP — instance variable (not static).
    // VI: Máu hiện tại — biến từng instance (không dùng static).
    private int _currentHealth;
    
    // Salinity state / Trạng thái mặn
    private bool _isWilted = false;
    private bool _isFallen = false;   // Phase 3: rice has fallen completely
    private Vector3 _initialScale;
    private Quaternion _initialRotation;
    private Vector3 _targetScale;
    private Quaternion _targetRotation;
    private bool _isTransitioning = false;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        // EN: Initialize HP. VI: Khởi tạo máu.
        _currentHealth = maxHealth;

        // Cache initial transform values before any modification
        // Lưu giá trị transform ban đầu trước khi thay đổi
        _initialScale = transform.localScale;
        _initialRotation = transform.localRotation;
        _targetScale = _initialScale;
        _targetRotation = _initialRotation;

        // EN: Cache all renderers and create shared MaterialPropertyBlock.
        // VI: Cache tất cả renderer và tạo MaterialPropertyBlock dùng chung.
        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();
    }

    private void Start()
    {
        // Auto-find FarmArea if not assigned
        if (ownerArea == null)
        {
            ownerArea = GetComponentInParent<FarmArea>();
        }

        // Auto-find visual if not assigned
        if (riceVisual == null)
        {
            riceVisual = gameObject;
        }

        // Check current season state on start
        // Kiểm tra trạng thái mùa hiện tại khi khởi động
        CheckCurrentSeason();
    }

    private void OnEnable()
    {
        // Subscribe to season change event
        // Đăng ký lắng nghe sự kiện đổi mùa
        GameRulesProvider.OnPhaseChanged += OnSeasonChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        // Hủy đăng ký để tránh rò rỉ bộ nhớ
        GameRulesProvider.OnPhaseChanged -= OnSeasonChanged;
    }

    private void Update()
    {
        // Smoothly transition scale and rotation toward target
        // Chuyển đổi mượt scale và rotation về giá trị mục tiêu
        if (_isTransitioning)
        {
            float speed = wiltTransitionSpeed * Time.deltaTime;

            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, speed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _targetRotation, speed);

            // Stop transitioning when close enough
            // Dừng chuyển đổi khi đã đủ gần
            bool scaleReached = Vector3.Distance(transform.localScale, _targetScale) < 0.01f;
            bool rotReached = Quaternion.Angle(transform.localRotation, _targetRotation) < 0.5f;

            if (scaleReached && rotReached)
            {
                transform.localScale = _targetScale;
                transform.localRotation = _targetRotation;
                _isTransitioning = false;
            }
        }
    }

    // =========================================================================
    // SALINITY RESPONSE / PHẢN ỨNG VỚI ĐỘ MẶN
    // =========================================================================

    /// <summary>
    /// Check current season and apply wilt/fall if needed.
    /// Kiểm tra mùa hiện tại và áp hiệu ứng héo/ngã nếu cần.
    /// </summary>
    private void CheckCurrentSeason()
    {
        float intrusion = GameRulesProvider.Saltwater_Intrusion;

        if (intrusion >= 1f)
        {
            // Phase 3 (T4): rice falls completely
            // Giai đoạn 3 (T4): lúa ngã hẳn
            ApplyFall();
        }
        else if (intrusion >= 0.5f)
        {
            // Phase 2 (T2-T3): rice tilts 45°
            // Giai đoạn 2 (T2-T3): lúa nghiêng 45°
            ApplyWilt();
        }
        else
        {
            // Phase 1 (T11-T1): rice stands normally
            // Giai đoạn 1 (T11-T1): lúa đứng bình thường
            ClearWilt();
        }
    }

    /// <summary>
    /// Called when season changes via OnPhaseChanged event.
    /// Dùng intrusion thay vì enum để robust với cả VU2_1 và VU2_2.
    /// </summary>
    private void OnSeasonChanged(SeasonPhase newPhase)
    {
        float intrusion = GameRulesProvider.Saltwater_Intrusion;

        if (intrusion >= 1f)
        {
            // Đỉnh mùa khô / mặn cao: lúa ngã hẳn, KHÔNG thu hoạch được
            ApplyFall();
            canHarvest = false;
        }
        else if (intrusion >= 0.5f)
        {
            // Chuyển tiếp: lúa nghiêng 45°, vẫn thu hoạch được
            ApplyWilt();
            canHarvest = true;
        }
        else
        {
            // Đỉnh mùa mưa / mặn thấp: lúa khỏe mạnh, đứng thẳng
            ClearWilt();
            canHarvest = true;
        }
    }

    /// <summary>
    /// Apply wilt effect: shrink + tilt 45° + wilt color (Phase 2, T2-T3 or HP ≤ 50%).
    /// Áp hiệu ứng héo: thu nhỏ + nghiêng 45° + màu vàng úa (Giai đoạn 2 hoặc HP ≤ 50%).
    /// </summary>
    public void ApplyWilt()
    {
        _isWilted = true;
        _isFallen = false;

        // Target: shrink to wiltScale and tilt by wiltTiltAngle (45°) on Z axis
        // Mục tiêu: thu nhỏ theo wiltScale và nghiêng wiltTiltAngle (45°) trên trục Z
        _targetScale = _initialScale * wiltScale;
        _targetRotation = _initialRotation * Quaternion.Euler(0f, 0f, wiltTiltAngle);
        _isTransitioning = true;

        // EN: Apply yellowing color — only escalate, never downgrade from Fall.
        // VI: Áp màu vàng úa — chỉ leo thang, không hạ từ Fall xuống Wilt.
        if (_colorStage == ColorStage.None)
            ApplyShaderColor(wiltDamageColor, ColorStage.Wilt);
    }

    /// <summary>
    /// Apply fall effect: rice falls completely + dead color (Phase 3, T4 or HP ≤ 0).
    /// Áp hiệu ứng ngã: lúa ngã hẳn + màu nâu chết (Giai đoạn 3, T4 hoặc HP ≤ 0).
    /// </summary>
    public void ApplyFall()
    {
        _isWilted = true;
        _isFallen = true;

        // Target: shrink more and tilt by fallTiltAngle (90°) — rice lies flat
        // Mục tiêu: thu nhỏ thêm và nghiêng fallTiltAngle (90°) — lúa nằm rạp
        _targetScale = _initialScale * wiltScale * 0.8f;
        _targetRotation = _initialRotation * Quaternion.Euler(0f, 0f, fallTiltAngle);
        _isTransitioning = true;

        // EN: Always apply dead color on fall regardless of previous stage.
        // VI: Luôn áp màu nâu chết khi ngã, bất kể giai đoạn màu trước đó.
        ApplyShaderColor(fallDamageColor, ColorStage.Fall);
    }

    /// <summary>
    /// Clear wilt effect: restore original scale, rotation, and color.
    /// Xóa hiệu ứng héo: khôi phục scale, rotation và màu gốc.
    /// </summary>
    public void ClearWilt()
    {
        if (!_isWilted && !_isFallen) return;
        _isWilted = false;
        _isFallen = false;

        // Restore original transform values
        // Khôi phục giá trị transform gốc
        _targetScale = _initialScale;
        _targetRotation = _initialRotation;
        _isTransitioning = true;

        // EN: Clear shader color — restore material's original color.
        // VI: Xóa màu shader — khôi phục màu gốc của material.
        ClearShaderColor();
    }

    /// <summary>
    /// Whether the rice is currently wilted from salinity.
    /// Lúa có đang bị héo do mặn không.
    /// </summary>
    public bool IsWilted => _isWilted;

    /// <summary>
    /// Whether the rice has completely fallen (Phase 3).
    /// Lúa đã ngã hẳn chưa (Giai đoạn 3).
    /// </summary>
    public bool IsFallen => _isFallen;

    // =========================================================================
    // HARVESTING
    // =========================================================================

    /// <summary>
    /// Checks if this rice can be harvested.
    /// Kiểm tra xem lúa này có thể thu hoạch không.
    /// </summary>
    public bool CanHarvest()
    {
        return canHarvest && !_harvested;
    }

    /// <summary>
    /// Harvests this rice, calculates score.
    /// Thu hoạch lúa này, tính điểm.
    /// </summary>
    public void Harvest()
    {
        if (!CanHarvest()) return;

        _harvested = true;
        canHarvest = false;

        // Calculate score using game rules
        int score = CalculateScore();
        
        // Track in SeasonalSummary
        var summary = Thuan_23127_SeasonalSummary.Instance;
        if (summary != null && riceIcon != null)
        {
            summary.TrackDirect("Rice", riceIcon, score);
        }

        // Add to game score
        if (GameRulesProvider.GameActive)
        {
            var gm = Thuan_23127_GameManager.Instance;
            if (gm != null)
            {
                gm.AddScore(score);
            }
        }

        // Play sound
        if (harvestSound != null)
        {
            AudioSource.PlayClipAtPoint(harvestSound, transform.position);
        }

        // Hide visual
        if (riceVisual != null)
        {
            riceVisual.SetActive(false);
        }
    }

    /// <summary>
    /// Calculates harvest score based on salinity phase (3 phases).
    /// Tính điểm thu hoạch dựa trên giai đoạn mặn (3 giai đoạn).
    /// Phase 1 (T11-T1, Intrusion=0.0): 60 pts
    /// Phase 2 (T2-T3,  Intrusion=0.5): 30 pts
    /// Phase 3 (T4,     Intrusion=1.0):  0 pts (blocked by canHarvest)
    /// </summary>
    private int CalculateScore()
    {
        float intrusion = GameRulesProvider.Saltwater_Intrusion;
        
        // Phase 1 (T11-T1): mùa mưa, nước ngọt dồi dào → 60 điểm
        if (intrusion < 0.1f) return 60;
        
        // Phase 2 (T2-T3): bắt đầu xâm nhập mặn → 30 điểm
        if (intrusion < 1f) return 30;
        
        // Phase 3 (T4): mặn quá cao → 0 điểm (không thể thu hoạch)
        return 0;
    }

    /// <summary>
    /// Respawns this rice for harvesting.
    /// Respawn lúa này để thu hoạch.
    /// Also restores wilt state based on current season.
    /// Đồng thời khôi phục trạng thái héo theo mùa hiện tại.
    /// </summary>
    public void Respawn()
    {
        _harvested = false;
        canHarvest = true;

        // EN: Reset HP so Enemy can damage again next season.
        // VI: Reset máu để Enemy có thể gây damage lại mùa tiếp theo.
        _currentHealth = maxHealth;

        // EN: Clear damage color so rice looks fresh again.
        // VI: Xóa màu damage để lúa trông tươi lại.
        ClearShaderColor();

        if (riceVisual != null)
        {
            riceVisual.SetActive(true);
        }

        // Re-check season after respawn
        // Kiểm tra lại mùa sau khi respawn
        CheckCurrentSeason();
    }

    // =========================================================================
    // IDamageable — Enemy (saltwater) attacks rice each tick.
    // IDamageable — Enemy (nước mặn) tấn công lúa mỗi tick.
    // =========================================================================

    /// <summary>
    /// EN: Current HP accessor.
    /// VI: Truy cập máu hiện tại.
    /// </summary>
    public int Health => _currentHealth;

    /// <summary>
    /// EN: Called by Enemy.DealDamage(). Maps HP to visual state:
    ///       HP ≤ wiltThresholdHP → ApplyWilt (tilt 45°)
    ///       HP ≤ 0               → ApplyFall (tilt 90°) + canHarvest = false
    ///     Rice stays in scene — FarmArea respawns it next season.
    /// VI: Được Enemy.DealDamage() gọi. Ánh xạ HP sang trạng thái hình ảnh:
    ///       HP ≤ wiltThresholdHP → ApplyWilt (nghiêng 45°)
    ///       HP ≤ 0               → ApplyFall (ngã 90°) + canHarvest = false
    ///     Lúa ở lại scene — FarmArea respawn vào mùa tiếp theo.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (IsDead()) return;

        _currentHealth -= damage;
        Debug.Log($"[David_Rice] '{gameObject.name}' TakeDamage({damage}) " +
                  $"→ hp={_currentHealth}/{maxHealth}");

        // EN: Stage 1 — HP ≤ 50%: yellow wilt color + tilt 45°.
        // VI: Giai đoạn 1 — HP ≤ 50%: màu vàng úa + nghiêng 45°.
        if (_currentHealth <= maxHealth / 2 && _colorStage == ColorStage.None)
            ApplyShaderColor(wiltDamageColor, ColorStage.Wilt);

        // EN: Transform wilt when HP crosses wilt threshold.
        // VI: Hiệu ứng héo transform khi HP vượt ngưỡng.
        if (_currentHealth <= wiltThresholdHP && !_isWilted)
            ApplyWilt();

        // EN: Stage 2 — HP ≤ 0: brown dead color + fall 90°.
        // VI: Giai đoạn 2 — HP ≤ 0: màu nâu chết + ngã 90°.
        if (_currentHealth <= 0)
        {
            Debug.Log($"[David_Rice] '{gameObject.name}' HP depleted → ApplyFall, canHarvest=false");
            ApplyFall();
            canHarvest = false;
        }
    }

    /// <summary>
    /// EN: Rice "dies" when HP ≤ 0. It falls over but is NOT destroyed
    ///     (FarmArea handles respawn each season).
    /// VI: Lúa "chết" khi HP ≤ 0. Nó ngã xuống nhưng KHÔNG bị Destroy
    ///     (FarmArea xử lý respawn mỗi mùa).
    /// </summary>
    public void Die()
    {
        // Guard chống đếm trùng trong cùng 1 "vòng đời" trước khi FarmArea respawn.
        // Guard against double-count within the same life before FarmArea respawns.
        if (_deathReported) return;
        _deathReported = true;

        // Thống kê: 1 cây lúa chết.
        // Statistics: a rice plant died.
        if (StatisticsManager.Instance != null)
            StatisticsManager.Instance.IncreaseFruitTreeDeathCount();

        // EN: Rice death = fallen state. No Destroy — FarmArea respawns it.
        // VI: Lúa chết = trạng thái ngã. Không Destroy — FarmArea respawn.
        ApplyFall();
        canHarvest = false;
    }

    // Cờ reset khi FarmArea respawn (nếu cần có thể expose qua method ResetDeathFlag).
    private bool _deathReported = false;
    public void ResetDeathFlag() { _deathReported = false; }

    /// <summary>
    /// EN: Returns true when HP ≤ 0.
    /// VI: Trả về true khi máu ≤ 0.
    /// </summary>
    public bool IsDead() => _currentHealth <= 0;

    // =========================================================================
    // COLOR HELPERS / HÀM HỖ TRỢ ĐỔI MÀU
    // =========================================================================

    /// <summary>
    /// EN: Apply a shader color to all renderers via MaterialPropertyBlock.
    ///     Uses _BaseColor (URP) with fallback to _Color (Built-in).
    /// VI: Áp màu shader lên tất cả renderer qua MaterialPropertyBlock.
    ///     Dùng _BaseColor (URP) với fallback _Color (Built-in).
    /// </summary>
    private void ApplyShaderColor(Color color, ColorStage stage)
    {
        if (_renderers == null || _mpb == null) return;
        _colorStage = stage;

        foreach (var r in _renderers)
        {
            if (r == null || r.sharedMaterial == null) continue;
            string prop = FindColorProperty(r);
            if (prop == null) continue;

            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(prop, color);
            r.SetPropertyBlock(_mpb);
        }
    }

    /// <summary>
    /// EN: Remove the color override — renderer reverts to material's original color.
    /// VI: Xóa override màu — renderer trở về màu gốc của material.
    /// </summary>
    private void ClearShaderColor()
    {
        if (_renderers == null) return;
        _colorStage = ColorStage.None;

        foreach (var r in _renderers)
        {
            if (r == null) continue;
            r.SetPropertyBlock(null);
        }
    }

    /// <summary>
    /// EN: Find the correct color shader property for a renderer's material.
    /// VI: Tìm property màu shader phù hợp cho material của renderer.
    /// </summary>
    private string FindColorProperty(Renderer r)
    {
        if (r.sharedMaterial.HasProperty(colorProperty))
            return colorProperty;

        if (tryCommonColorProps)
        {
            foreach (var prop in _fallbackColorProps)
            {
                if (r.sharedMaterial.HasProperty(prop))
                    return prop;
            }
        }
        return null;
    }

    // =========================================================================
    // Context menu for testing in Editor
    // Menu ngữ cảnh để test trong Editor
    // =========================================================================
    [ContextMenu("Test: Apply Wilt")]
    private void TestApplyWilt() => ApplyWilt();

    [ContextMenu("Test: Clear Wilt")]
    private void TestClearWilt() => ClearWilt();

    [ContextMenu("Test: Take 1 Damage")]
    private void TestTakeDamage() => TakeDamage(1);

    [ContextMenu("Test: Apply Wilt Color")]
    private void TestWiltColor() => ApplyShaderColor(wiltDamageColor, ColorStage.Wilt);

    [ContextMenu("Test: Apply Fall Color")]
    private void TestFallColor() => ApplyShaderColor(fallDamageColor, ColorStage.Fall);

    [ContextMenu("Test: Clear Color")]
    private void TestClearColor() => ClearShaderColor();
}
