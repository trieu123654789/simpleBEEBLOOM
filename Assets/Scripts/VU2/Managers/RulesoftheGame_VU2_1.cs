using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// ScoreFlow - Determines when scores are calculated.
// ScoreFlow - Xác định thời điểm tính điểm.
// =============================================================================
public enum ScoreFlow 
{ 
    GrowthTime,  // Score on each harvest / Tính điểm mỗi lần thu hoạch
    Seasonal     // Score only when season changes / Tính điểm khi đổi mùa
}

// =============================================================================
// RulesoftheGame_VU2_1 - Main game controller for seasons, time, and game state.
// RulesoftheGame_VU2_1 - Controller chính điều khiển mùa, thời gian, và trạng thái game.
// 
// This is the "brain" of the game. It controls:
// - Season transitions (Rainy → Dry → End)
// - Time flow and game duration
// - Weather effects (rain particles, skybox)
// - Water level movement
// - VR locomotion locking
// - Month system (6 months overlay: Nov-Apr)
// 
// Đây là "bộ não" của game. Nó điều khiển:
// - Chuyển đổi mùa (Mưa → Khô → Kết thúc)
// - Luồng thời gian và thời lượng game
// - Hiệu ứng thời tiết (hạt mưa, skybox)
// - Di chuyển mực nước
// - Khóa di chuyển VR
// - Hệ thống tháng (6 tháng overlay: T11-T4)
// =============================================================================
public class RulesoftheGame_VU2_1 : MonoBehaviour, IGameRules
{
    // =========================================================================
    // WEATHER & VISUAL REFERENCES
    // THAM CHIẾU THỜI TIẾT & HÌNH ẢNH
    // =========================================================================
    
    // Rain particle system object.
    // Object hệ thống hạt mưa.
    public GameObject Weather_Rain;
    
    // HUD clock display.
    // Hiển thị đồng hồ trên HUD.
    public Text clockText;
    
    // Current elapsed time (counts up from 0).
    // Thời gian đã trôi qua (đếm lên từ 0).
    public float timeRemaining = 0;
    
    // Is game currently running?
    // Game có đang chạy không?
    public bool playGame = false;
    
    // Season indicator icons on HUD.
    // Icon chỉ báo mùa trên HUD.
    public GameObject Rain_image;
    public GameObject Sun_image;

    // =========================================================================
    // UI MENU REFERENCES
    // THAM CHIẾU MENU UI
    // =========================================================================
    public GameObject StartMenu;
    public GameObject ResultMenu;
    public GameObject ResultDetailsScore;
    public GameObject InteractiveObjects;
    public GameObject UIForVR;
    public GameObject NPC_Talk;
    
    // Skybox materials for each season.
    // Vật liệu Skybox cho từng mùa.
    public Material Skybox_Rain;
    public Material Skybox_Sun;

    // =========================================================================
    // STATIC GAME STATE (accessible from anywhere)
    // TRẠNG THÁI GAME STATIC (truy cập từ bất kỳ đâu)
    // =========================================================================
    
    // Saltwater intrusion level: 0.0 = rainy (fresh), 1.0 = dry (salty).
    // Mức xâm nhập mặn: 0.0 = mưa (ngọt), 1.0 = khô (mặn).
    public static float Saltwater_Intrusion = 0.0f;
    
    // Current season phase.
    // Pha mùa hiện tại.
    private static SeasonPhase _currentPhase = SeasonPhase.Rainy1;
    
    // Event fired when season changes. UI and game objects subscribe to this.
    // Sự kiện được bắn khi mùa thay đổi. UI và game objects đăng ký lắng nghe.
    public static event System.Action<SeasonPhase> OnPhaseChanged;
    
    // Cached phase to detect changes.
    // Phase được cache để phát hiện thay đổi.
    private static SeasonPhase _cachedPhase = (SeasonPhase)(-1);

    // =========================================================================
    // MONTH SYSTEM (6 months overlay: November - April)
    // HỆ THỐNG THÁNG (6 tháng overlay: Tháng 11 - Tháng 4)
    // =========================================================================
    
    // Current month (1-6, maps to calendar Nov-Apr).
    // Tháng hiện tại (1-6, map sang lịch T11-T4).
    public static int CurrentMonthIndex { get; private set; } = 1;
    
