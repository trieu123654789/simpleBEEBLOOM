using System;
using UnityEngine;

// =============================================================================
// TidalPhase - Represents the 4 positions of the moon in its orbit.
// TidalPhase - Đại diện cho 4 vị trí của Mặt trăng trên quỹ đạo.
// 
//   Position 1 (NewMoon)      → Triều cường (Spring Tide)
//   Position 2 (FirstQuarter) → Triều kém   (Neap Tide)
//   Position 3 (FullMoon)     → Triều cường (Spring Tide)
//   Position 4 (LastQuarter)  → Triều kém   (Neap Tide)
// =============================================================================
public enum TidalPhase
{
    NewMoon = 0,       // Vị trí 1: Không trăng    → Triều cường
    FirstQuarter = 1,  // Vị trí 2: Trăng khuyết   → Triều kém
    FullMoon = 2,      // Vị trí 3: Trăng tròn     → Triều cường
    LastQuarter = 3    // Vị trí 4: Trăng khuyết   → Triều kém
}

// =============================================================================
// TidalState - Current tide state derived from moon phase.
// TidalState - Trạng thái triều hiện tại, suy ra từ pha Mặt trăng.
// =============================================================================
public enum TidalState
{
    SpringTide,  // Triều cường: nước dâng cao, con mặn nhiều + nhanh
    NeapTide     // Triều kém:   nước hạ thấp, con mặn ít + rút ra
}

// =============================================================================
// TidalClockManager - Central tidal system that drives all tidal mechanics.
// TidalClockManager - Hệ thống triều trung tâm điều khiển tất cả cơ chế triều.
//
// DESIGN:
// - The moon orbits continuously over the game duration
// - Each game month contains a configurable number of tidal cycles
// - Spring Tides (vị trí 1, 3) replace "Mùa Khô" behavior:
//     → Water rises, enemies spawn faster & move faster
// - Neap Tides (vị trí 2, 4) replace "Mùa Mưa" behavior:
//     → Water recedes, enemies retreat/slow down, exposed mudflats
//
// THIẾT KẾ:
// - Mặt trăng quay liên tục trong suốt thời gian game
// - Mỗi tháng chứa số chu kỳ triều có thể cấu hình
// - Triều cường (vị trí 1, 3) thay thế hành vi "Mùa Khô":
//     → Nước dâng, con mặn sinh nhiều hơn & di chuyển nhanh hơn
// - Triều kém (vị trí 2, 4) thay thế hành vi "Mùa Mưa":
//     → Nước rút, con mặn lui ra/chậm lại, lộ bãi bồi
// =============================================================================
public class TidalClockManager : MonoBehaviour
{
    // =========================================================================
    // SINGLETON
    // =========================================================================
    public static TidalClockManager Instance { get; private set; }

    // =========================================================================
    // CONFIGURATION
    // CẤU HÌNH
    // =========================================================================
    [Header("Tidal Cycle Configuration / Cấu hình chu kỳ triều")]
    
    [Tooltip("Duration of one full tidal cycle (moon orbit) in seconds.\n"
           + "Thời gian 1 chu kỳ triều đầy đủ (1 vòng trăng) tính bằng giây.\n"
           + "Default: 120s = 2 phút mỗi chu kỳ.")]
    public float tidalCycleDuration = 120f;

    [Tooltip("Number of tidal cycles per game month.\n"
           + "Số chu kỳ triều mỗi tháng game.\n"
           + "Default: 2 (mỗi tháng có 2 lần triều cường + 2 lần triều kém).")]
    public int cyclesPerMonth = 2;

    [Header("Tidal Intensity / Cường độ triều")]
    
    [Tooltip("Enemy speed multiplier during Spring Tide.\n"
           + "Hệ số tốc độ con mặn khi triều cường.")]
    public float springTideEnemySpeedMultiplier = 1.8f;

    [Tooltip("Enemy speed multiplier during Neap Tide.\n"
           + "Hệ số tốc độ con mặn khi triều kém.")]
    public float neapTideEnemySpeedMultiplier = 0.5f;

    [Tooltip("Enemy spawn rate multiplier during Spring Tide.\n"
           + "Hệ số tốc độ sinh con mặn khi triều cường.")]
    public float springTideSpawnMultiplier = 2.0f;

    [Tooltip("Enemy spawn rate multiplier during Neap Tide.\n"
           + "Hệ số tốc độ sinh con mặn khi triều kém.")]
    public float neapTideSpawnMultiplier = 0.3f;

    [Tooltip("Water level offset during Spring Tide (added to base).\n"
           + "Mực nước bổ sung khi triều cường (cộng vào mức cơ sở).")]
    public float springTideWaterOffset = 20f;

