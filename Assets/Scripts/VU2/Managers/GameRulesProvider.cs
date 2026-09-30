using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// =============================================================================
// GameRulesProvider - Static bridge to the active level's IGameRules.
// GameRulesProvider - Cầu nối tĩnh đến IGameRules của level đang chạy.
//
// USAGE: Replace all direct references to RulesoftheGame_VU2_1.* statics
//        with GameRulesProvider.* equivalents.
//
// SỬ DỤNG: Thay tất cả tham chiếu trực tiếp RulesoftheGame_VU2_1.*
//           bằng GameRulesProvider.* tương ứng.
//
// Example migration:
//   BEFORE: RulesoftheGame_VU2_1.Saltwater_Intrusion
//   AFTER:  GameRulesProvider.Saltwater_Intrusion
//
//   BEFORE: RulesoftheGame_VU2_1.OnPhaseChanged += handler;
//   AFTER:  GameRulesProvider.OnPhaseChanged += handler;
// =============================================================================
public static class GameRulesProvider
{
    // =========================================================================
    // ACTIVE RULES INSTANCE
    // =========================================================================
    private static IGameRules _active;

    // =========================================================================
    // Register / Unregister — called by each Rules class in OnEnable/OnDisable.
    // Đăng ký / Hủy đăng ký — được gọi bởi mỗi Rules class trong OnEnable/OnDisable.
    // =========================================================================
    public static void Register(IGameRules rules)
    {
        if (_active != null && _active != rules)
        {
            // Unsubscribe old forwarding before switching.
            // Hủy forwarding cũ trước khi chuyển.
            UnbindEvents(_active);
        }
        _active = rules;
        BindEvents(_active);
        Debug.Log($"[GameRulesProvider] Registered: {rules.GetType().Name}");
    }

    public static void Unregister(IGameRules rules)
    {
        if (_active == rules)
        {
            UnbindEvents(_active);
            _active = null;
        }
    }

    // =========================================================================
    // STATIC PROPERTIES — drop-in replacements for RulesoftheGame_VU2_1.*
    // THUỘC TÍNH STATIC — thay thế trực tiếp cho RulesoftheGame_VU2_1.*
    // =========================================================================
    public static float Saltwater_Intrusion => _active?.SaltwaterIntrusion ?? 0f;
    public static SeasonPhase CurrentPhase => _active?.GetCurrentPhase() ?? SeasonPhase.Rainy1;
    public static int CurrentMonthIndex => _active?.GetCurrentMonthIndex() ?? 1;
    public static float CurrentWaterLevelPercent => _active?.GetCurrentWaterLevelPercent() ?? 40f;
    public static float CurrentWaterLevelMultiplier => _active?.GetCurrentWaterLevelMultiplier() ?? 1f;
    public static bool GameActive => _active?.IsGameActive() ?? false;
    public static bool IsPlaying => _active?.IsPlaying() ?? false;
    public static ScoreFlow CurrentScoringMode => _active?.GetScoringMode() ?? ScoreFlow.Seasonal;
    public static float MonthDuration => _active?.MonthDuration ?? 30f;
    public static float TimeRemaining => _active?.TimeRemaining ?? 0f;
    public static Transform Player => _active?.Player;
    public static GameObject Target => _active?.Target;

    // =========================================================================
    // EVENTS — forwarded from the active IGameRules
    // SỰ KIỆN — chuyển tiếp từ IGameRules đang hoạt động
    // =========================================================================
    public static event Action<SeasonPhase> OnPhaseChanged;
    public static event Action<int> OnMonthChanged;
    public static event Action<float> OnWaterLevelChanged;

    // =========================================================================
    // EVENT FORWARDING
    // CHUYỂN TIẾP SỰ KIỆN
    // =========================================================================
    private static void BindEvents(IGameRules rules)
    {
        if (rules == null) return;
        rules.PhaseChanged += ForwardPhaseChanged;
        rules.MonthChanged += ForwardMonthChanged;
        rules.WaterLevelChanged += ForwardWaterLevelChanged;
    }

    private static void UnbindEvents(IGameRules rules)
    {
        if (rules == null) return;
        rules.PhaseChanged -= ForwardPhaseChanged;
        rules.MonthChanged -= ForwardMonthChanged;
        rules.WaterLevelChanged -= ForwardWaterLevelChanged;
    }

    private static void ForwardPhaseChanged(SeasonPhase p) => OnPhaseChanged?.Invoke(p);
    private static void ForwardMonthChanged(int m) => OnMonthChanged?.Invoke(m);
    private static void ForwardWaterLevelChanged(float w) => OnWaterLevelChanged?.Invoke(w);

    // =========================================================================
    // RESET — call when loading a new scene to clear stale state.
    // RESET — gọi khi load scene mới để xóa state cũ.
    // =========================================================================
    public static void Reset()
    {
        if (_active != null)
            UnbindEvents(_active);
        _active = null;
        OnPhaseChanged = null;
        OnMonthChanged = null;
        OnWaterLevelChanged = null;
    }
}
