using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// Thuan_23127_PlantGrowth - Manages the complete lifecycle of plants/animals/fish.
// Thuan_23127_PlantGrowth - Quản lý toàn bộ vòng đời của cây/động vật/cá.
// 
// This is the core component attached to every growing entity in the game.
// It handles: Growing → Ready → Harvesting → Done states.
// 
// Đây là component cốt lõi được gắn vào mọi thực thể đang phát triển trong game.
// Nó xử lý: Các trạng thái Đang phát triển → Sẵn sàng → Đang thu hoạch → Hoàn thành.
// 
// KEY RESPONSIBILITIES:
// - Growth progress over time
// - Salinity-based score calculation
// - Visual feedback (progress bar, warning icons)
// - Animation control based on salinity stress
// - Event broadcasting for UI/FarmArea
// 
// TRÁCH NHIỆM CHÍNH:
// - Tiến độ phát triển theo thời gian
// - Tính điểm dựa trên độ mặn
// - Phản hồi trực quan (thanh tiến độ, icon cảnh báo)
// - Điều khiển animation theo stress độ mặn
// - Phát sự kiện cho UI/FarmArea
// =============================================================================
public class Thuan_23127_PlantGrowth : MonoBehaviour
{
    // =========================================================================
    // UI REFERENCES
    // THAM CHIẾU UI
    // =========================================================================
    [Header("Progress UI (shared for grow & harvest)")]
    // Progress bar fill image (0-1 fill amount).
    // Image fill của thanh tiến độ (fill amount 0-1).
    public Image progressFill;
    
    // Text showing percentage (e.g., "75%").
    // Text hiển thị phần trăm (ví dụ: "75%").
    public TextMeshProUGUI progressPercentText;

    [Header("UI (Salinity)")]
    // Displays current salinity for this plant.
    // Hiển thị độ mặn hiện tại cho cây này.
    public TextMeshProUGUI salinityText;
    
    [Header("Icon warningIcon")]
    // Warning icon shown when salinity exceeds threshold.
    // Icon cảnh báo hiển thị khi độ mặn vượt ngưỡng.
    public Image warningIcon;
    
    // Tracks if warning has been evaluated once (for initial snap).
    // Theo dõi nếu cảnh báo đã được đánh giá lần đầu (cho snap ban đầu).
    private bool _warningEvaluatedOnce = false;

    // =========================================================================
    // XR INTERACTION
    // TƯƠNG TÁC XR
    // =========================================================================
    [Header("XR (Harvest optional)")]
    // XR interactable for VR harvest interaction.
    // XR interactable cho tương tác thu hoạch VR.
    public XRSimpleInteractable harvestInteractable;

    // =========================================================================
    // TIMING CONFIGURATION
    // CẤU HÌNH THỜI GIAN
    // =========================================================================
    [Header("Timing")]
    // Delay before destroying object after harvest (allows visual feedback).
    // Thời gian chờ trước khi hủy object sau thu hoạch (cho phép phản hồi trực quan).
    [SerializeField] private float destroyDelaySeconds = 30f;

    // =========================================================================
    // LIFECYCLE EVENTS
    // CÁC SỰ KIỆN VÒNG ĐỜI
    // 
    // These events allow FarmArea, HUD, and other systems to react to changes.
    // Các sự kiện này cho phép FarmArea, HUD, và các hệ thống khác phản ứng với thay đổi.
    // =========================================================================
    
    // Plant states: Growing → Ready → Harvesting → Done
    // Các trạng thái cây: Đang phát triển → Sẵn sàng → Đang thu hoạch → Hoàn thành
    public enum State { Growing, Ready, Harvesting, Done }
    
    // Fired when growth/harvest progress changes (0.0 to 1.0).
    // Bắn khi tiến độ phát triển/thu hoạch thay đổi (0.0 đến 1.0).
    public event Action<float> OnProgressChanged;
    
    // Fired when salinity updates (current value, threshold value).
    // Bắn khi độ mặn cập nhật (giá trị hiện tại, giá trị ngưỡng).
    public event Action<float, float> OnSalinityChanged;
    
    // Fired when lifecycle state changes.
    // Bắn khi trạng thái vòng đời thay đổi.
    public event Action<State> OnStateChanged;
    
    // Fired just before object is destroyed (for cleanup).
    // Bắn ngay trước khi object bị hủy (để dọn dẹp).
    public event Action OnAboutToDestroy;
    
    // Fired when harvested with the actual points earned.
    // Bắn khi thu hoạch với số điểm thực tế kiếm được.
    public event Action<int> OnHarvested;

