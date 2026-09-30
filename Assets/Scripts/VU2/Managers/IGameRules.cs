using System;
using UnityEngine;

// =============================================================================
// IGameRules - Shared interface for game rules controllers.
// IGameRules - Interface chung cho các controller luật chơi.
//
// Both RulesoftheGame_VU2_1 (Level 1) and RulesOfTheGame_VU2_2 (Level 2)
// implement this interface. Consumer scripts should use GameRulesProvider
// instead of directly referencing a specific level's static fields.
//
// Cả RulesoftheGame_VU2_1 (Màn 1) và RulesOfTheGame_VU2_2 (Màn 2)
// đều implement interface này. Các script consumer nên dùng GameRulesProvider
// thay vì trực tiếp tham chiếu static fields của level cụ thể.
// =============================================================================
public interface IGameRules
{
    float SaltwaterIntrusion { get; }
    SeasonPhase GetCurrentPhase();
    int GetCurrentMonthIndex();
    float GetCurrentWaterLevelPercent();
    float GetCurrentWaterLevelMultiplier();
    bool IsGameActive();
    bool IsPlaying();
    ScoreFlow GetScoringMode();
    
    // Instance fields needed by some consumers.
    float MonthDuration { get; }
    float TimeRemaining { get; }
    Transform Player { get; }
    GameObject Target { get; }
    
    event Action<SeasonPhase> PhaseChanged;
    event Action<int> MonthChanged;
    event Action<float> WaterLevelChanged;
}