    [Tooltip("Water level offset during Neap Tide (subtracted from base).\n"
           + "Mực nước giảm khi triều kém (trừ khỏi mức cơ sở).")]
    public float neapTideWaterOffset = -15f;

    [Header("Season Salinity Boost / Tăng cường độ mặn theo mùa")]
    
    [Tooltip("Extra salinity multiplier applied to Spring Tide during dry season.\n"
           + "Hệ số nhân thêm cho triều cường trong mùa khô.\n"
           + "E.g., 1.5 means Spring Tide is 50% stronger during dry season.")]
    public float drySeasonSpringBoost = 1.5f;

    // =========================================================================
    // RUNTIME STATE (read-only from outside)
    // TRẠNG THÁI RUNTIME (chỉ đọc từ bên ngoài)
    // =========================================================================

    /// <summary>
    /// Normalized moon angle: 0.0 → 1.0 = full orbit.
    /// Góc Mặt trăng chuẩn hóa: 0.0 → 1.0 = 1 vòng quay.
    /// 0.00 = Position 1 (New Moon / Không trăng)
    /// 0.25 = Position 2 (First Quarter / Trăng khuyết)
    /// 0.50 = Position 3 (Full Moon / Trăng tròn)
    /// 0.75 = Position 4 (Last Quarter / Trăng khuyết)
    /// </summary>
    public float MoonPhaseNormalized { get; private set; }

    /// <summary>
    /// Moon angle in degrees: 0° → 360°.
    /// Góc Mặt trăng theo độ: 0° → 360°.
    /// </summary>
    public float MoonAngleDegrees { get; private set; }

    /// <summary>
    /// Current discrete moon phase (4 positions).
    /// Pha Mặt trăng rời rạc hiện tại (4 vị trí).
    /// </summary>
    public TidalPhase CurrentPhase { get; private set; }

    /// <summary>
    /// Current tidal state: Spring or Neap.
    /// Trạng thái triều hiện tại: Cường hoặc Kém.
    /// </summary>
    public TidalState CurrentTide { get; private set; }

    /// <summary>
    /// Tidal intensity: 0.0 (weakest, at Neap positions 2/4) → 1.0 (strongest, at Spring positions 1/3).
    /// Uses a smooth cosine curve: cos(2 * moonAngle) mapped to 0..1.
    /// Cường độ triều: 0.0 (yếu nhất, triều kém) → 1.0 (mạnh nhất, triều cường).
    /// Dùng đường cong cosine mượt: cos(2 × góc trăng) ánh xạ về 0..1.
    /// </summary>
    public float TidalIntensity { get; private set; }

    /// <summary>
    /// Current enemy speed multiplier based on tidal intensity.
    /// Hệ số tốc độ con mặn hiện tại dựa trên cường độ triều.
    /// </summary>
    public float CurrentEnemySpeedMultiplier { get; private set; }

    /// <summary>
    /// Current enemy spawn multiplier based on tidal intensity.
    /// Hệ số sinh con mặn hiện tại dựa trên cường độ triều.
    /// </summary>
    public float CurrentSpawnMultiplier { get; private set; }

    /// <summary>
    /// Current water level offset from tidal effect.
    /// Mực nước chênh lệch do triều.
    /// </summary>
    public float CurrentWaterLevelOffset { get; private set; }

    // =========================================================================
    // EVENTS
    // SỰ KIỆN
    // =========================================================================
    
    /// <summary>
    /// Fired when discrete tidal phase changes (4 times per cycle).
    /// Bắn khi pha triều rời rạc thay đổi (4 lần/chu kỳ).
    /// </summary>
    public static event Action<TidalPhase> OnTidalPhaseChanged;

    /// <summary>
    /// Fired every frame with current intensity (for smooth animations).
    /// Bắn mỗi frame với cường độ hiện tại (cho animation mượt).
    /// </summary>
    public static event Action<float> OnTidalIntensityUpdated;

    /// <summary>
    /// Fired when tide state changes between Spring and Neap.
    /// Bắn khi trạng thái triều chuyển giữa Cường và Kém.
    /// </summary>
    public static event Action<TidalState> OnTidalStateChanged;

    // =========================================================================
    // INTERNAL
    // NỘI BỘ
    // =========================================================================
    private float _elapsedTime;
    private bool _isRunning;
    private TidalPhase _lastPhase = (TidalPhase)(-1);
    private TidalState _lastTideState = (TidalState)(-1);
    private float _effectiveCycleDuration;

    /// <summary>
    /// Whether the tidal clock is currently running.
    /// Đồng hồ triều có đang chạy hay không.
    /// </summary>
    public bool IsRunning => _isRunning;

