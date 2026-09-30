using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// David_SeasonHUD - Displays water level and salinity HUD with smooth animations.
// David_SeasonHUD - Hiển thị HUD mực nước và độ mặn với animation mượt.
// 
// This HUD component shows:
// - River water level (based on month)
// - Salinity inside dyke (fresh water zone)
// - Salinity outside dyke (brackish water zone)
// - Current season indicator
// 
// Component HUD này hiển thị:
// - Mực nước sông (dựa trên tháng)
// - Độ mặn trong đê (vùng nước ngọt)
// - Độ mặn ngoài đê (vùng nước lợ)
// - Chỉ báo mùa hiện tại
// 
// Features:
// - Smooth animated transitions when season/month changes
// - Multi-language support via JsonReader
// - Color changes based on season (blue=rainy, orange=dry)
// 
// Tính năng:
// - Chuyển đổi animation mượt khi mùa/tháng thay đổi
// - Hỗ trợ đa ngôn ngữ qua JsonReader
// - Đổi màu theo mùa (xanh=mưa, cam=khô)
// =============================================================================
public class David_SeasonHUD : MonoBehaviour
{
    // =========================================================================
    // WATER LEVEL UI ELEMENTS
    // CÁC PHẦN TỬ UI MỰC NƯỚC
    // =========================================================================
    [Header("River Water Level / Mực nước sông")]
    // Label text (e.g., "Mực nước sông:").
    // Text nhãn (ví dụ: "Mực nước sông:").
    public Text waterLevelLabel;
    
    // Value text showing current status (e.g., "Đầy" or "Thấp").
    // Text giá trị hiển thị trạng thái (ví dụ: "Đầy" hoặc "Thấp").
    public Text waterLevelValue;
    
    // Slider showing water level 0-100%.
    // Slider hiển thị mực nước 0-100%.
    public Slider waterLevelSlider;
    
    // Fill image for color changes.
    // Image fill để đổi màu.
    public Image waterLevelFill;
    
    // =========================================================================
    // INSIDE DYKE SALINITY (Fresh water zone)
    // ĐỘ MẶN TRONG ĐÊ (Vùng nước ngọt)
    // =========================================================================
    [Header("Inside Dyke Salinity / Độ mặn Trong Đê")]
    public Text insideSalinityLabel;
    public Text insideSalinityValue;
    public Slider insideSalinitySlider;
    public Image insideSalinityFill;
    
    // =========================================================================
    // OUTSIDE DYKE SALINITY (Brackish water zone)
    // ĐỘ MẶN NGOÀI ĐÊ (Vùng nước lợ)
    // =========================================================================
    [Header("Outside Dyke Salinity / Độ mặn Ngoài Đê")]
    public Text outsideSalinityLabel;
    public Text outsideSalinityValue;
    public Slider outsideSalinitySlider;
    public Image outsideSalinityFill;
    
    // =========================================================================
    // SEASON/TIME DISPLAY (Optional)
    // HIỂN THỊ MÙA/THỜI GIAN (Tùy chọn)
    // =========================================================================
    [Header("Season/Time (Optional) / Thời gian/Mùa")]
    // Displays current season name (e.g., "Mùa mưa").
    // Hiển thị tên mùa hiện tại (ví dụ: "Mùa mưa").
    public Text seasonLabel;
    
    // Displays current month (e.g., "Month 6").
    // Hiển thị tháng hiện tại (ví dụ: "Month 6").
    public Text timeLabel;
    
    // Slider showing overall game time progress (0 → 1 over full game).
    // Slider hiển thị tiến trình thời gian game (0 → 1 trong suốt game).
    [Tooltip("Drag the Slider under 'Thang' here / Kéo Slider dưới 'Thang' vào đây")]
    public Slider timeSlider;
    
    // Fill image of the time slider (for color changes per phase).
    // Image fill của time slider (để đổi màu theo giai đoạn).
    public Image timeFill;
    
    // =========================================================================
    // COLOR CONFIGURATION
    // CẤU HÌNH MÀU SẮC
    // =========================================================================
    [Header("Colors / Màu sắc")]
    // Color during rainy season (blue-ish).
    // Màu trong mùa mưa (xanh dương).
    public Color rainyColor = new Color(0.2f, 0.6f, 1f, 1f);
    
    // Color during dry season (orange-ish).
    // Màu trong mùa khô (cam).
    public Color dryColor = new Color(1f, 0.6f, 0.2f, 1f);
    