    // Water level percentage (0-100) based on month.
    // Phần trăm mực nước (0-100) dựa trên tháng.
    public static float CurrentWaterLevelPercent { get; private set; } = 40f;
    
    // Multiplier applied to salinity based on water level.
    // Hệ số áp dụng cho độ mặn dựa trên mực nước.
    public static float CurrentWaterLevelMultiplier { get; private set; } = 1f;
    
    // Events for month and water level changes.
    // Sự kiện cho thay đổi tháng và mực nước.
    public static event System.Action<int> OnMonthChanged;
    public static event System.Action<float> OnWaterLevelChanged;

    // Is game currently active (playing)?
    // Game có đang hoạt động (đang chơi) không?
    public static bool GameActive { get; private set; } = false;

    // =========================================================================
    // WATER MOVEMENT (visual water level animation)
    // DI CHUYỂN NƯỚC (animation mực nước trực quan)
    // =========================================================================
    
    // Water object to animate.
    // Object nước để animate.
    public GameObject target;
    
    // Start and end positions for water animation.
    // Vị trí bắt đầu và kết thúc cho animation nước.
    public Vector3 pointA;
    public Vector3 pointB;
    
    // Duration of water movement animation.
    // Thời lượng animation di chuyển nước.
    public float moveTime = 3f;

    // =========================================================================
    // AUDIO
    // ÂM THANH
    // =========================================================================
    [Header("Music")]
    public AudioClip rainMusic;       // Music during rainy season / Nhạc mùa mưa
    public AudioClip normalMusic;     // Music during dry season / Nhạc mùa khô
    [FormerlySerializedAs("messageSFX")] 
    public AudioClip messageSfx;      // Sound when game ends / Âm thanh khi game kết thúc
    private AudioSource _audioSource;

    // =========================================================================
    // INTERNAL STATE FLAGS
    // CỜ TRẠNG THÁI NỘI BỘ
    // =========================================================================
    private bool _moving, _rainning;
    private bool _enteredDry = false, _enteredRainy2 = false;
    private float _phaseStartTime = 0f;
    private Vector3 _fromPos, _toPos;
    private bool _applyMoveThisFrame = false;
    private bool _didSnapPointA = false;

    // =========================================================================
    // VR LOCOMOTION CONTROL
    // ĐIỀU KHIỂN DI CHUYỂN VR
    // =========================================================================
    [Header("XR Move Lock (VR)")]
    // Reference to XR move provider - disabled during menus.
    // Tham chiếu đến XR move provider - tắt trong menu.
    public ActionBasedContinuousMoveProvider moveProvider;
    public LocomotionSystem locomotionSystem;                 
    public ActionBasedContinuousTurnProvider turnProvider;   
    
    // If true, also lock turning when movement is locked.
    // Nếu true, cũng khóa xoay khi di chuyển bị khóa.
    public bool lockTurningToo = false;

    // =========================================================================
    // PLAYER POSITION TRACKING
    // THEO DÕI VỊ TRÍ NGƯỜI CHƠI
    // =========================================================================
    [Header("Player Start")]
    public Transform player;
    private Vector3 _playerStartPos;
    private bool _playerStartSaved = false;
    
    // =========================================================================
    // UI ROOTS
    // GỐC UI
    // =========================================================================
    [Header("UI Root (optional)")]
    public GameObject GameplayUIRoot; 
    public GameObject GameUIRoot;

    // =========================================================================
    // SCORING MODE CONFIGURATION
    // CẤU HÌNH CHẾ ĐỘ TÍNH ĐIỂM
    // =========================================================================
    [Header("Scoring Mode")]
    // GrowthTime = score immediately on harvest.
    // Seasonal = score when season changes (force harvest all).
    // GrowthTime = tính điểm ngay khi thu hoạch.
    // Seasonal = tính điểm khi đổi mùa (ép thu hoạch tất cả).
    public ScoreFlow scoringMode = ScoreFlow.Seasonal;
    
    public static ScoreFlow CurrentScoringMode { get; private set; }

    // =========================================================================
    // MONTH SYSTEM CONFIGURATION
    // CẤU HÌNH HỆ THỐNG THÁNG
    // =========================================================================
    [Header("Month System (6 months: Nov-Apr)")]
    [Tooltip("Total game duration in seconds (default 180s = 3 minutes).")]
    public float totalGameDuration = 180f;
    
    [Tooltip("Duration of each month in seconds (default 30s = 180s/6).")]
    public float monthDuration = 30f;
    