    // =========================================================================
    // DATA REFERENCES (only one is set based on entity type)
    // THAM CHIẾU DỮ LIỆU (chỉ một cái được đặt dựa trên loại thực thể)
    // =========================================================================
    private Plant  _plantData;   // For plants (rice, durian, coconut, etc.)
    private Animal _animalData;  // For livestock (chicken, duck, etc.)
    private Fish   _fishData;    // For aquatic (fish, shrimp, etc.)

    // =========================================================================
    // RUNTIME STATE
    // TRẠNG THÁI RUNTIME
    // =========================================================================
    
    // Total growth time in seconds.
    // Tổng thời gian phát triển tính bằng giây.
    private float _growTotal;
    
    // Elapsed growth time.
    // Thời gian phát triển đã trôi qua.
    private float _growElapsed;
    
    // Time to complete harvest animation.
    // Thời gian hoàn thành animation thu hoạch.
    private float _harvestTime;
    
    // Base economic value (points before salinity adjustment).
    // Giá trị kinh tế gốc (điểm trước khi điều chỉnh độ mặn).
    private int _econ;

    // State flags.
    // Các cờ trạng thái.
    private bool _growing, _ready;
    private bool _harvested;
    private bool _harvesting;
    private Coroutine _harvestCo;

    // Owner references.
    // Tham chiếu đến chủ sở hữu.
    private FarmArea _ownerArea;
    private int _ownerIndex = -1;
    private Thuan_23127_JsonReader _jsonReader;

    // =========================================================================
    // SALINITY PROVIDER
    // PROVIDER ĐỘ MẶN
    // 
    // FarmArea injects this function so plant uses area-specific salinity.
    // FarmArea tiêm hàm này để cây dùng độ mặn riêng của vùng.
    // =========================================================================
    private Func<float> _salinityProvider;
    
    // =========================================================================
    // ANIMATION CONFIGURATION
    // CẤU HÌNH ANIMATION
    // =========================================================================
    [Header("Anim (optional)")]
    // Animator component (auto-found if not assigned).
    // Component Animator (tự động tìm nếu chưa gán).
    public Animator plantAnimator;
    
    // Animation state names for healthy/stressed plants.
    // Tên các state animation cho cây khỏe mạnh/bị stress.
    public string animGood = "Tree_Good";
    public string animBad  = "Tree_Bad";
    
    // Delay before playing "bad" animation when salinity exceeds threshold.
    // Thời gian chờ trước khi phát animation "xấu" khi độ mặn vượt ngưỡng.
    public float salinityBadDelay = 10f;
    
    // Runtime animation state.
    // Trạng thái animation runtime.
    private bool _isOverSalt = false;
    private bool _badAnimPlayedThisSaltPeriod = false;
    private Coroutine _salinityBadCo;

    // Event for health description text changes.
    // Sự kiện cho thay đổi text mô tả sức khỏe.
    public event Action<string> OnHealthTextChanged;

    // =========================================================================
    // SetSalinityProvider - Allows FarmArea to inject salinity source.
    // SetSalinityProvider - Cho phép FarmArea tiêm nguồn độ mặn.
    // 
    // This enables each plant to use its zone's specific salinity value.
    // Điều này cho phép mỗi cây dùng giá trị độ mặn riêng của vùng.
    // =========================================================================
    public void SetSalinityProvider(Func<float> provider) { _salinityProvider = provider; }

    // =========================================================================
    // Init (Plant) - Initialize from Plant data and start lifecycle.
    // Init (Plant) - Khởi tạo từ dữ liệu Plant và bắt đầu vòng đời.
    // 
    // Called by: FarmArea.PlantInternal() when planting a crop.
    // Được gọi bởi: FarmArea.PlantInternal() khi trồng cây.
    // =========================================================================
    public void Init(Plant data, FarmArea area, int plotIndex, Thuan_23127_JsonReader reader)
    {
        _plantData  = data;  _animalData = null; _fishData = null;
        _ownerArea  = area;  _ownerIndex = plotIndex; _jsonReader = reader;

        _growTotal   = Mathf.Max(0f, data.growth_time);
        _harvestTime = (data.harvest_time > 0f) ? data.harvest_time : 2f;
        _econ        = Mathf.Max(0, data.economic_benefits);
        Debug.Log($"Plant Init - ID: {data.id}, Growth Time: {data.growth_time}");

        CommonInitAndStart();
        UpdateSalinityEvent();
    }