    // =========================================================================
    // LIFECYCLE
    // VÒNG ĐỜI
    // =========================================================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        // Subscribe to season changes to adjust tidal behavior.
        // Đăng ký lắng nghe thay đổi mùa để điều chỉnh hành vi triều.
        GameRulesProvider.OnPhaseChanged += OnSeasonPhaseChanged;
    }

    private void OnDisable()
    {
        GameRulesProvider.OnPhaseChanged -= OnSeasonPhaseChanged;
    }

    private void Start()
    {
        _isRunning = false;
        _elapsedTime = 0f;

        // Initialize derived values to sensible defaults so EnemySpawner
        // doesn't multiply by 0 before StartTidalClock() is called.
        // Khởi tạo giá trị mặc định hợp lý để EnemySpawner
        // không bị nhân với 0 trước khi StartTidalClock() được gọi.
        CurrentEnemySpeedMultiplier = 1f;
        CurrentSpawnMultiplier = 1f;
        CurrentWaterLevelOffset = 0f;
        TidalIntensity = 0f;

        RecalculateCycleDuration();

        // Auto-start as fallback: nếu sau 5s chưa có hệ thống nào gọi
        // StartTidalClock(), tự khởi động.
        Invoke(nameof(AutoStartFallback), 5f);
    }

    /// <summary>
    /// Fallback: auto-start if no other system started the clock.
    /// Dự phòng: tự khởi động nếu chưa hệ thống nào gọi.
    /// </summary>
    private void AutoStartFallback()
    {
        if (!_isRunning)
        {
            Debug.LogWarning("[TidalClock] Auto-starting (no system called StartTidalClock after 5s)");
            StartTidalClock();
        }
    }

    // =========================================================================
    // PUBLIC API
    // API CÔNG KHAI
    // =========================================================================
    
    /// <summary>
    /// Start the tidal clock. Call this when game starts.
    /// Bắt đầu đồng hồ triều. Gọi khi game bắt đầu.
    /// </summary>
    public void StartTidalClock()
    {
        _elapsedTime = 0f;
        _isRunning = true;
        _lastPhase = (TidalPhase)(-1);
        _lastTideState = (TidalState)(-1);
        RecalculateCycleDuration();
        Debug.Log($"[TidalClock] Started — cycleDuration={_effectiveCycleDuration:F1}s, " +
                  $"cyclesPerMonth={cyclesPerMonth}");
    }

    /// <summary>
    /// Stop the tidal clock. Call this when game ends.
    /// Dừng đồng hồ triều. Gọi khi game kết thúc.
    /// </summary>
    public void StopTidalClock()
    {
        _isRunning = false;
    }

    /// <summary>
    /// Reset and restart the tidal clock.
    /// Reset và khởi động lại đồng hồ triều.
    /// </summary>
    public void ResetTidalClock()
    {
        StopTidalClock();
        StartTidalClock();
    }

    // =========================================================================
    // UPDATE LOOP
    // VÒNG LẶP CẬP NHẬT
    // =========================================================================
    private void Update()
    {
        if (!_isRunning) return;

        _elapsedTime += Time.deltaTime;

        // Calculate moon position in current cycle.
        // Tính vị trí Mặt trăng trong chu kỳ hiện tại.
        float cycleTime = _elapsedTime % _effectiveCycleDuration;
        MoonPhaseNormalized = cycleTime / _effectiveCycleDuration;
        MoonAngleDegrees = MoonPhaseNormalized * 360f;

        // =====================================================================
        // Determine discrete phase (4 quadrants).
        // Xác định pha rời rạc (4 phần tư).
        // =====================================================================
        if (MoonPhaseNormalized < 0.125f || MoonPhaseNormalized >= 0.875f)
            CurrentPhase = TidalPhase.NewMoon;        // Position 1
        else if (MoonPhaseNormalized < 0.375f)
            CurrentPhase = TidalPhase.FirstQuarter;   // Position 2
        else if (MoonPhaseNormalized < 0.625f)
            CurrentPhase = TidalPhase.FullMoon;        // Position 3
        else
            CurrentPhase = TidalPhase.LastQuarter;     // Position 4

        // Determine tide state.
        // Xác định trạng thái triều.
        CurrentTide = (CurrentPhase == TidalPhase.NewMoon || CurrentPhase == TidalPhase.FullMoon)
            ? TidalState.SpringTide
            : TidalState.NeapTide;

        // =====================================================================
        // Calculate smooth tidal intensity using cosine.
        // Tính cường độ triều mượt bằng cosine.
        //
        // cos(2 * 2π * phase) peaks at phase=0 (NewMoon) and phase=0.5 (FullMoon)
        // and dips at phase=0.25 (FirstQuarter) and phase=0.75 (LastQuarter)
        // Map from [-1, 1] to [0, 1]
        // =====================================================================
        float cosValue = Mathf.Cos(2f * 2f * Mathf.PI * MoonPhaseNormalized);
        TidalIntensity = (cosValue + 1f) * 0.5f; // 0..1

        // =====================================================================
        // Apply season boost: dry season amplifies spring tide effects.
        // Áp dụng tăng cường theo mùa: mùa khô khuếch đại hiệu ứng triều cường.
        // =====================================================================
        float seasonBoost = GetSeasonBoost();

        // =====================================================================
        // Calculate derived values.
        // Tính các giá trị dẫn xuất.
        // =====================================================================
        CurrentEnemySpeedMultiplier = Mathf.Lerp(
            neapTideEnemySpeedMultiplier,
            springTideEnemySpeedMultiplier * seasonBoost,
            TidalIntensity
        );

        CurrentSpawnMultiplier = Mathf.Lerp(
            neapTideSpawnMultiplier,
            springTideSpawnMultiplier * seasonBoost,
            TidalIntensity
        );

        CurrentWaterLevelOffset = Mathf.Lerp(
            neapTideWaterOffset,
            springTideWaterOffset * seasonBoost,
            TidalIntensity
        );

        // =====================================================================
        // Fire events.
        // Bắn sự kiện.
        // =====================================================================
        if (CurrentPhase != _lastPhase)
        {
            _lastPhase = CurrentPhase;
            OnTidalPhaseChanged?.Invoke(CurrentPhase);
            Debug.Log($"[TidalClock] Phase → {CurrentPhase} | Tide → {CurrentTide} | " +
                      $"Intensity={TidalIntensity:F2} | MoonAngle={MoonAngleDegrees:F0}°");
        }

        if (CurrentTide != _lastTideState)
        {
            _lastTideState = CurrentTide;
            OnTidalStateChanged?.Invoke(CurrentTide);
        }

        OnTidalIntensityUpdated?.Invoke(TidalIntensity);
    }

    // =========================================================================
    // HELPERS
    // HỖ TRỢ
    // =========================================================================
    
    /// <summary>
    /// Called when the season phase changes (Rainy1 / Dry / Rainy2).
    /// Được gọi khi mùa thay đổi (Mưa1 / Khô / Mưa2).
    /// Logs the transition so designers can verify tidal-season interaction.
    /// </summary>
    private void OnSeasonPhaseChanged(SeasonPhase newPhase)
    {
        Debug.Log($"[TidalClock] Season changed → {newPhase} | " +
                  $"SeasonBoost will be {GetSeasonBoost():F2}");
    }

    /// <summary>
    /// Recalculate effective cycle duration based on cyclesPerMonth and tidalCycleDuration.
    /// Tính lại thời lượng chu kỳ hiệu quả.
    /// </summary>
    private void RecalculateCycleDuration()
    {
        _effectiveCycleDuration = tidalCycleDuration / Mathf.Max(1, cyclesPerMonth);
    }

    /// <summary>
    /// Get season-based boost factor for tidal effects.
    /// Lấy hệ số tăng cường triều dựa trên mùa.
    /// Dry season → stronger spring tides. Rainy → normal (1.0).
    /// Mùa khô → triều cường mạnh hơn. Mùa mưa → bình thường (1.0).
    /// </summary>
    private float GetSeasonBoost()
    {
        // Use static Saltwater_Intrusion from RulesoftheGame_VU2_1:
        // 0.0 = rainy (fresh), 0.5 = medium, 1.0 = dry (salty)
        float saltLevel = GameRulesProvider.Saltwater_Intrusion;
        return Mathf.Lerp(1f, drySeasonSpringBoost, saltLevel);
    }

    /// <summary>
    /// Check if current tide is Spring Tide.
    /// Kiểm tra triều hiện tại có phải triều cường không.
    /// </summary>
    public bool IsSpringTide() => CurrentTide == TidalState.SpringTide;

    /// <summary>
    /// Check if current tide is Neap Tide.
    /// Kiểm tra triều hiện tại có phải triều kém không.
    /// </summary>
    public bool IsNeapTide() => CurrentTide == TidalState.NeapTide;

    /// <summary>
    /// Get total number of tidal cycles elapsed.
    /// Lấy tổng số chu kỳ triều đã trôi qua.
    /// </summary>
    public int GetCompletedCycles() => Mathf.FloorToInt(_elapsedTime / _effectiveCycleDuration);
}