    [Tooltip("Water level (0-100%) for each month. Index 0 = November, ..., 5 = April.")]
    // Water level table: high in Nov, declining to 20% by April.
    // Bảng mực nước: cao vào T11, giảm dần đến 20% vào T4.
    // T11: 80%, T12: 65%, T1: 50%, T2: 40%, T3: 30%, T4: 20%
    public float[] monthWaterLevels = new float[6]
    {
        80f, 65f, 50f, 40f, 30f, 20f
    };

    [Tooltip("Salinity multiplier when water level is lowest.")]
    public float waterLevelMultiplierMin = 0.85f;
    
    [Tooltip("Salinity multiplier when water level is highest.")]
    public float waterLevelMultiplierMax = 1.15f;

    // =========================================================================
    // IGameRules IMPLEMENTATION
    // ==========================================================================
    // Instance-level events for GameRulesProvider forwarding.
    // Sự kiện cấp instance để GameRulesProvider chuyển tiếp.
    public event System.Action<SeasonPhase> PhaseChanged;
    public event System.Action<int> MonthChanged;
    public event System.Action<float> WaterLevelChanged;

    float IGameRules.SaltwaterIntrusion => Saltwater_Intrusion;
    SeasonPhase IGameRules.GetCurrentPhase() => _currentPhase;
    int IGameRules.GetCurrentMonthIndex() => CurrentMonthIndex;
    float IGameRules.GetCurrentWaterLevelPercent() => CurrentWaterLevelPercent;
    float IGameRules.GetCurrentWaterLevelMultiplier() => CurrentWaterLevelMultiplier;
    bool IGameRules.IsGameActive() => GameActive;
    bool IGameRules.IsPlaying() => playGame;
    ScoreFlow IGameRules.GetScoringMode() => CurrentScoringMode;
    float IGameRules.MonthDuration => monthDuration;
    float IGameRules.TimeRemaining => timeRemaining;
    Transform IGameRules.Player => player;
    GameObject IGameRules.Target => target;

    private void OnEnable() => GameRulesProvider.Register(this);
    private void OnDisable() => GameRulesProvider.Unregister(this);

    // Helper: fire instance events alongside statics (called from SetPhase etc.)
    private void FirePhaseChanged(SeasonPhase p) => PhaseChanged?.Invoke(p);
    private void FireMonthChanged(int m) => MonthChanged?.Invoke(m);
    private void FireWaterLevelChanged(float w) => WaterLevelChanged?.Invoke(w);

    // =========================================================================
    // Awake - Initialize references and sync scoring mode.
    // Awake - Khởi tạo tham chiếu và đồng bộ chế độ tính điểm.
    // =========================================================================
    private void Awake()
    {
        // Auto-find XR components if not assigned.
        // Tự động tìm component XR nếu chưa gán.
        if (!moveProvider) moveProvider = FindObjectOfType<ActionBasedContinuousMoveProvider>(true);
        if (!locomotionSystem) locomotionSystem = FindObjectOfType<LocomotionSystem>(true);
        if (!turnProvider) turnProvider = FindObjectOfType<ActionBasedContinuousTurnProvider>(true);
        
        _audioSource = GetComponent<AudioSource>();
        CurrentScoringMode = scoringMode;
    }

    // =========================================================================
    // Start - Set initial UI state before game begins.
    // Start - Thiết lập trạng thái UI ban đầu trước khi game bắt đầu.
    // =========================================================================
    public void Start()
    {
        playGame = false;
        GameActive = false;

        // Show start menu, hide result menu.
        // Hiển thị menu bắt đầu, ẩn menu kết quả.
        ResultMenu.SetActive(false);
        StartMenu.SetActive(true);
        
        // Set default weather (sunny).
        // Đặt thời tiết mặc định (nắng).
        Weather_Rain.SetActive(false);
        Rain_image.SetActive(false);
        Sun_image.SetActive(true);
        NPC_Talk.SetActive(false);
        PlayMusic(normalMusic);

        _moving = false;
        _enteredDry = _enteredRainy2 = false;
        _applyMoveThisFrame = false;

        // Lock player movement until game starts.
        // Khóa di chuyển người chơi cho đến khi game bắt đầu.
        SetMovementLocked(true);
        GameplayUIRoot.SetActive(true);
        GameUIRoot.SetActive(true);
        
        // Force water level values (override Inspector-serialized values).
        // Bắt buộc dùng giá trị mực nước trong code (bỏ qua giá trị Inspector).
        // T11: 80%, T12: 65%, T1: 50%, T2: 40%, T3: 30%, T4: 20%
        monthWaterLevels = new float[] { 80f, 65f, 50f, 40f, 30f, 20f };
    }