    // =========================================================================
    // ANIMATION CONFIGURATION
    // CẤU HÌNH ANIMATION
    // =========================================================================
    [Header("Animation Config / Cấu hình Animation")]
    [Tooltip("Transition duration in seconds / Thời gian chuyển đổi (giây)")]
    // Duration for smooth slider animations when values change.
    // Thời lượng animation mượt khi giá trị thay đổi.
    public float transitionDuration = 10f;
    
    // =========================================================================
    // SALINITY DATA SOURCES
    // NGUỒN DỮ LIỆU ĐỘ MẶN
    // =========================================================================
    [Header("Salinity Sources / Nguồn độ mặn")]
    [Tooltip("Drag FarmArea for 'Inside Dyke' zone here")]
    // FarmArea representing the fresh water zone (inside dyke).
    // FarmArea đại diện cho vùng nước ngọt (trong đê).
    public FarmArea insideDykeArea;
    
    [Tooltip("Drag FarmArea for 'Outside Dyke' zone here")]
    // FarmArea representing the brackish water zone (outside dyke).
    // FarmArea đại diện cho vùng nước lợ (ngoài đê).
    public FarmArea outsideDykeArea;
    
    [Tooltip("Maximum salinity for slider calculation (unit: ‰)")]
    // Used to convert salinity value to slider percentage.
    // Dùng để chuyển đổi giá trị độ mặn thành phần trăm slider.
    public float maxSalinity = 4f;
    
    // =========================================================================
    // LOCALIZATION
    // ĐA NGÔN NGỮ
    // =========================================================================
    [Header("Localization / Đa ngôn ngữ")]
    [Tooltip("Drag JsonReader from scene")]
    // Reference for getting localized text labels.
    // Tham chiếu để lấy các nhãn text đa ngôn ngữ.
    public Thuan_23127_JsonReader jsonReader;
    
    // =========================================================================
    // INTERNAL STATE
    // TRẠNG THÁI NỘI BỘ
    // =========================================================================
    
    // Currently displayed season phase.
    // Phase mùa đang hiển thị.
    private SeasonPhase _currentPhase = SeasonPhase.Rainy1;
    
    // Active animation coroutine (for cancellation).
    // Coroutine animation đang chạy (để hủy).
    private Coroutine _animationCoroutine;
    
    // Cached reference to access timeRemaining & monthDuration.
    // Tham chiếu cached để truy cập timeRemaining & monthDuration.
    // Migrated to GameRulesProvider (static class, no instance needed)
    
    // =========================================================================
    // Start - Initialize and display initial UI state.
    // Start - Khởi tạo và hiển thị trạng thái UI ban đầu.
    // =========================================================================
    private void Start()
    {
        // Auto-find JsonReader if not assigned.
        // Tự động tìm JsonReader nếu chưa gán.
        if (jsonReader == null)
        {
            jsonReader = FindObjectOfType<Thuan_23127_JsonReader>();
        }
        
        // Cache reference to game rules for time slider updates.
        // Cache tham chiếu đến game rules để cập nhật time slider.
        // _gameRules removed  using GameRulesProvider static class
        
        // Initialize time slider to 1 (full = 100%).
        // Khởi tạo time slider ở 1 (đầy = 100%).
        if (timeSlider != null) timeSlider.value = 1f;
        
        // Display initial UI instantly (no animation).
        // Hiển thị UI ban đầu ngay lập tức (không animation).
        UpdateUI(_currentPhase, true);
    }
    
    // =========================================================================
    // OnEnable - Subscribe to season and month change events.
    // OnEnable - Đăng ký lắng nghe sự kiện đổi mùa và tháng.
    // =========================================================================
    private void OnEnable()
    {
        // Subscribe to season change event.
        // Đăng ký sự kiện đổi mùa.
        GameRulesProvider.OnPhaseChanged += OnSeasonChanged;
        
        // Subscribe to month change event.
        // Đăng ký sự kiện đổi tháng.
        GameRulesProvider.OnMonthChanged += OnMonthChanged;
    }
    
    // =========================================================================
    // OnDisable - Unsubscribe from events to prevent memory leaks.
    // OnDisable - Hủy đăng ký sự kiện để tránh rò rỉ bộ nhớ.
    // =========================================================================
    private void OnDisable()
    {
        GameRulesProvider.OnPhaseChanged -= OnSeasonChanged;
        GameRulesProvider.OnMonthChanged -= OnMonthChanged;
    }
    
