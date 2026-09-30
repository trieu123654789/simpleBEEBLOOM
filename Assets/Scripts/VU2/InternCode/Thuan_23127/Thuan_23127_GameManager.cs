using System;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// GameManager - Central score and game state controller (Singleton).
// Quản lý điểm số và trạng thái game trung tâm (Singleton).
// 
// This class is the single source of truth for the player's score.
// It also calculates salinity based on season and can report to a server.
// Lớp này là nguồn duy nhất cho điểm số của người chơi.
// Nó cũng tính toán độ mặn theo mùa và có thể gửi báo cáo lên server.
// =============================================================================
public class Thuan_23127_GameManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton instance - access from anywhere via Thuan_23127_GameManager.Instance
    // Instance Singleton - truy cập từ bất kỳ đâu qua Thuan_23127_GameManager.Instance
    // -------------------------------------------------------------------------
    public static Thuan_23127_GameManager Instance;

    // -------------------------------------------------------------------------
    // Current player score (read-only from outside).
    // Điểm hiện tại của người chơi (chỉ đọc từ bên ngoài).
    // -------------------------------------------------------------------------
    public int Score { get; private set; }
    
    // -------------------------------------------------------------------------
    // Event fired whenever the score changes. UI elements subscribe to this.
    // Sự kiện được bắn mỗi khi điểm thay đổi. Các UI element đăng ký lắng nghe.
    // -------------------------------------------------------------------------
    public event Action<int> OnScoreChanged;

    [Header("Refs")]
    // -------------------------------------------------------------------------
    // Reference to JSON reader for localization and UI text updates.
    // Tham chiếu đến JSON reader để đa ngôn ngữ và cập nhật text UI.
    // -------------------------------------------------------------------------
    public Thuan_23127_JsonReader jsonReader;
    
    [Header("Salinity Config")]
    [Tooltip("Base salinity (‰) - used if not loaded from JSON")]
    // -------------------------------------------------------------------------
    // Base salinity value in parts per thousand (‰).
    // Giá trị độ mặn gốc tính bằng phần nghìn (‰).
    // -------------------------------------------------------------------------
    public float salinityBase = 1.0f;
    
    [Tooltip("Multiplier for rainy season (lower salinity)")]
    // -------------------------------------------------------------------------
    // During rainy season, salinity = base × rainyFactor (e.g., 1.0 × 0.3 = 0.3‰).
    // Trong mùa mưa, độ mặn = base × rainyFactor (ví dụ: 1.0 × 0.3 = 0.3‰).
    // -------------------------------------------------------------------------
    public float rainyFactor = 0.3f;
    
    [Tooltip("Multiplier for dry season (higher salinity)")]
    // -------------------------------------------------------------------------
    // During dry season, salinity = base × dryFactor (e.g., 1.0 × 1.5 = 1.5‰).
    // Trong mùa khô, độ mặn = base × dryFactor (ví dụ: 1.0 × 1.5 = 1.5‰).
    // -------------------------------------------------------------------------
    public float dryFactor = 1.5f;
    
    // (Removed cached _rules field — now using GameRulesProvider.)
    // (Đã xóa trường _rules — giờ dùng GameRulesProvider.)
    
    [Header("SFX")]
    // -------------------------------------------------------------------------
    // Audio for harvest feedback - plays when player scores.
    // Âm thanh phản hồi thu hoạch - phát khi người chơi ghi điểm.
    // -------------------------------------------------------------------------
    public AudioSource audioSource;
    public AudioClip harvestClip;

    // -------------------------------------------------------------------------
    // Server communication settings (for GAMA integration).
    // Cài đặt giao tiếp server (cho tích hợp GAMA).
    // -------------------------------------------------------------------------
    [SerializeField] private string harvestMessageName = "harvest_report";
    
    // Cached list of all FarmAreas in scene for server reporting.
    // Danh sách cache tất cả FarmArea trong scene để báo cáo server.
    private readonly List<FarmArea> _cachedAreas = new List<FarmArea>();

    // =========================================================================
    // Awake - Singleton setup. Ensures only one instance exists across scenes.
    // Awake - Thiết lập Singleton. Đảm bảo chỉ có một instance tồn tại.
    // =========================================================================
    private void Awake()
    {
        // If another instance exists, destroy this duplicate.
        // Nếu đã có instance khác, hủy bản sao này.
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
        // Persist across scene loads so score is not lost.
        // Giữ lại qua các lần load scene để không mất điểm.
        DontDestroyOnLoad(gameObject);
    }

    // =========================================================================
    // GetSeasonSalinity - Calculates current salinity based on season phase and water level.
    // GetSeasonSalinity - Tính độ mặn hiện tại dựa trên giai đoạn mùa và mực nước.
    //
    // Linear mapping: Saltwater_Intrusion (0.0 → 0.5 → 1.0) × salinityBase → display (0 → 2 → 4‰).
    // Ánh xạ tuyến tính: Saltwater_Intrusion × salinityBase → hiển thị (0 → 2 → 4‰).
    //
    //   Phase 1 (T11–T1, Intrusion=0.0) →  0.0‰  (nước ngọt)
    //   Phase 2 (T2–T3,  Intrusion=0.5) →  2.0‰  (xâm nhập nhẹ)
    //   Phase 3 (T4,     Intrusion=1.0) →  4.0‰  (xâm nhập nặng)
    //
    // Set salinityBase = 4 in Inspector to get range 0–4‰.
    // Đặt salinityBase = 4 trong Inspector để có dải 0–4‰.
    //
    // Called by: PlantGrowth, FarmArea, UI components.
    // Được gọi bởi: PlantGrowth, FarmArea, các component UI.
    // =========================================================================
    public float GetSeasonSalinity()
    {
        // Linear: Intrusion × salinityBase gives 0‰ / 2‰ / 4‰ for the 3 phases.
        // Tuyến tính: Intrusion × salinityBase cho 0‰ / 2‰ / 4‰ theo 3 giai đoạn.
        float intrusion = GameRulesProvider.Saltwater_Intrusion;

        // Apply water level multiplier for fine-grained monthly variation (0.85–1.15×).
        // Áp dụng hệ số mực nước cho biến thiên tháng chi tiết (0.85–1.15×).
        var waterMultiplier = Mathf.Max(0.01f, GameRulesProvider.CurrentWaterLevelMultiplier);

        return Mathf.Max(0f, intrusion * salinityBase * waterMultiplier);
    }

    // =========================================================================
    // AddScore - Adds points to player's total score.
    // AddScore - Cộng điểm vào tổng điểm của người chơi.
    // 
    // Called by: PlantGrowth.FinalizeHarvest(), David_Fruit.CollectFruit(), David_Egg
    // Được gọi bởi: PlantGrowth.FinalizeHarvest(), David_Fruit.CollectFruit(), David_Egg
    // 
    // Example scoring (Durian with threshold 0.8):
    // - Rainy (S=0.30): S ≤ T → factor=1.0 → +4 points
    // - Dry (S=1.50): S > T → factor=0.8/1.5≈0.53 → +2 points
    // 
    // Ví dụ tính điểm (Sầu riêng với ngưỡng 0.8):
    // - Mùa mưa (S=0.30): S ≤ T → hệ số=1.0 → +4 điểm
    // - Mùa khô (S=1.50): S > T → hệ số=0.8/1.5≈0.53 → +2 điểm
    // =========================================================================
    public void AddScore(int value)
    {
        // Only allow scoring when game is active.
        // Chỉ cho phép ghi điểm khi game đang hoạt động.
        if (!GameRulesProvider.GameActive) return;
        
        // Double-check with rules controller via provider.
        // Kiểm tra lại với controller luật chơi qua provider.
        if (!GameRulesProvider.IsPlaying)
        {
            return;
        }
        
        // Update score and play feedback sound.
        // Cập nhật điểm và phát âm thanh phản hồi.
        Score += value;
        audioSource.PlayOneShot(harvestClip);
        
        // Notify all listeners (UI elements).
        // Thông báo cho tất cả listener (các UI element).
        OnScoreChanged?.Invoke(Score);

        // Update UI text elements with localized label.
        // Cập nhật các text UI với nhãn đa ngôn ngữ.
        if (!jsonReader) jsonReader = FindObjectOfType<Thuan_23127_JsonReader>();
        if (!jsonReader) return;
        
        var l = jsonReader.GetCurrentLangData();
        var scoreLabel = l?.labels?.score ?? "Score";

        if (jsonReader.scoreText)
            jsonReader.scoreText.text = $"{scoreLabel}: {Score}";
        if (jsonReader.scoreTextEndGame)
            jsonReader.scoreTextEndGame.text = $"{scoreLabel}: {Score}";
        if (jsonReader.scoreTextDetails)
            jsonReader.scoreTextDetails.text = $"{scoreLabel}: {Score}";
            
        // Uncomment to enable server reporting:
        // Bỏ comment để bật báo cáo server:
        // SendHarvestMessageToServer(value);
    }

    // =========================================================================
    // ResetScore - Resets score to zero. Called when restarting game.
    // ResetScore - Reset điểm về 0. Được gọi khi restart game.
    // =========================================================================
    public void ResetScore()
    {
        Score = 0;

        // Update UI to show zero score.
        // Cập nhật UI để hiển thị điểm 0.
        if (!jsonReader)
            jsonReader = FindObjectOfType<Thuan_23127_JsonReader>();

        if (jsonReader)
        {
            var l = jsonReader.GetCurrentLangData();
            var scoreLabel = l?.labels?.score ?? "Score";

            if (jsonReader.scoreText)
                jsonReader.scoreText.text = $"{scoreLabel}: {Score}";
            if (jsonReader.scoreTextEndGame)
                jsonReader.scoreTextEndGame.text = $"{scoreLabel}: {Score}";
            if(jsonReader.scoreTextDetails)
                jsonReader.scoreTextDetails.text = $"{scoreLabel}: {Score}";
        }

        // Notify listeners of reset.
        // Thông báo cho listener về việc reset.
        OnScoreChanged?.Invoke(Score);
    }

    // =========================================================================
    // Start - Called once when game starts. Caches FarmArea references.
    // Start - Được gọi một lần khi game bắt đầu. Cache các tham chiếu FarmArea.
    // =========================================================================
    private void Start()
    {
        CacheFarmAreas();
    }

    // =========================================================================
    // RefreshFarmAreaCache - Public method to refresh cache after scene changes.
    // RefreshFarmAreaCache - Method public để refresh cache sau khi scene thay đổi.
    // =========================================================================
    public void RefreshFarmAreaCache()
    {
        CacheFarmAreas();
    }

    // =========================================================================
    // CacheFarmAreas - Finds and caches all FarmArea objects in scene.
    // CacheFarmAreas - Tìm và cache tất cả FarmArea trong scene.
    // 
    // Why cache? Avoids expensive FindObjectsOfType calls during gameplay.
    // Tại sao cache? Tránh gọi FindObjectsOfType tốn kém trong gameplay.
    // =========================================================================
    private void CacheFarmAreas()
    {
        _cachedAreas.Clear();
        _cachedAreas.AddRange(FindObjectsOfType<FarmArea>());
    }

    // =========================================================================
    // SendHarvestMessageToServer - Reports harvest data to GAMA server.
    // SendHarvestMessageToServer - Báo cáo dữ liệu thu hoạch lên server GAMA.
    // 
    // Payload includes: total score, last gain, salinity per area.
    // Payload bao gồm: tổng điểm, điểm vừa đạt, độ mặn từng vùng.
    // 
    // Currently disabled - uncomment in AddScore() to enable.
    // Hiện tại đã tắt - bỏ comment trong AddScore() để bật.
    // =========================================================================
    private void SendHarvestMessageToServer(int lastGain)
    {
        var conn = ConnectionManager.Instance;
        if (ConnectionManager.Instance == null || !conn.IsConnectionState(ConnectionState.AUTHENTICATED))
            return;

        if (_cachedAreas.Count == 0)
            CacheFarmAreas();

        // Build payload with harvest data.
        // Tạo payload với dữ liệu thu hoạch.
        var payload = new Dictionary<string, string>
        {
            { "event", "harvest" },
            { "total_score", Score.ToString() },
            { "last_gain", lastGain.ToString() },
            { "season_salinity", GetSeasonSalinity().ToString("F2") }
        };

        // Add salinity for each farm area.
        // Thêm độ mặn cho từng vùng nông trại.
        foreach (var area in _cachedAreas)
        {
            if (!area) continue;
            var id = area.GetServerAreaId();
            payload[$"area_{id}_salinity"] = area.GetCurrentSalinityForServer().ToString("F2");
        }

        conn.SendExecutableAsk(harvestMessageName, payload);
    }
}