    // =========================================================================
    // Update - Main game loop. Runs every frame during gameplay.
    // Update - Vòng lặp game chính. Chạy mỗi frame trong gameplay.
    // 
    // TIMELINE (3 phases, computed from monthDuration):
    // Phase 1 (T11–T1): 0 to monthDuration×3  — Saltwater_Intrusion = 0.0
    // Phase 2 (T2–T3):  monthDuration×3 to ×5 — Saltwater_Intrusion = 0.5
    // Phase 3 (T4):     monthDuration×5 to ×6 — Saltwater_Intrusion = 1.0
    // >monthDuration×6: Game End
    // 
    // DÒNG THỜI GIAN (3 giai đoạn, tính từ monthDuration):
    // Giai đoạn 1 (T11–T1): 0 đến monthDuration×3  — Saltwater_Intrusion = 0.0
    // Giai đoạn 2 (T2–T3):  monthDuration×3 đến ×5 — Saltwater_Intrusion = 0.5
    // Giai đoạn 3 (T4):     monthDuration×5 đến ×6 — Saltwater_Intrusion = 1.0
    // >monthDuration×6: Kết thúc Game
    // =========================================================================
    public void Update()
    {
        _applyMoveThisFrame = false;
        if (!playGame) return;

        // Increment elapsed time.
        // Tăng thời gian đã trôi qua.
        timeRemaining += Time.deltaTime;
        DisplayTime(timeRemaining);

        // Update month system (independent of phase logic).
        // Cập nhật hệ thống tháng (độc lập với logic giai đoạn).
        UpdateMonthAndWaterLevel(timeRemaining);

        // Calculate phase boundaries from monthDuration (NOT hardcoded).
        // Tính mốc giai đoạn từ monthDuration (KHÔNG hardcode).
        // Phase 1: 3 months (T11,T12,T1), Phase 2: 2 months (T2,T3), Phase 3: 1 month (T4)
        float phase1End = monthDuration * 3f;   // Default 30×3 = 90s
        float phase2End = monthDuration * 5f;   // Default 30×5 = 150s
        float gameEnd   = monthDuration * 6f;   // Default 30×6 = 180s

        // =====================================================================
        // PHASE 1: T11–T1 — Low salinity
        // GIAI ĐOẠN 1: T11–T1 — Độ mặn thấp
        // =====================================================================
        if (timeRemaining <= phase1End)
        {
            SetPhase(SeasonPhase.Rainy1);
            _rainning = true;
            
            // Enable rain effects and rainy skybox.
            // Bật hiệu ứng mưa và skybox mưa.
            //Weather_Rain.SetActive(true); // DISABLED — tắt hiệu ứng mưa tạm thời
            Rain_image.SetActive(true);
            Sun_image.SetActive(false);
            RenderSettings.skybox = Skybox_Rain; 
            DynamicGI.UpdateEnvironment();
            PlayMusic(rainMusic);

            _moving = false;
            _enteredDry = false;
            _enteredRainy2 = false;
        }
        // =====================================================================
        // PHASE 2: T2–T3 — Medium salinity
        // GIAI ĐOẠN 2: T2–T3 — Độ mặn trung bình
        // =====================================================================
        else if (timeRemaining > phase1End && timeRemaining <= phase2End)
        {
            SetPhase(SeasonPhase.Dry);
            _rainning = false;
            
            // Disable rain, enable sunny effects.
            // Tắt mưa, bật hiệu ứng nắng.
            //Weather_Rain.SetActive(false);
            Rain_image.SetActive(false);
            Sun_image.SetActive(true);
            RenderSettings.skybox = Skybox_Sun; 
            DynamicGI.UpdateEnvironment();
            PlayMusic(normalMusic);

            // Start water movement animation when entering phase 2.
            // Bắt đầu animation di chuyển nước khi vào giai đoạn 2.
            if (!_enteredDry)
            {
                _enteredDry = true; 
                _phaseStartTime = timeRemaining;
                _fromPos = target ? target.transform.position : pointA;
                _toPos = Vector3.Lerp(pointA, pointB, 0.5f); // Move halfway
                _moving = true;
            }
            if (_moving && target) _applyMoveThisFrame = true;
        }
        // =====================================================================
        // PHASE 3: T4 — High salinity
        // GIAI ĐOẠN 3: T4 — Độ mặn cao
        // =====================================================================
        else if (timeRemaining > phase2End && timeRemaining <= gameEnd)
        {
            SetPhase(SeasonPhase.Rainy2);
            _rainning = false;
            
            Weather_Rain.SetActive(false);
            Rain_image.SetActive(false);
            Sun_image.SetActive(true);
            RenderSettings.skybox = Skybox_Sun; 
            DynamicGI.UpdateEnvironment();
            PlayMusic(normalMusic);

            // Continue water movement to full extent in phase 3.
            // Tiếp tục di chuyển nước đến mức tối đa trong giai đoạn 3.
            if (!_enteredRainy2)
            {
                _enteredRainy2 = true;
                _phaseStartTime = timeRemaining;
                _fromPos = target ? target.transform.position : Vector3.Lerp(pointA, pointB, 0.5f);
                _toPos = pointB;
                _moving = true;
            }
            if (_moving && target) _applyMoveThisFrame = true;
        }
        // =====================================================================
        // GAME END (>gameEnd)
        // KẾT THÚC GAME (>gameEnd)
        // =====================================================================
        else
        {
            _rainning = false;
            Weather_Rain.SetActive(false);
            Rain_image.SetActive(false);
            Sun_image.SetActive(true);
            RenderSettings.skybox = Skybox_Sun; 
            DynamicGI.UpdateEnvironment();
            PlayMusic(normalMusic);

            // Stop the game.
            // Dừng game.
            playGame = false;
            GameActive = false;

            _moving = false;
            _applyMoveThisFrame = false;

            // Freeze all HUDs and scoreboards.
            // Đóng băng tất cả HUD và bảng điểm.
            var farms = FindObjectsOfType<FarmArea>(true);
            foreach (var a in farms) a.FreezeHUD();

            var boards = FindObjectsOfType<Thuan_23127_TotalBoard>(true);
            foreach (var b in boards) b.Freeze(true);

            // Lock player movement.
            // Khóa di chuyển người chơi.
            SetMovementLocked(true);

            // Play end game sound and show result menu.
            // Phát âm thanh kết thúc và hiển thị menu kết quả.
            if (_audioSource && messageSfx) _audioSource.PlayOneShot(messageSfx);
            ResultMenu.SetActive(true);
            GameplayUIRoot.SetActive(false);
            GameUIRoot.SetActive(false);
        }
    }