    // =========================================================================
    // OnSeasonChanged - Called when season phase changes.
    // OnSeasonChanged - Được gọi khi phase mùa thay đổi.
    // 
    // Triggers UI update with smooth animation.
    // Kích hoạt cập nhật UI với animation mượt.
    // =========================================================================
    private void OnSeasonChanged(SeasonPhase newPhase)
    {
        _currentPhase = newPhase;
        UpdateUI(newPhase, false);
    }

    // =========================================================================
    // OnMonthChanged - Called when month changes.
    // OnMonthChanged - Được gọi khi tháng thay đổi.
    // 
    // Updates water level based on new month's water table.
    // Cập nhật mực nước dựa trên bảng nước của tháng mới.
    // =========================================================================
    private void OnMonthChanged(int newMonth)
    {
        // Update UI using current season state.
        // Cập nhật UI dựa trên mùa hiện tại.
        UpdateUI(_currentPhase, false);
    }
    
    // =========================================================================
    // GetInsideSalinity - Gets salinity for inside dyke zone.
    // GetInsideSalinity - Lấy độ mặn cho vùng trong đê.
    // 
    // Priority: Assigned FarmArea > Global GameManager.
    // Ưu tiên: FarmArea được gán > GameManager toàn cục.
    // =========================================================================
    private float GetInsideSalinity()
    {
        if (insideDykeArea != null)
            return insideDykeArea.GetAreaSalinity();
        var gm = Thuan_23127_GameManager.Instance;
        return gm != null ? gm.GetSeasonSalinity() : 0f;
    }
    
    // =========================================================================
    // GetOutsideSalinity - Gets salinity for outside dyke zone.
    // GetOutsideSalinity - Lấy độ mặn cho vùng ngoài đê.
    // =========================================================================
    private float GetOutsideSalinity()
    {
        if (outsideDykeArea != null)
            return outsideDykeArea.GetAreaSalinity();
        var gm = Thuan_23127_GameManager.Instance;
        return gm != null ? gm.GetSeasonSalinity() : 0f;
    }
    
    // =========================================================================
    // GetDisplayMonth - Converts game month index (1-6) to calendar month.
    // GetDisplayMonth - Chuyển đổi chỉ số tháng game (1-6) sang tháng lịch.
    // 
    // Game displays Nov-Apr, so gameMonth 1 -> calendar 11, 2 -> 12, 3 -> 1, ..., 6 -> 4.
    // Game hiển thị T11-T4, nên gameMonth 1 -> lịch 11, 2 -> 12, 3 -> 1, ..., 6 -> 4.
    // =========================================================================
    private int GetDisplayMonth(int gameMonth)
    {
        // Offset by 10 months (November = 11 is 10 months after January = 1)
        // gameMonth 1 -> (1-1+10) % 12 + 1 = 11 (November)
        // gameMonth 2 -> (2-1+10) % 12 + 1 = 12 (December)
        // gameMonth 3 -> (3-1+10) % 12 + 1 = 1  (January)
        // gameMonth 6 -> (6-1+10) % 12 + 1 = 4  (April)
        return ((gameMonth - 1 + 10) % 12) + 1;
    }
    
    // =========================================================================
    // IsRainySeasonMonth - Returns true if calendar month is in rainy season.
    // IsRainySeasonMonth - Trả về true nếu tháng lịch trong mùa mưa.
    // 
    // All displayed months (Nov-Apr) are dry season, so always false.
    // Tất cả tháng hiển thị (T11-T4) đều là mùa khô, luôn trả về false.
    // =========================================================================
    private bool IsRainySeasonMonth(int calendarMonth)
    {
        // Nov-Apr are all dry season months.
        // T11-T4 đều là tháng mùa khô.
        return calendarMonth >= 5 && calendarMonth <= 10;
    }
    
