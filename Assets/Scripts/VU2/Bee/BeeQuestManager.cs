// =============================================================================
// BeeQuestManager - Hệ thống nhiệm vụ tuần tự cho game BeeBloom.
// BeeQuestManager - Sequential quest system for BeeBloom game.
//
// CẤU TRÚC NHIỆM VỤ / QUEST STRUCTURE
//   Danh sách Quest[] chạy tuần tự (index 0, 1, 2, ...).
//   Mỗi Quest có:
//     - Title / Subtitle hiển thị trên HUD
//     - Điều kiện hoàn thành (enum QuestType + target số lượng)
//     - Điểm bền vững thưởng khi hoàn thành
//
// LOẠI NHIỆM VỤ / QUEST TYPES
//   CollectNectar       : Thu đủ X đơn vị mật hoa
//   DeliverHoney        : Giao đủ X mật ong về tổ
//   PollinateFlowers    : Thụ phấn X lần
//   PollinateSpecies    : Thụ phấn đủ X loài khác nhau
//   SurviveTime         : Sống sót X giây mà TotalScore > 40
//   ReachThriving       : Đạt TotalScore >= 70
//
// TÍCH HỢP / INTEGRATION
//   Đọc từ BeeGameStats, BeePollinationSystem, BeeSustainabilityManager
//   Ghi questTitle/questSubtitle vào BeeGameStats → BeeHud tự cập nhật
// =============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BeeQuestManager : MonoBehaviour
{
    // =========================================================================
    // QUEST DEFINITION / ĐỊNH NGHĨA NHIỆM VỤ
    // =========================================================================
    public enum QuestType
    {
        CollectNectar,       // Thu đủ X mật hoa (units, mỗi unit = 1 lần AddNectar01)
        DeliverHoney,        // Giao đủ X mật ong về tổ
        PollinateFlowers,    // Thụ phấn X lần
        PollinateSpecies,    // Thụ phấn đủ X loài khác nhau
        SurviveTime,         // Sống sót X giây khi TotalScore > 40
        ReachThriving        // Đạt TotalScore >= 70 (không cần target)
    }

    [Serializable]
    public class Quest
    {
        public string title      = "Nhiệm vụ";
        public string subtitle   = "Hoàn thành nhiệm vụ";
        public QuestType type    = QuestType.CollectNectar;

        [Tooltip("Số mục tiêu cần đạt (tuỳ QuestType).")]
        public float targetAmount = 5f;

        [Tooltip("Điểm môi trường thưởng khi hoàn thành.")]
        public float envReward   = 0f;
        [Tooltip("Điểm kinh tế thưởng khi hoàn thành.")]
        public float ecoReward   = 0f;
        [Tooltip("Điểm xã hội thưởng khi hoàn thành.")]
        public float socReward   = 0f;

        [HideInInspector] public float progress = 0f;
        [HideInInspector] public bool  completed = false;
    }

    // =========================================================================
    // SETTINGS
    // =========================================================================
    [Header("Quests / Danh sách nhiệm vụ")]
    [Tooltip("Danh sách nhiệm vụ tuần tự. Nếu để trống sẽ dùng quest mặc định.")]
    public List<Quest> quests = new();

    [Header("Game Over / Kết thúc")]
    [Tooltip("Có kết thúc game khi SustainabilityManager Collapsed không.")]
    public bool gameOverOnCollapse = true;

    [Tooltip("Có hiển thị win screen khi xong hết quest không.")]
    public bool winOnAllQuestsComplete = true;

    // =========================================================================
    // REFERENCES / THAM CHIẾU
    // =========================================================================
    [Header("References")]
    public BeeGameStats         playerStats;
    public BeePollinationSystem pollination;
    public BeeSustainabilityManager sustainability;
    public BeeGameOverScreen    gameOverScreen; // nullable

    // =========================================================================
    // PUBLIC STATE
    // =========================================================================
    public int  CurrentQuestIndex { get; private set; } = 0;
    public bool AllQuestsComplete { get; private set; } = false;
    public bool IsGameOver        { get; private set; } = false;

    public Quest CurrentQuest => (quests != null && CurrentQuestIndex < quests.Count)
                                  ? quests[CurrentQuestIndex] : null;

    // =========================================================================
    // EVENTS
    // =========================================================================
    public Action<Quest>  OnQuestStarted;
    public Action<Quest>  OnQuestCompleted;
    public Action         OnAllQuestsComplete;
    public Action         OnGameOver;

    // =========================================================================
    // INTERNAL
    // =========================================================================
    private int   _pollinationCount     = 0;
    private float _surviveTimer         = 0f;
    private readonly HashSet<BeeFlower.FlowerSpecies> _pollinatedSpecies = new();
    private float _nectarCollectedTotal = 0f;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        // Auto-find references
        if (playerStats    == null) playerStats    = FindObjectOfType<BeeGameStats>();
        if (pollination    == null) pollination    = FindObjectOfType<BeePollinationSystem>();
        if (sustainability == null) sustainability = FindObjectOfType<BeeSustainabilityManager>();
        if (gameOverScreen == null) gameOverScreen = FindObjectOfType<BeeGameOverScreen>();

        // Default quest list nếu chưa cấu hình
        if (quests == null || quests.Count == 0)
            BuildDefaultQuests();
    }

    private void OnEnable()
    {
        if (sustainability != null)
            sustainability.OnCollapsed += HandleCollapsed;
    }

    private void OnDisable()
    {
        if (sustainability != null)
            sustainability.OnCollapsed -= HandleCollapsed;
    }

    private void Start()
    {
        if (quests.Count > 0) StartQuest(0);
    }

    private void Update()
    {
        if (IsGameOver || AllQuestsComplete) return;
        if (CurrentQuest == null) return;

        UpdateQuestProgress();
        CheckQuestComplete();
        PushToHUD();
    }

    // =========================================================================
    // UpdateQuestProgress - Đọc trạng thái hiện tại để tính tiến độ.
    // =========================================================================
    private void UpdateQuestProgress()
    {
        var q = CurrentQuest;
        if (q.completed) return;

        switch (q.type)
        {
            case QuestType.CollectNectar:
                // Đọc từ playerStats: mỗi lần nectar > 0 tăng dần
                // Theo dõi từ bên ngoài qua sự tăng của nectar01
                if (playerStats != null)
                    q.progress = Mathf.Max(q.progress, playerStats.honeyDelivered * q.targetAmount / Mathf.Max(1, playerStats.honeyGoal));
                break;

            case QuestType.DeliverHoney:
                if (playerStats != null)
                    q.progress = playerStats.honeyDelivered;
                break;

            case QuestType.PollinateFlowers:
                q.progress = _pollinationCount;
                break;

            case QuestType.PollinateSpecies:
                q.progress = _pollinatedSpecies.Count;
                break;

            case QuestType.SurviveTime:
                if (sustainability != null && sustainability.TotalScore >= 40f)
                    _surviveTimer += Time.deltaTime;
                q.progress = _surviveTimer;
                break;

            case QuestType.ReachThriving:
                if (sustainability != null)
                    q.progress = sustainability.TotalScore;
                break;
        }
    }

    // =========================================================================
    // CheckQuestComplete
    // =========================================================================
    private void CheckQuestComplete()
    {
        var q = CurrentQuest;
        if (q.completed) return;

        bool done = q.type switch
        {
            QuestType.ReachThriving => sustainability != null &&
                                       sustainability.Status == BeeSustainabilityManager.EcosystemStatus.Thriving,
            _                       => q.progress >= q.targetAmount
        };

        if (!done) return;

        q.completed = true;
        CompleteCurrentQuest();
    }

    // =========================================================================
    // CompleteCurrentQuest
    // =========================================================================
    private void CompleteCurrentQuest()
    {
        var q = CurrentQuest;
        Debug.Log($"[BeeQuestManager] ✅ Hoàn thành: {q.title}");

        // Thưởng điểm bền vững
        sustainability?.AddEnvironmentPoints(q.envReward);
        sustainability?.AddEconomicPoints(q.ecoReward);
        sustainability?.AddSocialPoints(q.socReward);

        OnQuestCompleted?.Invoke(q);

        // Chuyển quest tiếp theo
        int next = CurrentQuestIndex + 1;
        if (next < quests.Count)
        {
            CurrentQuestIndex = next;
            StartQuest(next);
        }
        else
        {
            AllQuestsComplete = true;
            OnAllQuestsComplete?.Invoke();
            gameOverScreen?.ShowWin(sustainability?.TotalScore ?? 0f,
                                   sustainability?.EnvironmentScore ?? 0f,
                                   sustainability?.EconomicScore ?? 0f,
                                   sustainability?.SocialScore ?? 0f);
            Debug.Log("[BeeQuestManager] 🎉 TẤT CẢ NHIỆM VỤ HOÀN THÀNH!");
        }
    }

    // =========================================================================
    // StartQuest
    // =========================================================================
    private void StartQuest(int index)
    {
        CurrentQuestIndex = index;
        var q = quests[index];
        q.progress = 0f;
        q.completed = false;

        if (playerStats != null)
        {
            playerStats.questTitle    = q.title;
            playerStats.questSubtitle = q.subtitle;
        }

        OnQuestStarted?.Invoke(q);
        Debug.Log($"[BeeQuestManager] 🎯 Bắt đầu quest: {q.title} | Mục tiêu: {q.targetAmount}");
    }

    // =========================================================================
    // PushToHUD - Cập nhật HUD tiến độ quest.
    // =========================================================================
    private void PushToHUD()
    {
        if (playerStats == null || CurrentQuest == null) return;
        var q = CurrentQuest;

        // Hiển thị tiến độ trong subtitle
        float pct = q.targetAmount > 0 ? Mathf.Clamp01(q.progress / q.targetAmount) : 0f;
        playerStats.questSubtitle = q.type switch
        {
            QuestType.ReachThriving  => $"Điểm tổng: {sustainability?.TotalScore:F0}/70",
            QuestType.SurviveTime    => $"Thời gian: {q.progress:F0}/{q.targetAmount:F0}s",
            _                        => $"{q.subtitle} ({q.progress:F0}/{q.targetAmount:F0})"
        };
    }

    // =========================================================================
    // HandleCollapsed - Game Over khi hệ sinh thái sụp đổ.
    // =========================================================================
    private void HandleCollapsed()
    {
        if (!gameOverOnCollapse || IsGameOver) return;
        IsGameOver = true;
        OnGameOver?.Invoke();
        gameOverScreen?.ShowGameOver(sustainability?.TotalScore ?? 0f,
                                     sustainability?.EnvironmentScore ?? 0f,
                                     sustainability?.EconomicScore ?? 0f,
                                     sustainability?.SocialScore ?? 0f);
        Debug.Log("[BeeQuestManager] 💀 GAME OVER — Hệ sinh thái sụp đổ!");
    }

    // =========================================================================
    // Pollination tracking - Gọi từ BeePollinationSystem khi thụ phấn thành công
    // =========================================================================
    /// <summary>Gọi khi ong thụ phấn thành công.</summary>
    public void OnPollinationSuccess(BeeFlower.FlowerSpecies species)
    {
        _pollinationCount++;
        _pollinatedSpecies.Add(species);
    }

    // =========================================================================
    // BuildDefaultQuests - Quest mặc định nếu không cấu hình.
    // =========================================================================
    private void BuildDefaultQuests()
    {
        quests = new List<Quest>
        {
            new Quest
            {
                title = "Thu thập mật hoa",
                subtitle = "Hút mật từ các bông hoa",
                type = QuestType.CollectNectar,
                targetAmount = 3f,
                ecoReward = 10f
            },
            new Quest
            {
                title = "Thụ phấn",
                subtitle = "Mang phấn đến hoa cùng loài",
                type = QuestType.PollinateFlowers,
                targetAmount = 5f,
                envReward = 15f, socReward = 10f
            },
            new Quest
            {
                title = "Giao mật về tổ",
                subtitle = "Bay về tổ ong khi đầy mật",
                type = QuestType.DeliverHoney,
                targetAmount = 5f,
                ecoReward = 20f
            },
            new Quest
            {
                title = "Đa dạng sinh học",
                subtitle = "Thụ phấn đủ 3 loài hoa",
                type = QuestType.PollinateSpecies,
                targetAmount = 3f,
                socReward = 30f
            },
            new Quest
            {
                title = "Hệ sinh thái bội thu",
                subtitle = "Đưa điểm tổng lên 70",
                type = QuestType.ReachThriving,
                targetAmount = 70f,
                envReward = 10f, ecoReward = 10f, socReward = 10f
            }
        };
    }
}