    // =========================================================================
    // UpdateMonthAndWaterLevel - Updates month index and water level from time.
    // UpdateMonthAndWaterLevel - Cập nhật tháng và mực nước theo thời gian.
    // 
    // This runs independently of the season system to provide finer control.
    // Chạy độc lập với hệ thống mùa để cung cấp kiểm soát chi tiết hơn.
    // =========================================================================
    private void UpdateMonthAndWaterLevel(float t)
    {
        // Calculate current month (1-6) based on elapsed time.
        // Tính tháng hiện tại (1-6) dựa trên thời gian đã trôi qua.
        float safeMonthDuration = Mathf.Max(0.01f, monthDuration);
        int monthIndex = Mathf.Clamp(Mathf.FloorToInt(t / safeMonthDuration) + 1, 1, 6);

        // Fire event if month changed.
        // Bắn sự kiện nếu tháng thay đổi.
        if (monthIndex != CurrentMonthIndex)
        {
            CurrentMonthIndex = monthIndex;
            OnMonthChanged?.Invoke(CurrentMonthIndex);
            FireMonthChanged(CurrentMonthIndex);
        }

        // Smoothly interpolate water level between months for gradual decrease.
        // Nội suy mượt mực nước giữa các tháng để giảm dần theo thời gian.
        float progressInMonth = (t / safeMonthDuration) - (monthIndex - 1);  // 0.0 → 1.0 within current month
        int nextMonthIndex = Mathf.Min(monthIndex + 1, 6);
        float currentMonthWater = GetWaterLevelForMonth(monthIndex);
        float nextMonthWater = GetWaterLevelForMonth(nextMonthIndex);
        float waterLevel = Mathf.Lerp(currentMonthWater, nextMonthWater, progressInMonth);

        if (!Mathf.Approximately(waterLevel, CurrentWaterLevelPercent))
        {
            CurrentWaterLevelPercent = waterLevel;
            CurrentWaterLevelMultiplier = GetWaterLevelMultiplier(CurrentWaterLevelPercent);
            OnWaterLevelChanged?.Invoke(CurrentWaterLevelPercent);
            FireWaterLevelChanged(CurrentWaterLevelPercent);
        }
        else
        {
            CurrentWaterLevelMultiplier = GetWaterLevelMultiplier(CurrentWaterLevelPercent);
        }
    }

