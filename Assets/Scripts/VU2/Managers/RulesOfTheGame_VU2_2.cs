using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// RulesOfTheGame_VU2_2 - Game controller for Level 2 (Rainy season: May-Oct).
// RulesOfTheGame_VU2_2 - Controller game cho Màn 2 (Mùa mưa: T5-T10).
//
// This is the "brain" of Level 2. It controls:
// - Season transitions with REVERSED salinity (High → Medium → Low)
// - Time flow and game duration (6 months: May-October)
// - Weather effects (rain particles, skybox)
// - Water level movement (rising water as rainy season progresses)
// - VR locomotion locking
// - Month system (6 months overlay: May-Oct)
//
// Đây là "bộ não" của Màn 2. Nó điều khiển:
// - Chuyển đổi mùa với độ mặn NGƯỢC (Cao → Trung bình → Thấp)
// - Luồng thời gian và thời lượng game (6 tháng: T5-T10)
// - Hiệu ứng thời tiết (hạt mưa, skybox)
// - Di chuyển mực nước (nước dâng khi mùa mưa tiến triển)
// - Khóa di chuyển VR
// - Hệ thống tháng (6 tháng overlay: T5-T10)
//
// KEY DIFFERENCE FROM VU2_1:
// VU2_1 (Level 1): Nov-Apr, dry season, salinity INCREASES (0→0.5→1.0)
// VU2_2 (Level 2): May-Oct, rainy season, salinity DECREASES (1.0→0.5→0)
//
// KHÁC BIỆT CHÍNH SO VỚI VU2_1:
// VU2_1 (Màn 1): T11-T4, mùa khô, độ mặn TĂNG (0→0.5→1.0)
// VU2_2 (Màn 2): T5-T10, mùa mưa, độ mặn GIẢM (1.0→0.5→0)
// =============================================================================
public class RulesOfTheGame_VU2_2 : MonoBehaviour, IGameRules
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
    //
    // NOTE: These are separate from VU2_1 statics. Only ONE level should
    // be active at a time. GameManager should reference the active level.
    //
    // GHI CHÚ: Các biến này tách biệt với VU2_1. Chỉ MỘT level nên
    // hoạt động tại một thời điểm. GameManager nên tham chiếu level đang chạy.
    // =========================================================================

    // Saltwater intrusion level: 1.0 = start (salty from dry season), 0.0 = end (fresh).
    // Mức xâm nhập mặn: 1.0 = bắt đầu (mặn từ mùa khô), 0.0 = kết thúc (ngọt).
    public static float Saltwater_Intrusion = 0.0f;

    // Current season phase.
    // Pha mùa hiện tại.
    private static SeasonPhase _currentPhase = SeasonPhase.Dry;

    // Event fired when season changes. UI and game objects subscribe to this.
    // Sự kiện được bắn khi mùa thay đổi. UI và game objects đăng ký lắng nghe.
    public static event System.Action<SeasonPhase> OnPhaseChanged;

    // Cached phase to detect changes.
    // Phase được cache để phát hiện thay đổi.
    private static SeasonPhase _cachedPhase = (SeasonPhase)(-1);

    // =========================================================================
    // MONTH SYSTEM (6 months overlay: May - October)
    // HỆ THỐNG THÁNG (6 tháng overlay: Tháng 5 - Tháng 10)
    // =========================================================================

    // Current month (1-6, maps to calendar May-Oct).
    // Tháng hiện tại (1-6, map sang lịch T5-T10).
    public static int CurrentMonthIndex { get; private set; } = 1;

    // Water level percentage (0-100) based on month.
    // Phần trăm mực nước (0-100) dựa trên tháng.
    public static float CurrentWaterLevelPercent { get; private set; } = 20f;

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
    //
    // In Level 2, water RISES as rainy season progresses (pointA → pointB = up).
    // Trong Màn 2, nước DÂNG khi mùa mưa tiến triển (pointA → pointB = lên trên).
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
    public AudioClip rainMusic;       // Music during rainy phases / Nhạc pha mưa
    public AudioClip normalMusic;     // Music during dry/transition phases / Nhạc pha khô/chuyển tiếp
    [FormerlySerializedAs("messageSFX")]
    public AudioClip messageSfx;      // Sound when game ends / Âm thanh khi game kết thúc
    private AudioSource _audioSource;

    // =========================================================================
    // INTERNAL STATE FLAGS
    // CỜ TRẠNG THÁI NỘI BỘ
    // =========================================================================
    private bool _moving, _rainning;
    private bool _enteredPhase2 = false, _enteredPhase3 = false;
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
    //
    // Level 2: 6 months (May-Oct), water levels RISE (opposite of Level 1).
    // Màn 2: 6 tháng (T5-T10), mực nước DÂNG (ngược với Màn 1).
    // =========================================================================
    [Header("Month System (6 months: May-Oct)")]
    [Tooltip("Total game duration in seconds (default 180s = 3 minutes).")]
    public float totalGameDuration = 180f;

    [Tooltip("Duration of each month in seconds (default 30s = 180s/6).")]
    public float monthDuration = 30f;

    [Tooltip("Water level (0-100%) for each month. Index 0 = May, ..., 5 = October.")]
    // Water level table: low in May (end of dry), rising to 80% by October (peak rainy).
    // Bảng mực nước: thấp vào T5 (cuối khô), dâng đến 80% vào T10 (đỉnh mưa).
    // T5: 20%, T6: 30%, T7: 40%, T8: 55%, T9: 70%, T10: 80%
    public float[] monthWaterLevels = new float[6]
    {
        20f, 30f, 40f, 55f, 70f, 80f
    };

    [Tooltip("Salinity multiplier when water level is lowest.")]
    public float waterLevelMultiplierMin = 0.85f;

    [Tooltip("Salinity multiplier when water level is highest.")]
    public float waterLevelMultiplierMax = 1.15f;

    // =========================================================================
    // IGameRules IMPLEMENTATION
    // ==========================================================================
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

    private void OnEnable()
    {
        GameRulesProvider.Register(this);
        LevelManager.OnWaveStepChanged += HandleWaveStepChanged;
    }
    private void OnDisable()
    {
        GameRulesProvider.Unregister(this);
        LevelManager.OnWaveStepChanged -= HandleWaveStepChanged;
    }

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

        // Set default weather (sunny — beginning of rainy season, still dry).
        // Đặt thời tiết mặc định (nắng — đầu mùa mưa, vẫn còn khô).
        if (Weather_Rain) Weather_Rain.SetActive(false);
        if (Rain_image) Rain_image.SetActive(false);
        if (Sun_image) Sun_image.SetActive(true);
        if (NPC_Talk) NPC_Talk.SetActive(false);
        PlayMusic(normalMusic);

        _moving = false;
        _enteredPhase2 = _enteredPhase3 = false;
        _applyMoveThisFrame = false;

        // Lock player movement until game starts.
        // Khóa di chuyển người chơi cho đến khi game bắt đầu.
        SetMovementLocked(true);
        if (GameplayUIRoot) GameplayUIRoot.SetActive(true);
        if (GameUIRoot) GameUIRoot.SetActive(true);

        // Force water level values (override Inspector-serialized values).
        // Bắt buộc dùng giá trị mực nước trong code (bỏ qua giá trị Inspector).
        // T5: 20%, T6: 30%, T7: 40%, T8: 55%, T9: 70%, T10: 80%
        monthWaterLevels = new float[] { 20f, 30f, 40f, 55f, 70f, 80f };
    }

    // =========================================================================
    // Update - Main game loop. Runs every frame during gameplay.
    // Update - Vòng lặp game chính. Chạy mỗi frame trong gameplay.
    //
    // TIMELINE (đồng bộ với LevelManager.OnWaveStepChanged):
    //   Preparation  → Phase Rainy2  — Saltwater_Intrusion = 0.0 (mùa mưa)
    //   Defense      → Phase Dry     — Saltwater_Intrusion = 1.0 (mùa khô)
    //   LevelManager.Finished → Game End
    //
    // Update này chỉ lo time/clock/month/water animation.
    // Phase chuyển hoàn toàn event-driven qua HandleWaveStepChanged().
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

        // Drive water animation if active.
        // Đẩy animation nước nếu đang hoạt động.
        if (_moving && target) _applyMoveThisFrame = true;

        // End game when LevelManager finishes all waves.
        // Kết thúc game khi LevelManager hoàn thành tất cả wave.
        if (_levelManager == null) _levelManager = FindObjectOfType<LevelManager>();
        if (_levelManager != null && _levelManager.Finished)
        {
            EndGame();
        }
    }

    // =========================================================================
    // HandleWaveStepChanged - Reacts to LevelManager wave step transitions.
    // HandleWaveStepChanged - Phản ứng với chuyển step của LevelManager.
    // =========================================================================
    private LevelManager _levelManager;
    private Coroutine _intrusionRampCoroutine;

    [Header("Dry Phase Ramp")]
    [Tooltip("Thời gian (giây) để Saltwater_Intrusion ramp 0→1 khi vào Defense. Trong khoảng này nước rút dần; cây chỉ héo khi ramp xong.")]
    public float intrusionRampDuration = 8f;

    private void HandleWaveStepChanged(WaveStep step)
    {
        if (!playGame) return;

        // Stop any in-progress ramp on every transition.
        // Dừng ramp đang chạy mỗi khi chuyển step.
        if (_intrusionRampCoroutine != null)
        {
            StopCoroutine(_intrusionRampCoroutine);
            _intrusionRampCoroutine = null;
        }

        if (step == WaveStep.Preparation)
        {
            // Rainy season: low salinity, rain visuals, water at base level.
            // Mùa mưa: mặn thấp, hiệu ứng mưa, nước ở mức gốc.
            Saltwater_Intrusion = 0f;
            SetPhase(SeasonPhase.Rainy2);
            _rainning = true;

            if (Weather_Rain) Weather_Rain.SetActive(true);
            if (Rain_image) Rain_image.SetActive(true);
            if (Sun_image) Sun_image.SetActive(false);
            if (Skybox_Rain) RenderSettings.skybox = Skybox_Rain;
            DynamicGI.UpdateEnvironment();
            PlayMusic(rainMusic);

            _moving = false;
        }
        else // WaveStep.Defense
        {
            // Dry season: visuals đổi NGAY, nhưng intrusion ramp dần để nước rút trước
            // → cây chỉ héo khi nước thực sự đã rút (qua threshold).
            _rainning = false;

            if (Weather_Rain) Weather_Rain.SetActive(false);
            if (Rain_image) Rain_image.SetActive(false);
            if (Sun_image) Sun_image.SetActive(true);
            if (Skybox_Sun) RenderSettings.skybox = Skybox_Sun;
            DynamicGI.UpdateEnvironment();
            PlayMusic(normalMusic);

            // Start water rising animation across the entire Defense step.
            // Bắt đầu animation nước dâng trải dài toàn bộ step Defense.
            _phaseStartTime = timeRemaining;
            _fromPos = target ? target.transform.position : pointA;
            _toPos = pointB;
            if (_levelManager == null) _levelManager = FindObjectOfType<LevelManager>();
            if (_levelManager != null) moveTime = _levelManager.CurrentWaveStepTime;
            _moving = true;

            // Ramp Saltwater_Intrusion 0→1 dần dần. SubsidenceManager sẽ lerp seasonY
            // theo intrusion → nước SF_Water_Sea (1) rút từ từ. SetPhase(Dry) chỉ fire
            // khi intrusion vượt ngưỡng → cây héo SAU khi nước đã rút.
            _intrusionRampCoroutine = StartCoroutine(RampIntrusionToDry());
        }
    }

    private System.Collections.IEnumerator RampIntrusionToDry()
    {
        float duration = Mathf.Max(0.1f, intrusionRampDuration);
        float startIntrusion = Saltwater_Intrusion;
        float elapsed = 0f;

        // Ramp Saltwater_Intrusion 0→1 dần. SubsidenceManager đọc giá trị này
        // → SF_Water_Sea (1) sẽ rút từ rainyWaterY xuống dryWaterY song song.
        // CHƯA fire SetPhase(Dry) trong khi ramp → cây chưa nhận OnPhaseChanged → chưa héo.
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Saltwater_Intrusion = Mathf.Lerp(startIntrusion, 1f, t);
            yield return null;
        }

        // Sau khi nước đã rút xong → fire SetPhase(Dry) → cây mới héo.
        Saltwater_Intrusion = 1f;
        SetPhase(SeasonPhase.Dry);
        _intrusionRampCoroutine = null;
    }

    // =========================================================================
    // EndGame - Finalize gameplay and show result UI.
    // EndGame - Kết thúc gameplay và hiện thị UI kết quả.
    // =========================================================================
    private void EndGame()
    {
        if (!playGame) return;

        _rainning = false;
        if (Weather_Rain) Weather_Rain.SetActive(false);
        if (Rain_image) Rain_image.SetActive(false);
        if (Sun_image) Sun_image.SetActive(true);
        if (Skybox_Sun) RenderSettings.skybox = Skybox_Sun;
        DynamicGI.UpdateEnvironment();
        PlayMusic(normalMusic);

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

        SetMovementLocked(true);

        if (_audioSource && messageSfx) _audioSource.PlayOneShot(messageSfx);
        if (ResultMenu) ResultMenu.SetActive(true);
        if (GameplayUIRoot) GameplayUIRoot.SetActive(false);
        if (GameUIRoot) GameUIRoot.SetActive(false);
    }

    // =========================================================================
    // UpdateMonthAndWaterLevel - Updates month index and water level from time.
    // UpdateMonthAndWaterLevel - Cập nhật tháng và mực nước theo thời gian.
    //
    // This runs independently of the season system to provide finer control.
    // In Level 2, water level INCREASES over time (opposite of Level 1).
    // Chạy độc lập với hệ thống mùa để cung cấp kiểm soát chi tiết hơn.
    // Trong Màn 2, mực nước TĂNG theo thời gian (ngược với Màn 1).
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

        // Smoothly interpolate water level between months for gradual increase.
        // Nội suy mượt mực nước giữa các tháng để tăng dần theo thời gian.
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
    // IsRainyMonth - Determines if a month is in the rainy period.
    // IsRainyMonth - Xác định tháng có thuộc giai đoạn mưa không.
    //
    // In Level 2 (May-Oct), months 4-6 (Aug-Oct) are peak rainy.
    // Trong Màn 2 (T5-T10), tháng 4-6 (T8-T10) là đỉnh mưa.
    // =========================================================================
    public bool IsRainyMonth(int monthIndex)
    {
        // Month 4+ (August onwards) = peak rainy season.
        // Tháng 4+ (từ tháng 8 trở đi) = đỉnh mùa mưa.
        return monthIndex >= 4;
    }

    // =========================================================================
    // GetWaterLevelMultiplier - Converts water level % to salinity multiplier.
    // GetWaterLevelMultiplier - Chuyển đổi % mực nước thành hệ số độ mặn.
    //
    // In Level 2: Higher water = LOWER salinity multiplier (rain dilutes salt).
    // Trong Màn 2: Mực nước cao hơn = hệ số độ mặn THẤP hơn (mưa pha loãng muối).
    // =========================================================================
    public float GetWaterLevelMultiplier(float waterLevelPercent)
    {
        float t = Mathf.Clamp01(waterLevelPercent / 100f);
        // REVERSED from VU2_1: high water → low multiplier (more rain = less salt).
        // NGƯỢC với VU2_1: nước cao → hệ số thấp (mưa nhiều = ít muối).
        return Mathf.Lerp(waterLevelMultiplierMax, waterLevelMultiplierMin, t);
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
        if (NPC_Talk) NPC_Talk.SetActive(true);
        ResultDetailsScore.SetActive(false);

        // Log timing configuration for debugging.
        // Ghi log cấu hình thời gian để debug.
        if (_levelManager == null) _levelManager = FindObjectOfType<LevelManager>();
        if (_levelManager != null)
        {
            Debug.Log($"[GAME START - LEVEL 2] Timeline đồng bộ với LevelManager | " +
                      $"Preparation = Rainy (Salinity=0.0) | " +
                      $"Defense = Dry (Salinity=1.0) | " +
                      $"End khi LevelManager.Finished");
        }
        else
        {
            Debug.LogWarning("[GAME START - LEVEL 2] Không tìm thấy LevelManager — phase sẽ không tự chuyển.");
        }

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

        _enteredPhase2 = _enteredPhase3 = false;
        _moving = false;
        _applyMoveThisFrame = false;
        _phaseStartTime = timeRemaining;

        // Sync initial phase from LevelManager's current wave step
        // (vì OnWaveStepChanged có thể đã fire trước khi VU2_2 subscribe).
        if (_levelManager != null)
        {
            HandleWaveStepChanged(_levelManager.CurrentWaveStep);
        }
        else
        {
            // Fallback: assume Preparation/Rainy at game start.
            HandleWaveStepChanged(WaveStep.Preparation);
        }

        // Unlock player movement.
        // Mở khóa di chuyển người chơi.
        SetMovementLocked(false);
        if (GameplayUIRoot) GameplayUIRoot.SetActive(true);
        if (GameUIRoot) GameUIRoot.SetActive(true);
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
        if (InteractiveObjects) InteractiveObjects.SetActive(true);
        if (UIForVR) UIForVR.SetActive(true);
    }

    // =========================================================================
    // SetPhase - Changes the current season phase.
    // SetPhase - Thay đổi pha mùa hiện tại.
    //
    // This is the core season transition logic (REVERSED salinity):
    // 1. If Seasonal scoring, settle all farms first
    // 2. Update static Saltwater_Intrusion value (1.0 → 0.5 → 0.0)
    // 3. Fire OnPhaseChanged event
    // 4. Update all plant salinity displays
    //
    // Đây là logic chuyển mùa cốt lõi (độ mặn NGƯỢC):
    // 1. Nếu là chế độ Seasonal, chốt điểm tất cả farm trước
    // 2. Cập nhật giá trị static Saltwater_Intrusion (1.0 → 0.5 → 0.0)
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

        // Set salinity per phase (REVERSED from VU2_1):
        //   Dry    (Phase 1, T5-T7)  = 1.0 (high — residual from dry season)
        //   Rainy1 (Phase 2, T8-T9)  = 0.5 (medium — rains diluting)
        //   Rainy2 (Phase 3, T10)    = 0.0 (low — peak rainfall)
        // Đặt độ mặn theo giai đoạn (NGƯỢC với VU2_1):
        //   Dry    (GĐ 1, T5-T7)  = 1.0 (cao — tồn dư từ mùa khô)
        //   Rainy1 (GĐ 2, T8-T9)  = 0.5 (trung bình — mưa pha loãng)
        //   Rainy2 (GĐ 3, T10)    = 0.0 (thấp — đỉnh mưa)
        if (phase == SeasonPhase.Dry)
            Saltwater_Intrusion = 1f;
        else if (phase == SeasonPhase.Rainy1)
            Saltwater_Intrusion = 0.5f;
        else // Rainy2 = Phase 3
            Saltwater_Intrusion = 0f;

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