    // =========================================================================
    // Init (Animal) - Initialize from Animal data.
    // Init (Animal) - Khởi tạo từ dữ liệu Animal.
    // 
    // Used for livestock like chickens, ducks.
    // Dùng cho vật nuôi như gà, vịt.
    // =========================================================================
    public void Init(Animal data, FarmArea area, int plotIndex, Thuan_23127_JsonReader reader)
    {
        _plantData  = null;  _animalData = data; _fishData = null;
        _ownerArea  = area;  _ownerIndex = plotIndex; _jsonReader = reader;

        _growTotal   = Mathf.Max(0f, data.growth_time);
        _harvestTime = (data.harvest_time > 0f) ? data.harvest_time : 2f;
        _econ = Mathf.Max(0, data.economic_benefits);
        Debug.Log($"Animal Init - ID: {data.id}, Growth Time: {data.growth_time}");

        CommonInitAndStart();
        UpdateSalinityEvent();
    }

    // =========================================================================
    // Init (Fish) - Initialize from Fish data.
    // Init (Fish) - Khởi tạo từ dữ liệu Fish.
    // 
    // Used for aquatic animals like fish, shrimp.
    // Dùng cho động vật thủy sản như cá, tôm.
    // =========================================================================
    public void Init(Fish data, FarmArea area, int plotIndex, Thuan_23127_JsonReader reader)
    {
        _plantData  = null;  _animalData = null; _fishData = data;
        _ownerArea  = area;  _ownerIndex = plotIndex; _jsonReader = reader;

        _growTotal   = Mathf.Max(0f, data.growth_time);
        _harvestTime = (data.harvest_time > 0f) ? data.harvest_time : 2f;
        _econ        = Mathf.Max(0, data.economic_benefits);
        Debug.Log($"Fish Init - ID: {data.id}, Growth Time: {data.growth_time}");
        CommonInitAndStart();
        UpdateSalinityEvent();
    }

    // =========================================================================
    // CurrentSalinity - Gets current salinity for scoring/display.
    // CurrentSalinity - Lấy độ mặn hiện tại để tính điểm/hiển thị.
    // 
    // Priority: 1) Injected provider, 2) Global GameManager.
    // Ưu tiên: 1) Provider được tiêm, 2) GameManager toàn cục.
    // =========================================================================
    private float CurrentSalinity()
    {
        if (_salinityProvider != null) return Mathf.Max(0f, _salinityProvider());
        var gm = Thuan_23127_GameManager.Instance;
        return gm ? gm.GetSeasonSalinity() : 0f;
    }

    // =========================================================================
    // UpdateSalinityEvent - Fires salinity update event for HUD.
    // UpdateSalinityEvent - Bắn sự kiện cập nhật độ mặn cho HUD.
    // 
    // Called by: FarmArea on phase change, internal UI updates.
    // Được gọi bởi: FarmArea khi đổi phase, cập nhật UI nội bộ.
    // =========================================================================
    public void UpdateSalinityEvent()
    {
        float current = CurrentSalinity();
        float threshold = 0f;
        if (_plantData  != null) threshold = _plantData.salinity_threshold;
        if (_animalData != null) threshold = _animalData.salinity_threshold;
        if (_fishData   != null) threshold = _fishData.salinity_threshold;
        
        OnSalinityChanged?.Invoke(current, threshold);
        
        // Check for animation triggers.
        // Kiểm tra trigger animation.
        EvaluateSalinityEffects(current, threshold);
        
        // Update health description text.
        // Cập nhật text mô tả sức khỏe.
        EmitHealthDescription(current, threshold);
    }

    // =========================================================================
    // CommonInitAndStart - Shared initialization logic for all entity types.
    // CommonInitAndStart - Logic khởi tạo chung cho tất cả loại thực thể.
    // 
    // Sets up XR interaction, resets state, and starts growth coroutine.
    // Thiết lập tương tác XR, reset trạng thái, và bắt đầu coroutine phát triển.
    // =========================================================================
    private void CommonInitAndStart()
    {
        // Setup XR harvest interaction.
        // Thiết lập tương tác thu hoạch XR.
        if (!harvestInteractable) harvestInteractable = GetComponent<XRSimpleInteractable>();
        if (harvestInteractable)
        {
            harvestInteractable.selectEntered.RemoveAllListeners();
            harvestInteractable.selectEntered.AddListener(_ => { TryStartHarvest(); });
        }

        // Reset all state flags.
        // Reset tất cả cờ trạng thái.
        _growing = true; _ready = false; _harvested = false; _harvesting = false;

        OnProgressChanged?.Invoke(0f);
        OnStateChanged?.Invoke(State.Growing);
        StartCoroutine(CoGrow());
    }