    // =========================================================================
    // GetWaterLevelForMonth - Returns water level (0-100) for given month.
    // GetWaterLevelForMonth - Trả về mực nước (0-100) cho tháng được chỉ định.
    // =========================================================================
    public float GetWaterLevelForMonth(int monthIndex)
    {
        if (monthWaterLevels == null || monthWaterLevels.Length < 6)
        {
            // Fallback to 50% if table is invalid.
            // Dùng 50% nếu bảng không hợp lệ.
            return 50f;
        }

        int idx = Mathf.Clamp(monthIndex, 1, 6) - 1;
        return monthWaterLevels[idx];
    }

    // =========================================================================
    // IsRainyMonth - All 6 months (Nov-Apr) are in dry season, so always false.
    // IsRainyMonth - Cả 6 tháng (T11-T4) đều trong mùa khô, luôn trả về false.
    // 
    // Note: This is for reference. Salinity still follows time-based phases.
    // Lưu ý: Đây chỉ để tham khảo. Độ mặn vẫn theo pha dựa trên thời gian.
    // =========================================================================
    public bool IsRainyMonth(int monthIndex)
    {
        // Nov-Apr are all dry season months.
        // T11-T4 đều là tháng mùa khô.
        return false;
    }

    // =========================================================================
    // GetWaterLevelMultiplier - Converts water level % to salinity multiplier.
    // GetWaterLevelMultiplier - Chuyển đổi % mực nước thành hệ số độ mặn.
    // 
    // Higher water = slightly higher salinity multiplier.
    // Mực nước cao hơn = hệ số độ mặn cao hơn một chút.
    // =========================================================================
    public float GetWaterLevelMultiplier(float waterLevelPercent)
    {
        float t = Mathf.Clamp01(waterLevelPercent / 100f);
        return Mathf.Lerp(waterLevelMultiplierMin, waterLevelMultiplierMax, t);
    }

    // =========================================================================
    // LateUpdate - Apply water movement after all Update() calls.
    // LateUpdate - Áp dụng di chuyển nước sau tất cả các Update().
    // =========================================================================
    private void LateUpdate()
    {
        if (_applyMoveThisFrame) StepMove();
    }

    // =========================================================================
    // StepMove - Smoothly interpolates water position over time.
    // StepMove - Nội suy mượt vị trí nước theo thời gian.
    // =========================================================================
    private void StepMove()
    {
        if (!target) return;
        if ((_toPos - _fromPos).sqrMagnitude < 1e-6f) { _moving = false; return; }

        float elapsed = timeRemaining - _phaseStartTime;
        float t = (moveTime <= 0f) ? 1f : Mathf.Clamp01(elapsed / moveTime);
        t = Mathf.SmoothStep(0f, 1f, t);
        target.transform.position = Vector3.Lerp(_fromPos, _toPos, t);
        if (t >= 1f) _moving = false;
    }

    // =========================================================================
    // DisplayTime - Shows elapsed time on HUD clock (MM:SS format).
    // DisplayTime - Hiển thị thời gian lên đồng hồ HUD (định dạng MM:SS).
    // =========================================================================
    void DisplayTime(float t)
    {
        float m = Mathf.FloorToInt(t / 60);
        float s = Mathf.FloorToInt(t % 60);
        clockText.text = $"{m:00}:{s:00}";
    }

    // =========================================================================
    // PlayMusic - Plays background music clip if different from current.
    // PlayMusic - Phát nhạc nền nếu khác với clip hiện tại.
    // =========================================================================
    void PlayMusic(AudioClip clip)
    {
        if (!clip) return;
        if (!_audioSource) _audioSource = GetComponent<AudioSource>();
        if (!_audioSource) return;
        if (_audioSource.clip == clip) return;
        _audioSource.clip = clip;
        _audioSource.Play();
    }