    // =========================================================================
    // UpdateUI - Main method to update all HUD elements.
    // UpdateUI - Method chính để cập nhật tất cả phần tử HUD.
    // 
    // Parameters:
    // - phase: Current season phase
    // - instant: If true, update immediately. If false, animate.
    // 
    // Tham số:
    // - phase: Phase mùa hiện tại
    // - instant: Nếu true, cập nhật ngay. Nếu false, có animation.
    // =========================================================================
    public void UpdateUI(SeasonPhase phase, bool instant = false)
    {
        // Determine season based on calendar month (May-Oct = Rainy, Nov-Apr = Dry).
        // Xác định mùa dựa trên tháng lịch (T5-10 = Mưa, T11-4 = Khô).
        int displayMonth = GetDisplayMonth(GameRulesProvider.CurrentMonthIndex);
        bool isRainy = IsRainySeasonMonth(displayMonth);
        
        // Calculate target values.
        // Tính các giá trị đích.
        
        // Water level from month table (0-100%).
        // Mực nước từ bảng tháng (0-100%).
        float targetWaterLevel = Mathf.Clamp01(GameRulesProvider.CurrentWaterLevelPercent / 100f);
        
        // Get actual salinity from both zones.
        // Lấy độ mặn thực từ cả 2 vùng.
        float insideSalinity = GetInsideSalinity();
        float outsideSalinity = GetOutsideSalinity();
        float targetInsideSlider = Mathf.Clamp01(insideSalinity / maxSalinity);
        float targetOutsideSlider = Mathf.Clamp01(outsideSalinity / maxSalinity);
        
        // Update static text labels.
        // Cập nhật các nhãn text cố định.
        UpdateStaticLabels(isRainy);
        UpdateMonthLabel();

        // Water level fill is always blue (river water color).
        // Fill mực nước luôn là xanh dương (màu nước sông).
        if (waterLevelFill != null) waterLevelFill.color = new Color(0.2f, 0.5f, 1f, 1f);
        
        // Green for healthy salinity, red for dangerous.
        // Xanh cho độ mặn an toàn, đỏ cho nguy hiểm.
        if (insideSalinityFill != null) insideSalinityFill.color = isRainy ? Color.green : Color.red;
        if (outsideSalinityFill != null) outsideSalinityFill.color = isRainy ? Color.green : Color.red;

        if (instant)
        {
            // Instant update - no animation.
            // Cập nhật tức thì - không animation.
            if (waterLevelSlider != null) waterLevelSlider.value = targetWaterLevel;
            if (insideSalinitySlider != null) insideSalinitySlider.value = targetInsideSlider;
            if (outsideSalinitySlider != null) outsideSalinitySlider.value = targetOutsideSlider;
            
            UpdateDynamicValues(targetWaterLevel * 100f, insideSalinity, outsideSalinity, isRainy);
        }
        else
        {
            // Animated update - smooth transition.
            // Cập nhật có animation - chuyển đổi mượt.
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimateUIChange(
                targetWaterLevel, 
                targetInsideSlider, insideSalinity,
                targetOutsideSlider, outsideSalinity, 
                isRainy));
        }
    }

    // =========================================================================
    // AnimateUIChange - Coroutine for smooth slider animations.
    // AnimateUIChange - Coroutine cho animation slider mượt.
    // 
    // Lerps all slider values from current to target over transitionDuration.
    // Nội suy tất cả giá trị slider từ hiện tại đến đích trong transitionDuration.
    // =========================================================================
    private IEnumerator AnimateUIChange(
        float targetWater, 
        float targetInsideSlider, float targetInsideReal,
        float targetOutsideSlider, float targetOutsideReal,
        bool isRainy)
    {
        // Record starting values.
        // Ghi lại giá trị bắt đầu.
        float startWater = waterLevelSlider != null ? waterLevelSlider.value : 0f;
        float startInsideSlider = insideSalinitySlider != null ? insideSalinitySlider.value : 0f;
        float startOutsideSlider = outsideSalinitySlider != null ? outsideSalinitySlider.value : 0f;
        float startInsideReal = startInsideSlider * maxSalinity;
        float startOutsideReal = startOutsideSlider * maxSalinity;
        
        float timer = 0f;
        
        // Animate over duration.
        // Animation trong thời lượng.
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / transitionDuration);
            
            // Lerp all values.
            // Nội suy tất cả giá trị.
            float currentWater = Mathf.Lerp(startWater, targetWater, t);
            float currentInsideSlider = Mathf.Lerp(startInsideSlider, targetInsideSlider, t);
            float currentOutsideSlider = Mathf.Lerp(startOutsideSlider, targetOutsideSlider, t);
            float currentInsideReal = Mathf.Lerp(startInsideReal, targetInsideReal, t);
            float currentOutsideReal = Mathf.Lerp(startOutsideReal, targetOutsideReal, t);
            
            // Apply to sliders.
            // Áp dụng vào slider.
            if (waterLevelSlider != null) waterLevelSlider.value = currentWater;
            if (insideSalinitySlider != null) insideSalinitySlider.value = currentInsideSlider;
            if (outsideSalinitySlider != null) outsideSalinitySlider.value = currentOutsideSlider;
            
            UpdateDynamicValues(currentWater * 100f, currentInsideReal, currentOutsideReal, isRainy);
            
            yield return null;
        }
        
        // Ensure final values are exact.
        // Đảm bảo giá trị cuối cùng chính xác.
        if (waterLevelSlider != null) waterLevelSlider.value = targetWater;
        if (insideSalinitySlider != null) insideSalinitySlider.value = targetInsideSlider;
        if (outsideSalinitySlider != null) outsideSalinitySlider.value = targetOutsideSlider;
        UpdateDynamicValues(targetWater * 100f, targetInsideReal, targetOutsideReal, isRainy);

        // BUG FIX: Clear coroutine reference so LateUpdate live-update can resume.
        // Xóa tham chiếu coroutine để live-update trong LateUpdate có thể chạy lại.
        _animationCoroutine = null;
    }
    
    // =========================================================================
    // UpdateDynamicValues - Updates text values that change during animation.
    // UpdateDynamicValues - Cập nhật giá trị text thay đổi trong animation.
    // =========================================================================
    private void UpdateDynamicValues(float waterPercent, float insidePpt, float outsidePpt, bool isRainy)
    {
        var lang = jsonReader?.GetCurrentLangData();
        var labels = lang?.labels;
        
        // Get localized text for water level status.
        // Lấy text đa ngôn ngữ cho trạng thái mực nước.
        var fullText = labels?.full ?? "Đầy";
        var lowText = labels?.low ?? "Thấp";
        
        // Show "Full" if water >= 75%, otherwise "Low".
        // Hiển thị "Đầy" nếu nước >= 75%, ngược lại "Thấp".
        string currentWaterText = (waterPercent >= 75f) ? fullText : lowText;
        
        if (waterLevelValue != null) waterLevelValue.text = currentWaterText;
        
        // Display salinity with unit (‰ = parts per thousand).
        // Hiển thị độ mặn với đơn vị (‰ = phần nghìn).
        if (insideSalinityValue != null) insideSalinityValue.text = $"{insidePpt:0.0} ‰";
        if (outsideSalinityValue != null) outsideSalinityValue.text = $"{outsidePpt:0.0} ‰";
    }

    // =========================================================================
    // UpdateStaticLabels - Updates fixed text labels with localization.
    // UpdateStaticLabels - Cập nhật các nhãn text cố định với đa ngôn ngữ.
    // =========================================================================
    private void UpdateStaticLabels(bool isRainy)
    {
        var lang = jsonReader?.GetCurrentLangData();
        var labels = lang?.labels;
        
        // Get localized labels with fallbacks.
        // Lấy nhãn đa ngôn ngữ với fallback.
        var waterLabelText = labels?.water_level ?? "Mực nước sông:";
        // OLD: 2-season system (commented out)
        // var rainyText = labels?.season_rainy ?? "Mùa mưa";
        // var dryText = labels?.season_dry ?? "Mùa khô";
        
        if (waterLevelLabel != null) waterLevelLabel.text = waterLabelText;
        if (insideSalinityLabel != null) insideSalinityLabel.text = "Độ Mặn:";
        if (outsideSalinityLabel != null) outsideSalinityLabel.text = "Độ Mặn Ngoài Đê:";
        
        // NEW: 3-phase system based on calendar month
        // Giai đoạn 1: T11–T1 | Giai đoạn 2: T2–T3 | Giai đoạn 3: T4
        if (seasonLabel != null)
        {
            int displayMonth = GetDisplayMonth(GameRulesProvider.CurrentMonthIndex);
            string phaseText;
            if (displayMonth >= 11 || displayMonth <= 1)
                phaseText = "Giai đoạn 1 (T11–T1)";
            else if (displayMonth >= 2 && displayMonth <= 3)
                phaseText = "Giai đoạn 2 (T2–T3)";
            else
                phaseText = "Giai đoạn 3 (T4)";
            seasonLabel.text = phaseText;
        }
    }

    // =========================================================================
    // UpdateMonthLabel - Updates the month display.
    // UpdateMonthLabel - Cập nhật hiển thị tháng.
    // =========================================================================
    private void UpdateMonthLabel()
    {
        if (timeLabel == null) return;

        // Display only the calendar month number.
        // Label "Tháng:" is already set in the UI hierarchy (textThang).
        // Chỉ hiển thị số tháng lịch.
        // Label "Tháng:" đã có sẵn trong UI hierarchy (textThang).
        int displayMonth = GetDisplayMonth(GameRulesProvider.CurrentMonthIndex);
        timeLabel.text = $"Tháng {displayMonth}";
    }
    
    // =========================================================================
    // RefreshLanguage - Call when language changes to update all text.
    // RefreshLanguage - Gọi khi đổi ngôn ngữ để cập nhật tất cả text.
    // =========================================================================
    public void RefreshLanguage()
    {
        UpdateUI(_currentPhase, true);
    }
    
    // =========================================================================
    // LateUpdate - Updates time slider and salinity sliders every frame.
    // LateUpdate - Cập nhật time slider và salinity slider mỗi frame.
    //
    // Runs after Update() to ensure timeRemaining is current.
    // Chạy sau Update() để đảm bảo timeRemaining đã cập nhật.
    // =========================================================================
    private void LateUpdate()
    {
        // Force water level fill to blue every frame.
        // Bắt buộc fill mực nước luôn xanh dương.
        if (waterLevelFill != null) waterLevelFill.color = new Color(0.2f, 0.5f, 1f, 1f);

        // =====================================================================
        // LIVE SALINITY UPDATE — always runs every frame, independent of timeSlider.
        // CẬP NHẬT ĐỘ MẶN TRỰC TIẾP — luôn chạy mỗi frame, độc lập với timeSlider.
        //
        // BUG FIX: Moved OUT of the timeSlider null-guard so it always executes.
        // Phase 1 (Intrusion=0.0) → 0.0‰ | Phase 2 (0.5) → ~2‰ | Phase 3 (1.0) → ~4‰
        // =====================================================================
        if (_animationCoroutine == null)
        {
            float safeMax = Mathf.Max(0.01f, maxSalinity);

            float liveSalinityInside  = GetInsideSalinity();
            float liveSalinityOutside = GetOutsideSalinity();
            float insideSliderVal  = Mathf.Clamp01(liveSalinityInside  / safeMax);
            float outsideSliderVal = Mathf.Clamp01(liveSalinityOutside / safeMax);

            if (insideSalinitySlider  != null) insideSalinitySlider.value  = insideSliderVal;
            if (outsideSalinitySlider != null) outsideSalinitySlider.value = outsideSliderVal;

            if (insideSalinityValue  != null) insideSalinityValue.text  = $"{liveSalinityInside:0.0} ‰";
            if (outsideSalinityValue != null) outsideSalinityValue.text = $"{liveSalinityOutside:0.0} ‰";

            // Color: green < 2‰ | orange 2–3‰ | red > 3‰
            if (insideSalinityFill != null)
                insideSalinityFill.color = liveSalinityInside < 2f ? Color.green
                                         : liveSalinityInside < 3f ? new Color(1f, 0.6f, 0f)
                                         : Color.red;
            if (outsideSalinityFill != null)
                outsideSalinityFill.color = liveSalinityOutside < 2f ? Color.green
                                          : liveSalinityOutside < 3f ? new Color(1f, 0.6f, 0f)
                                          : Color.red;
        }

        // Time slider — requires _gameRules to be valid.
        // Time slider — cần _gameRules hợp lệ.
        if (timeSlider == null) return;

        float totalDuration = GameRulesProvider.MonthDuration * 6f;
        if (totalDuration <= 0f) return;

        // Progress 0.0 (start) → 1.0 (end).
        float progress = Mathf.Clamp01(1f - GameRulesProvider.TimeRemaining / totalDuration);
        timeSlider.value = progress;

        // Color fill: green (Phase1) → yellow (Phase2) → red (Phase3).
        if (timeFill != null)
        {
            float phase1End = GameRulesProvider.MonthDuration * 3f / totalDuration;
            float phase2End = GameRulesProvider.MonthDuration * 5f / totalDuration;
            timeFill.color = progress <= phase1End ? Color.green
                           : progress <= phase2End ? Color.yellow
                           : Color.red;
        }
    }
}