    // =========================================================================
    // CoGrow - Coroutine for growth phase.
    // CoGrow - Coroutine cho pha phát triển.
    // 
    // Progress increases from 0 to 1 over _growTotal seconds.
    // When complete, transitions to Ready state.
    // 
    // Tiến độ tăng từ 0 đến 1 trong _growTotal giây.
    // Khi hoàn thành, chuyển sang trạng thái Ready.
    // =========================================================================
    private IEnumerator CoGrow()
    {
        _growElapsed = 0f;

        // Instant growth if time is 0.
        // Phát triển tức thì nếu thời gian là 0.
        if (_growTotal <= 0f)
        {
            OnProgressChanged?.Invoke(1f);
            UpdateUI(1f);
            _growing = false; _ready = true;
            OnStateChanged?.Invoke(State.Ready);
            TryStartHarvest();
            yield break;
        }

        // Gradual growth over time.
        // Phát triển dần theo thời gian.
        while (_growElapsed < _growTotal) {
            _growElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_growElapsed / _growTotal);
            OnProgressChanged?.Invoke(t);
            UpdateUI(t);
            yield return null;
        }

        _growing = false; _ready = true;
        OnStateChanged?.Invoke(State.Ready);
        TryStartHarvest();
    }

    // =========================================================================
    // Input handlers for harvest.
    // Các handler input cho thu hoạch.
    // =========================================================================
    private void OnMouseDown() { TryStartHarvest(); }
    public  void HarvestNow()  { TryStartHarvest(); }

    // =========================================================================
    // TryStartHarvest - Validates conditions and starts harvest.
    // TryStartHarvest - Kiểm tra điều kiện và bắt đầu thu hoạch.
    // 
    // Conditions: Must be ready, not already harvested, not currently harvesting.
    // Điều kiện: Phải sẵn sàng, chưa thu hoạch, không đang thu hoạch.
    // =========================================================================
    private void TryStartHarvest()
    {
        if (!_ready || _harvested || _harvesting) return;
        if (_harvestCo != null) StopCoroutine(_harvestCo);
        _harvestCo = StartCoroutine(CoHarvest());
    }

    // =========================================================================
    // CoHarvest - Coroutine for harvest animation phase.
    // CoHarvest - Coroutine cho pha animation thu hoạch.
    // 
    // Shows progress bar filling up during harvest time.
    // Hiển thị thanh tiến độ đầy lên trong thời gian thu hoạch.
    // =========================================================================
    private System.Collections.IEnumerator CoHarvest()
    {
        _harvesting = true;
        OnStateChanged?.Invoke(State.Harvesting);
        
        // Disable interaction during harvest.
        // Tắt tương tác trong khi thu hoạch.
        if (harvestInteractable) harvestInteractable.enabled = false;

        OnProgressChanged?.Invoke(0f);
        float e = 0f, h = (_harvestTime > 0f) ? _harvestTime : 2f;
        
        // Progress through harvest time.
        // Tiến hành qua thời gian thu hoạch.
        while (e < h)
        {
            e += Time.deltaTime;
            var tt = Mathf.Clamp01(e / h);
            OnProgressChanged?.Invoke(tt);
            UpdateUI(tt);
            yield return null;
        }

        FinalizeHarvest();
    }

    // =========================================================================
    // FinalizeHarvest - Calculates score, fires events, schedules destruction.
    // FinalizeHarvest - Tính điểm, bắn sự kiện, lên lịch hủy.
    // 
    // This is where the actual score is calculated and added to GameManager.
    // Đây là nơi điểm thực tế được tính và cộng vào GameManager.
    // =========================================================================
    private void FinalizeHarvest()
    {
        if (_harvested) return;
        _harvested = true; _ready = false; _harvesting = false;

        // Calculate score adjusted by salinity.
        // Tính điểm được điều chỉnh theo độ mặn.
        int points = AdjustBySalinity(_econ);

        // SPECIAL RULES for specific fish types.
        // QUY TẮC ĐẶC BIỆT cho các loại cá cụ thể.
        
        // Shrimp (ID 5,6): Cannot survive in fresh water.
        // Tôm (ID 5,6): Không thể sống trong nước ngọt.
        if (_fishData != null && (_fishData.id == 5 || _fishData.id == 6))
        {
            if (_ownerArea && _ownerArea.waterType == WaterType.Fresh)
            {
                points = 0; // Zero points for wrong placement.
            }
        }

        // Red tilapia (ID 2): Cannot survive in salt water.
        // Cá điêu hồng (ID 2): Không thể sống trong nước mặn.
        if (_fishData != null && (_fishData.id == 2))
        {
            if (_ownerArea && _ownerArea.waterType == WaterType.Salt)
            {
                points = 0;
            }
        }

        // Add score to GameManager.
        // Cộng điểm vào GameManager.
        var gm = Thuan_23127_GameManager.Instance;
        if (gm) gm.AddScore(points);

        // Notify listeners of harvest.
        // Thông báo cho listener về việc thu hoạch.
        OnHarvested?.Invoke(points);

        // Schedule destruction after delay.
        // Lên lịch hủy sau thời gian chờ.
        StartCoroutine(CoDestroyAfter(destroyDelaySeconds));
        OnStateChanged?.Invoke(State.Done);
    }

    // =========================================================================
    // AdjustBySalinity - Adjusts base score based on salinity conditions.
    // AdjustBySalinity - Điều chỉnh điểm gốc dựa trên điều kiện độ mặn.
    // 
    // First tries table-based scoring, then falls back to threshold formula.
    // Đầu tiên thử tính điểm theo bảng, rồi fallback về công thức ngưỡng.
    // =========================================================================
    private int AdjustBySalinity(int baseValue)
    {
        // Try table-based scoring first.
        // Thử tính điểm theo bảng trước.
        int tableScore = GetTableBasedScore();
        if (tableScore >= 0)
        {
            return tableScore;
        }
        
        // Fallback: Threshold-based formula.
        // Fallback: Công thức dựa trên ngưỡng.
        // If salinity > threshold: score = baseValue * (threshold / salinity)
        // Nếu độ mặn > ngưỡng: điểm = điểm gốc * (ngưỡng / độ mặn)
        float t = 0f;
        if      (_plantData  != null) t = _plantData.salinity_threshold;
        else if (_animalData != null) t = _animalData.salinity_threshold;
        else if (_fishData   != null) t = _fishData.salinity_threshold;

        float s = CurrentSalinity();
        if (t <= 0f || s <= t) return baseValue;

        float ratio = Mathf.Clamp01(t / s);
        return Mathf.Max(0, Mathf.RoundToInt(baseValue * ratio));
    }
    
    // =========================================================================
    // GetTableBasedScore - Returns score from fixed Zone × Season table.
    // GetTableBasedScore - Trả về điểm từ bảng cố định Vùng × Mùa.
    // 
    // Returns -1 if entity not in table (use threshold formula instead).
    // Trả về -1 nếu thực thể không có trong bảng (dùng công thức ngưỡng thay thế).
    // 
    // SCORE TABLE:
    // | Type     | Fresh+Rainy | Fresh+Dry | Salt+Rainy | Salt+Dry |
    // |----------|-------------|-----------|------------|----------|
    // | Durian   | 15          | 10        | 6          | 4        |
    // | Coconut  | 12          | 8         | 8          | 5        |
    // | Fish     | 1           | 2         | 3          | 4        |
    // | Chicken  | 85%         | 80%       | 75%        | 60%      |
    // =========================================================================
    private int GetTableBasedScore()
    {
        // Determine zone and season.
        // Xác định vùng và mùa.
        bool isFresh = (_ownerArea != null && _ownerArea.waterType == WaterType.Fresh);
        bool isRainy = (GameRulesProvider.Saltwater_Intrusion < 1f);
        
        // Durian (Plant ID = 1) — 3-phase scoring, zone-independent.
        // Sầu riêng (Plant ID = 1) — tính điểm 3 giai đoạn, không phụ thuộc vùng.
        // Phase 1 (Intrusion=0.0): 150 pts | Phase 2 (Intrusion=0.5): 75 pts | Phase 3 (Intrusion=1.0): 0 pts
        if (_plantData != null && _plantData.id == 1)
        {
            float intrusion = GameRulesProvider.Saltwater_Intrusion;
            if (intrusion < 0.1f) return 150;  // Phase 1 — mùa mưa
            if (intrusion < 1f)   return 75;   // Phase 2 — chuyển tiếp
            return 0;                          // Phase 3 — mùa khô hoàn toàn
        }
        
        // Coconut (Plant ID = 10).
        // Dừa (Plant ID = 10).
        if (_plantData != null && _plantData.id == 10)
        {
            if (isFresh) return isRainy ? 12 : 8;
            else         return isRainy ? 8 : 5;
        }
        
        // Rice (Plant ID = 11) — 3-phase scoring, zone-independent.
        // Lúa (Plant ID = 11) — tính điểm 3 giai đoạn, không phụ thuộc vùng.
        // Phase 1 (Intrusion=0.0): 60 pts | Phase 2 (Intrusion=0.5): 30 pts | Phase 3 (Intrusion=1.0): 0 pts
        if (_plantData != null && _plantData.id == 11)
        {
            float intrusion = GameRulesProvider.Saltwater_Intrusion;
            if (intrusion < 0.1f) return 60;   // Phase 1 — mùa mưa
            if (intrusion < 1f)   return 30;   // Phase 2 — chuyển tiếp
            return 0;                          // Phase 3 — mất trắng
        }
        
        // All fish types.
        // Tất cả loại cá.
        if (_fishData != null)
        {
            if (isFresh) return isRainy ? 1 : 2;
            else         return isRainy ? 3 : 4;
        }
        
        // Chicken (Livestock ID = 3) - percentage of base value.
        // Gà (Livestock ID = 3) - phần trăm của giá trị gốc.
        if (_animalData != null && _animalData.id == 3)
        {
            float percent;
            if (isFresh) percent = isRainy ? 0.85f : 0.80f;
            else         percent = isRainy ? 0.75f : 0.60f;
            
            return Mathf.RoundToInt(_econ * percent);
        }
        
        // Not in table - return -1 to use threshold formula.
        // Không có trong bảng - trả về -1 để dùng công thức ngưỡng.
        return -1;
    }

    // =========================================================================
    // CoDestroyAfter - Waits, notifies FarmArea, then destroys object.
    // CoDestroyAfter - Chờ, thông báo FarmArea, rồi hủy object.
    // =========================================================================
    private System.Collections.IEnumerator CoDestroyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        OnAboutToDestroy?.Invoke();
        if (_ownerArea) _ownerArea.FreePlot(_ownerIndex);
        Destroy(gameObject);
    }

    // =========================================================================
    // UpdateUI - Updates local UI elements on the prefab.
    // UpdateUI - Cập nhật các UI element cục bộ trên prefab.
    // 
    // Called every frame during growth/harvest.
    // Được gọi mỗi frame trong khi phát triển/thu hoạch.
    // =========================================================================
    private void UpdateUI(float t)
    {
        // Update progress bar.
        // Cập nhật thanh tiến độ.
        if (progressFill) progressFill.fillAmount = t;
        if (progressPercentText) progressPercentText.text = Mathf.RoundToInt(t * 100f) + "%";

        // Update salinity display.
        // Cập nhật hiển thị độ mặn.
        var currentSalinity = CurrentSalinity();
        if (salinityText) salinityText.text = currentSalinity.ToString("F2") + " ‰";

        // Get threshold for salinity check.
        // Lấy ngưỡng để kiểm tra độ mặn.
        float threshold = 0f;
        if (_plantData  != null) threshold = _plantData.salinity_threshold;
        if (_animalData != null) threshold = _animalData.salinity_threshold;
        if (_fishData   != null) threshold = _fishData.salinity_threshold;

        // Evaluate salinity effects (animation triggers).
        // Đánh giá hiệu ứng độ mặn (trigger animation).
        EvaluateSalinityEffects(currentSalinity, threshold);
        EmitHealthDescription(currentSalinity, threshold);
        
        // Update warning icon with fade effect.
        // Cập nhật icon cảnh báo với hiệu ứng fade.
        if (warningIcon)
        {
            float targetAlpha = currentSalinity > threshold ? 1f : 0f;

            var cg = warningIcon.GetComponent<CanvasGroup>();
            if (!cg) cg = warningIcon.gameObject.AddComponent<CanvasGroup>();

            // Snap immediately on first evaluation.
            // Snap ngay lập tức khi đánh giá lần đầu.
            if (!_warningEvaluatedOnce)
            {
                _warningEvaluatedOnce = true;

                if (targetAlpha > 0f && !warningIcon.gameObject.activeSelf)
                    warningIcon.gameObject.SetActive(true);

                cg.alpha = targetAlpha;
                bool show = targetAlpha > 0f;
                warningIcon.enabled = show;
                warningIcon.raycastTarget = show;

                if (!show && warningIcon.gameObject.activeSelf)
                    warningIcon.gameObject.SetActive(false);

                return;
            }

            // Smooth fade for subsequent updates.
            // Fade mượt cho các lần cập nhật sau.
            if (targetAlpha > 0f && !warningIcon.gameObject.activeSelf)
            {
                warningIcon.gameObject.SetActive(true);
                if (cg.alpha <= 0f) cg.alpha = 0f;
            }

            const float fadeDuration = 0.25f;
            cg.alpha = Mathf.MoveTowards(
                cg.alpha,
                targetAlpha,
                (fadeDuration > 0f ? Time.deltaTime / fadeDuration : 1f)
            );

            bool visible = cg.alpha > 0.01f;
            warningIcon.enabled = visible;
            warningIcon.raycastTarget = visible;

            if (!visible && warningIcon.gameObject.activeSelf)
                warningIcon.gameObject.SetActive(false);
        }
    }

    // =========================================================================
    // ForceHarvestImmediateAndDestroy - Instant harvest without animation.
    // ForceHarvestImmediateAndDestroy - Thu hoạch tức thì không có animation.
    // 
    // Called by: FarmArea.SettleAndClearForNewSeason() when season changes.
    // Used in Seasonal scoring mode to settle all plants at phase boundary.
    // 
    // Được gọi bởi: FarmArea.SettleAndClearForNewSeason() khi đổi mùa.
    // Dùng trong chế độ Seasonal để chốt điểm tất cả cây tại ranh giới phase.
    // =========================================================================
    public void ForceHarvestImmediateAndDestroy()
    {
        if (_harvested) return;
        _ready = true;
        _harvesting = false;

        // Calculate score using area salinity.
        // Tính điểm sử dụng độ mặn của vùng.
        int points = AdjustBySalinity(_econ);

        // Apply special fish rules.
        // Áp dụng quy tắc đặc biệt cho cá.
        if (_fishData != null && (_fishData.id == 5 || _fishData.id == 6))
            if (_ownerArea && _ownerArea.waterType == WaterType.Fresh) points = 0;

        if (_fishData != null && (_fishData.id == 2))
            if (_ownerArea && _ownerArea.waterType == WaterType.Salt) points = 0;

        var gm = Thuan_23127_GameManager.Instance;
        if (gm) gm.AddScore(points);

        OnHarvested?.Invoke(points);
        OnStateChanged?.Invoke(State.Done);

        // Cleanup and destroy immediately (no delay).
        // Dọn dẹp và hủy ngay lập tức (không chờ).
        OnAboutToDestroy?.Invoke();
        if (_ownerArea) _ownerArea.FreePlot(_ownerIndex);
        Destroy(gameObject);
    }
    
    // =========================================================================
    // EvaluateSalinityEffects - Handles animation based on salinity stress.
    // EvaluateSalinityEffects - Xử lý animation dựa trên stress độ mặn.
    // 
    // If salinity exceeds threshold for 10 seconds, plays "bad" animation.
    // When salinity returns to safe level, plays "good" animation.
    // 
    // Nếu độ mặn vượt ngưỡng trong 10 giây, phát animation "xấu".
    // Khi độ mặn trở về mức an toàn, phát animation "tốt".
    // =========================================================================
    private void EvaluateSalinityEffects(float currentSalinity, float threshold)
    {
        // Only applies to plants (not animals/fish).
        // Chỉ áp dụng cho cây (không phải động vật/cá).
        if (_plantData == null) return;

        bool nowOver = currentSalinity > threshold;

        // Entering stress state.
        // Vào trạng thái stress.
        if (nowOver && !_isOverSalt)
        {
            _isOverSalt = true;
            _badAnimPlayedThisSaltPeriod = false;

            // Start 10-second countdown.
            // Bắt đầu đếm ngược 10 giây.
            if (_salinityBadCo != null) StopCoroutine(_salinityBadCo);
            _salinityBadCo = StartCoroutine(CoPlayBadAfterDelay());
        }
        // Exiting stress state (returning to safe).
        // Thoát khỏi trạng thái stress (trở về an toàn).
        else if (!nowOver && _isOverSalt)
        {
            _isOverSalt = false;

            // Cancel countdown.
            // Hủy đếm ngược.
            if (_salinityBadCo != null) { StopCoroutine(_salinityBadCo); _salinityBadCo = null; }

            // Play "good" animation.
            // Phát animation "tốt".
            if (plantAnimator && !string.IsNullOrEmpty(animGood))
            {
                plantAnimator.Play(animGood, -1, 0f);
            }
        }
    }

    // =========================================================================
    // CoPlayBadAfterDelay - Plays "bad" animation after salinity stress delay.
    // CoPlayBadAfterDelay - Phát animation "xấu" sau thời gian stress độ mặn.
    // =========================================================================
    private IEnumerator CoPlayBadAfterDelay()
    {
        float t = 0f;
        while (t < salinityBadDelay)
        {
            // Exit if no longer over threshold.
            // Thoát nếu không còn vượt ngưỡng.
            if (!_isOverSalt)
            {
                _salinityBadCo = null;
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }

        // Still over threshold after delay - play "bad" animation once.
        // Vẫn vượt ngưỡng sau thời gian chờ - phát animation "xấu" một lần.
        if (_isOverSalt && !_badAnimPlayedThisSaltPeriod)
        {
            _badAnimPlayedThisSaltPeriod = true;
            if (plantAnimator && !string.IsNullOrEmpty(animBad))
            {
                plantAnimator.Play(animBad, -1, 0f);
            }
        }
        _salinityBadCo = null;
    }

    // =========================================================================
    // Awake - Initialize warning icon and find animator.
    // Awake - Khởi tạo icon cảnh báo và tìm animator.
    // =========================================================================
    private void Awake()
    {
        // Initialize warning icon as hidden.
        // Khởi tạo icon cảnh báo ở trạng thái ẩn.
        if (warningIcon)
        {
            var cg = warningIcon.GetComponent<CanvasGroup>();
            if (!cg) cg = warningIcon.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            warningIcon.enabled = false;
            warningIcon.raycastTarget = false;
            warningIcon.gameObject.SetActive(false);
        }

        // Auto-find animator if not assigned.
        // Tự động tìm animator nếu chưa gán.
        if (!plantAnimator)
            plantAnimator = GetComponentInChildren<Animator>();
    }
    
    // =========================================================================
    // GetLangStrings - Gets localized strings for health description.
    // GetLangStrings - Lấy các chuỗi đa ngôn ngữ cho mô tả sức khỏe.
    // =========================================================================
    private (string unit, string statusHealthy, string statusDiseased,
        string labelThreshold, string labelCurrent,
        string tplHealthy, string tplDiseased)
        GetLangStrings()
    {
        var jr = _jsonReader ?? Thuan_23127_GameManager.Instance?.jsonReader;

        // Default Vietnamese strings.
        // Chuỗi tiếng Việt mặc định.
        string unit = "‰", sh = "Tốt", sd = "Bệnh",
            thr = "Ngưỡng chịu mặn", cur = "Độ mặn hiện tại",
            thTpl = "{tag} đang {status}. {currentLabel}: {current}{unit} | {thresholdLabel}: {threshold}{unit}.",
            dsTpl = "{tag} đang {status} do mặn vượt ngưỡng. {currentLabel}: {current}{unit} > {thresholdLabel}: {threshold}{unit}.";

        var l = jr != null ? jr.GetCurrentLangData() : null;
        if (l != null && l.interpretation != null)
        {
            var f = l.interpretation.fields;
            var s = l.interpretation.status_text;
            var t = l.interpretation.templates;

            unit  = f?.unit_ppt                ?? unit;
            sh    = s?.healthy                 ?? sh;
            sd    = s?.diseased                ?? sd;
            thr   = f?.threshold_label         ?? thr;
            cur   = f?.current_salinity_label  ?? cur;
            thTpl = t?.healthy_desc            ?? thTpl;
            dsTpl = t?.diseased_desc           ?? dsTpl;

        }

        return (unit, sh, sd, thr, cur, thTpl, dsTpl);
    }

    // =========================================================================
    // EmitHealthDescription - Broadcasts health status text for HUD.
    // EmitHealthDescription - Phát text trạng thái sức khỏe cho HUD.
    // 
    // Generates localized description like:
    // "Sầu riêng đang Tốt. Độ mặn hiện tại: 0.30‰ | Ngưỡng chịu mặn: 0.80‰."
    // =========================================================================
    private void EmitHealthDescription(float currentSalinity, float threshold)
    {
        // Get entity name.
        // Lấy tên thực thể.
        string tagName = string.Empty;
        if (_plantData != null)
        {
            tagName = _plantData.tag_name ?? "Cây";
        }
        else if (_animalData != null)
        {
            tagName = _animalData.tag_name ?? "Vật nuôi";
        }
        else if (_fishData != null)
        {
            tagName = _fishData.tag_name ?? "Thủy sản";
        }
        else
        {
            OnHealthTextChanged?.Invoke(string.Empty);
            return;
        }

        bool diseased = currentSalinity > threshold;

        var ls = GetLangStrings();

        // Build description from template.
        // Xây dựng mô tả từ template.
        string text = !diseased
            ? ls.tplHealthy
                .Replace("{tag}", tagName)
                .Replace("{status}", ls.statusHealthy)
                .Replace("{currentLabel}", ls.labelCurrent)
                .Replace("{thresholdLabel}", ls.labelThreshold)
                .Replace("{current}", currentSalinity.ToString("0.00"))
                .Replace("{threshold}", threshold.ToString("0.00"))
                .Replace("{unit}", ls.unit)
            : ls.tplDiseased
                .Replace("{tag}", tagName)
                .Replace("{status}", ls.statusDiseased)
                .Replace("{currentLabel}", ls.labelCurrent)
                .Replace("{thresholdLabel}", ls.labelThreshold)
                .Replace("{current}", currentSalinity.ToString("0.00"))
                .Replace("{threshold}", threshold.ToString("0.00"))
                .Replace("{unit}", ls.unit);

        OnHealthTextChanged?.Invoke(text);
    }

}