    // =========================================================================
    // StartGame - Called when player clicks "Start" button.
    // StartGame - Được gọi khi người chơi nhấn nút "Bắt đầu".
    // 
    // Resets time, shows gameplay UI, unlocks movement.
    // Reset thời gian, hiển thị UI gameplay, mở khóa di chuyển.
    // =========================================================================
    public void StartGame()
    {
        playGame = true;
        GameActive = true;

        // Reset harvest limits for new game.
        // Reset giới hạn thu hoạch cho game mới.
        David_Fruit.ResetAllHarvestCounts();

        StartMenu.SetActive(false);
        ResultMenu.SetActive(false);
        timeRemaining = 0f;
        // NPC_Talk.SetActive(true);
        ResultDetailsScore.SetActive(false);

        // Log timing configuration for debugging.
        // Ghi log cấu hình thời gian để debug.
        Debug.Log($"[GAME START] monthDuration={monthDuration}s | " +
                  $"Phase1(T11-T1): 0-{monthDuration * 3}s | " +
                  $"Phase2(T2-T3): {monthDuration * 3}-{monthDuration * 5}s | " +
                  $"Phase3(T4): {monthDuration * 5}-{monthDuration * 6}s | " +
                  $"Total: {monthDuration * 6}s = {monthDuration * 6 / 60f}min");

        // Save player start position for potential reset.
        // Lưu vị trí bắt đầu của người chơi để reset nếu cần.
        if (!_playerStartSaved && player)
        {
            _playerStartPos = player.position;
            _playerStartSaved = true;
        }

        // Save water starting position.
        // Lưu vị trí nước bắt đầu.
        if (!_didSnapPointA && target)
        {
            pointA = target.transform.position;
            _didSnapPointA = true;
        }

        _enteredDry = _enteredRainy2 = false;
        _moving = false; 
        _applyMoveThisFrame = false;
        _phaseStartTime = timeRemaining;

        // Unlock player movement.
        // Mở khóa di chuyển người chơi.
        SetMovementLocked(false);
        GameplayUIRoot.SetActive(true);
        GameUIRoot.SetActive(true);

        // EN: Notify GAMA bridge to send tree/spawner data to GAMA server.
        // VI: Thông báo GAMA bridge gửi dữ liệu cây/spawner lên GAMA server.
        if (GAMABridgeLevel1.Instance != null)
        {
            GAMABridgeLevel1.Instance.NotifyGameStarted();
        }

        // EN: Trigger SimulationManager to enter GAME state (same as Level2's GameManager.StartLevel()).
        // VI: Kích hoạt SimulationManager chuyển sang trạng thái GAME (giống GameManager.StartLevel() của Level2).
        SimulationManager sm = FindObjectOfType<SimulationManager>();
        if (sm != null)
        {
            sm.UpdateGameState(GameState.GAME);
            Debug.Log("[RulesoftheGame_VU2_1] SimulationManager set to GAME state.");
        }
    }

    // =========================================================================
    // RestartGame - Reloads scene to reset everything.
    // RestartGame - Reload scene để reset mọi thứ.
    // =========================================================================
    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    // =========================================================================
    // ShowResultDetailsScore - Shows detailed score breakdown panel.
    // ShowResultDetailsScore - Hiển thị bảng chi tiết điểm số.
    // =========================================================================
    public void ShowResultDetailsScore()
    {
        ResultDetailsScore.SetActive(true);
        ResultMenu.SetActive(false);
        InteractiveObjects.SetActive(false);
        UIForVR.SetActive(false);
        if (InteractiveObjects) InteractiveObjects.SetActive(false);
        if (UIForVR) UIForVR.SetActive(false);
    }

    // =========================================================================
    // CloseResultDetailsScore - Returns to main result menu.
    // CloseResultDetailsScore - Quay lại menu kết quả chính.
    // =========================================================================
    public void CloseResultDetailsScore()
    {
        ResultDetailsScore.SetActive(false);
        ResultMenu.SetActive(true);
        InteractiveObjects.SetActive(true);
        UIForVR.SetActive(true);
    }

    // =========================================================================
    // SetPhase - Changes the current season phase.
    // SetPhase - Thay đổi pha mùa hiện tại.
    // 
    // This is the core season transition logic:
    // 1. If Seasonal scoring, settle all farms first
    // 2. Update static Saltwater_Intrusion value
    // 3. Fire OnPhaseChanged event
    // 4. Update all plant salinity displays
    // 
    // Đây là logic chuyển mùa cốt lõi:
    // 1. Nếu là chế độ Seasonal, chốt điểm tất cả farm trước
    // 2. Cập nhật giá trị static Saltwater_Intrusion
    // 3. Bắn sự kiện OnPhaseChanged
    // 4. Cập nhật hiển thị độ mặn của tất cả cây
    // =========================================================================
    private void SetPhase(SeasonPhase phase)
    {
        // Only process if phase actually changed.
        // Chỉ xử lý nếu phase thực sự thay đổi.
        if (_cachedPhase == phase) return;

        // In Seasonal mode, settle all farms before changing phase.
        // Trong chế độ Seasonal, chốt điểm tất cả farm trước khi đổi phase.
        if (InstanceExistsAndSeasonal())
            SettleAllFarmsForNewSeason();

        _cachedPhase = phase;
        _currentPhase = phase;
        
        // Set salinity per phase:
        //   Rainy1 (Phase 1, T11-T1) = 0.0 (low)
        //   Dry    (Phase 2, T2-T3)  = 0.5 (medium)
        //   Rainy2 (Phase 3, T4)     = 1.0 (high)
        // Đặt độ mặn theo giai đoạn:
        //   Rainy1 (GĐ 1, T11-T1) = 0.0 (thấp)
        //   Dry    (GĐ 2, T2-T3)  = 0.5 (trung bình)
        //   Rainy2 (GĐ 3, T4)     = 1.0 (cao)
        if (phase == SeasonPhase.Rainy1)
            Saltwater_Intrusion = 0f;
        else if (phase == SeasonPhase.Dry)
            Saltwater_Intrusion = 0.5f;
        else // Rainy2 = Phase 3
            Saltwater_Intrusion = 1f;
        
        // Notify all listeners about phase change.
        // Thông báo cho tất cả listener về thay đổi phase.
        OnPhaseChanged?.Invoke(_currentPhase);
        FirePhaseChanged(_currentPhase);

        // Update salinity display on all growing plants.
        // Cập nhật hiển thị độ mặn trên tất cả cây đang phát triển.
        foreach (var t in FindObjectsOfType<Thuan_23127_PlantGrowth>())
            t.UpdateSalinityEvent();

        // Update global salinity UI.
        // Cập nhật UI độ mặn toàn cục.
        var gm = Thuan_23127_GameManager.Instance;
        if (gm && gm.jsonReader) gm.jsonReader.UpdateSalinityUI(gm.GetSeasonSalinity());
    }
    
    // =========================================================================
    // SetMovementLocked - Enables/disables VR locomotion.
    // SetMovementLocked - Bật/tắt di chuyển VR.
    // 
    // Used to prevent player movement during menus.
    // Dùng để ngăn di chuyển khi đang ở menu.
    // =========================================================================
    private void SetMovementLocked(bool locked)
    {
        if (moveProvider) moveProvider.enabled = !locked;
        if (lockTurningToo && turnProvider) turnProvider.enabled = !locked;
        if (locomotionSystem) locomotionSystem.enabled = !locked;
    }

    // =========================================================================
    // InstanceExistsAndSeasonal - Checks if using Seasonal scoring mode.
    // InstanceExistsAndSeasonal - Kiểm tra có đang dùng chế độ Seasonal không.
    // =========================================================================
    private bool InstanceExistsAndSeasonal()
    {
        return scoringMode == ScoreFlow.Seasonal;
    }

    // =========================================================================
    // SettleAllFarmsForNewSeason - Forces harvest all plants in all farms.
    // SettleAllFarmsForNewSeason - Ép thu hoạch tất cả cây trong tất cả farm.
    // 
    // Called when season changes in Seasonal mode.
    // Each FarmArea will score remaining objects and clear plots.
    // 
    // Được gọi khi mùa thay đổi trong chế độ Seasonal.
    // Mỗi FarmArea sẽ tính điểm các object còn lại và dọn plot.
    // =========================================================================
    private void SettleAllFarmsForNewSeason()
    {
        var farms = FindObjectsOfType<FarmArea>(true);
        foreach (var a in farms) a.SettleAndClearForNewSeason();
    }
}
