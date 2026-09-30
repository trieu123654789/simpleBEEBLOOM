using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// SeasonPhase - Represents the three season phases in the game.
// SeasonPhase - Đại diện cho ba pha mùa trong game.
// 
// Rainy1 (0): First rainy season (0-90 seconds)
// Dry (1): Dry season (90-180 seconds)
// Rainy2 (2): Second rainy season (unused in current implementation)
// =============================================================================
public enum SeasonPhase { Rainy1 = 0, Dry = 1, Rainy2 = 2 }

// =============================================================================
// SeasonUI - UI elements for displaying a single season's score.
// SeasonUI - Các phần tử UI để hiển thị điểm của một mùa.
// =============================================================================
[Serializable]
public class SeasonUI
{
    // Text displaying the season's score.
    // Text hiển thị điểm của mùa.
    public Text  scoreText;
    
    // Icon image for this season.
    // Image icon cho mùa này.
    public Image iconImage;
    
    // Default icon when no harvest has occurred.
    // Icon mặc định khi chưa có thu hoạch.
    public Sprite defaultIcon;
}

// =============================================================================
// Thuan_23127_AreaHUD - HUD display for a single FarmArea.
// Thuan_23127_AreaHUD - Hiển thị HUD cho một FarmArea đơn lẻ.
// 
// This HUD shows:
// - Plant growth progress bar
// - Current salinity vs threshold
// - Season scores (Rainy and Dry columns)
// - Subject icon (current plant type)
// - Health description text
// - Popup for detailed salinity information
// 
// HUD này hiển thị:
// - Thanh tiến độ phát triển cây
// - Độ mặn hiện tại so với ngưỡng
// - Điểm theo mùa (cột Mưa và Khô)
// - Icon đối tượng (loại cây hiện tại)
// - Text mô tả sức khỏe
// - Popup cho thông tin độ mặn chi tiết
// =============================================================================
public class Thuan_23127_AreaHUD : MonoBehaviour
{
    // =========================================================================
    // PROGRESS DISPLAY
    // HIỂN THỊ TIẾN ĐỘ
    // =========================================================================
    [Header("Progress")]
    // Fill image for progress bar (0-1 fill amount).
    // Image fill cho thanh tiến độ (fill amount 0-1).
    public Image progressFill;
    
    // Text showing percentage (e.g., "75%").
    // Text hiển thị phần trăm (ví dụ: "75%").
    public Text  progressPercentText;

    // =========================================================================
    // SALINITY DISPLAY
    // HIỂN THỊ ĐỘ MẶN
    // =========================================================================
    [Header("Salinity (HUD mini on board)")]
    // Legacy Text component for salinity display.
    // Component Text cũ cho hiển thị độ mặn.
    public Text salinityText;
    
    // TextMeshPro component for salinity display (preferred).
    // Component TextMeshPro cho hiển thị độ mặn (ưu tiên dùng).
    public TextMeshProUGUI salinityTextPro;

    // =========================================================================
    // SUBJECT & DESCRIPTION
    // ĐỐI TƯỢNG & MÔ TẢ
    // =========================================================================
    [Header("Subject & short description on board")]
    // Image showing the current plant/animal/fish icon.
    // Image hiển thị icon cây/động vật/cá hiện tại.
    public Image subjectImage;
    
    // Reference for localization.
    // Tham chiếu cho đa ngôn ngữ.
    public Thuan_23127_JsonReader jsonReader;
    
    // Text showing health status description.
    // Text hiển thị mô tả trạng thái sức khỏe.
    public Text descriptionText;

    // =========================================================================
    // SALINITY INFO POPUP
    // POPUP THÔNG TIN ĐỘ MẶN
    // =========================================================================
    [Header("Popup salinity panel (UI_Info_Salinity)")]
    // The popup panel GameObject (toggled on/off).
    // GameObject panel popup (bật/tắt).
    public GameObject showUIInformationSalinity;
    
    // Popup header showing current salinity.
    // Header popup hiển thị độ mặn hiện tại.
    public Text popupHeadText;
    
    // Popup body showing detailed description.
    // Body popup hiển thị mô tả chi tiết.
    public Text popupBodyText;

    // CanvasGroup for fade effects on popup.
    // CanvasGroup cho hiệu ứng fade trên popup.
    private CanvasGroup _popupCg;

    // -------------------------------------------------------------------------
    // Static reference to which HUD currently owns the popup.
    // Prevents multiple HUDs from fighting over the same popup.
    // Tham chiếu static đến HUD nào đang sở hữu popup.
    // Ngăn nhiều HUD tranh giành cùng một popup.
    // -------------------------------------------------------------------------
    private static Thuan_23127_AreaHUD _currentOwner;

    // =========================================================================
    // Awake - Initialize popup CanvasGroup and hide subject image.
    // Awake - Khởi tạo CanvasGroup popup và ẩn image đối tượng.
    // =========================================================================
    private void Awake()
    {
        // Get CanvasGroup for alpha transitions.
        // Lấy CanvasGroup cho chuyển đổi alpha.
        if (showUIInformationSalinity)
        {
            _popupCg = showUIInformationSalinity.GetComponent<CanvasGroup>();
        }

        // Start with subject image hidden.
        // Bắt đầu với image đối tượng bị ẩn.
        if (subjectImage)
        {
            subjectImage.sprite  = null;
            subjectImage.enabled = false;
            var c = subjectImage.color; 
            c.a = 0f; 
            subjectImage.color = c;
        }
    }

    // =========================================================================
    // OnClickShowInformationSalinity - Toggle salinity info popup.
    // OnClickShowInformationSalinity - Bật/tắt popup thông tin độ mặn.
    // 
    // Called by: UI Button onClick.
    // Được gọi bởi: UI Button onClick.
    // 
    // If popup is visible and owned by this HUD → hide it.
    // Otherwise → show popup with this HUD's data.
    // =========================================================================
    public void OnClickShowInformationSalinity()
    {
        if (!showUIInformationSalinity) return;

        // Check if popup is currently visible.
        // Kiểm tra popup có đang hiển thị không.
        bool visible = showUIInformationSalinity.activeSelf;
        if (_popupCg == null && visible)
            _popupCg = showUIInformationSalinity.GetComponent<CanvasGroup>();
        if (_popupCg != null)
            visible = visible && _popupCg.alpha > 0.01f;

        // If popup is open and owned by this HUD → close it.
        // Nếu popup đang mở và thuộc về HUD này → đóng nó.
        if (visible && _currentOwner == this)
        {
            HideInformationSalinity();
            return;
        }

        // Otherwise: open popup with this HUD's salinity data.
        // Ngược lại: mở popup với dữ liệu độ mặn của HUD này.

        // Copy salinity text to popup header.
        // Copy text độ mặn vào header popup.
        if (popupHeadText)
        {
            if (salinityTextPro != null && !string.IsNullOrEmpty(salinityTextPro.text))
                popupHeadText.text = salinityTextPro.text;
            else if (salinityText != null)
                popupHeadText.text = salinityText.text;
            else
                popupHeadText.text = string.Empty;
        }

        // Copy description to popup body.
        // Copy mô tả vào body popup.
        if (popupBodyText && descriptionText)
            popupBodyText.text = descriptionText.text ?? string.Empty;

        // Show popup with full opacity.
        // Hiển thị popup với độ mờ đầy đủ.
        showUIInformationSalinity.SetActive(true);

        if (_popupCg == null)
            _popupCg = showUIInformationSalinity.GetComponent<CanvasGroup>();

        if (_popupCg != null)
        {
            _popupCg.alpha = 1f;
            _popupCg.blocksRaycasts = false; // Không chặn XR ray
        }

        // Mark this HUD as popup owner.
        // Đánh dấu HUD này là chủ sở hữu popup.
        _currentOwner = this;
        Debug.Log($"[AreaHUD] Show salinity info popup - owner = {name}");
    }

    // =========================================================================
    // HideInformationSalinity - Closes the salinity info popup.
    // HideInformationSalinity - Đóng popup thông tin độ mặn.
    // 
    // Called by: Close button or internal logic.
    // Được gọi bởi: Nút đóng hoặc logic nội bộ.
    // =========================================================================
    public void HideInformationSalinity()
    {
        if (showUIInformationSalinity)
            showUIInformationSalinity.SetActive(false);

        // Clear ownership if this HUD was the owner.
        // Xóa quyền sở hữu nếu HUD này là chủ.
        if (_currentOwner == this)
            _currentOwner = null;

        if (_popupCg != null)
            _popupCg.alpha = 0f;

        Debug.Log($"[AreaHUD] Hide salinity info popup - owner = {name}");
    }

    // =========================================================================
    // SetDescription - Updates the health description text.
    // SetDescription - Cập nhật text mô tả sức khỏe.
    // 
    // Called by: PlantGrowth when health status changes.
    // Được gọi bởi: PlantGrowth khi trạng thái sức khỏe thay đổi.
    // =========================================================================
    public void SetDescription(string s)
    {
        if (descriptionText) descriptionText.text = s ?? string.Empty;
    }

    // =========================================================================
    // SEASON SCORES (2 columns: Rainy and Dry)
    // ĐIỂM THEO MÙA (2 cột: Mưa và Khô)
    // =========================================================================
    [Header("Season Scores (2 columns)")]
    // Rainy season column UI.
    // UI cột mùa Mưa.
    public SeasonUI rainy;
    
    // Dry season column UI.
    // UI cột mùa Khô.
    public SeasonUI dry;

    // Internal score storage: [0]=Rainy, [1]=Dry.
    // Lưu trữ điểm nội bộ: [0]=Mưa, [1]=Khô.
    private readonly int[] _phaseScores = new int[2];

    // =========================================================================
    // PhaseToIndex - Converts SeasonPhase enum to array index.
    // PhaseToIndex - Chuyển đổi enum SeasonPhase thành index mảng.
    // =========================================================================
    private static int PhaseToIndex(SeasonPhase p)
    {
        // Rainy1 and Rainy2 both map to index 0.
        // Rainy1 và Rainy2 đều map sang index 0.
        return (p == SeasonPhase.Rainy1) ? 0 : 1;
    }

    // =========================================================================
    // Show - Shows or hides the entire HUD.
    // Show - Hiển thị hoặc ẩn toàn bộ HUD.
    // =========================================================================
    public void Show(bool v) => gameObject.SetActive(v);

    // =========================================================================
    // SetProgress - Updates the progress bar display.
    // SetProgress - Cập nhật hiển thị thanh tiến độ.
    // 
    // Parameter t: Progress value from 0.0 to 1.0.
    // Tham số t: Giá trị tiến độ từ 0.0 đến 1.0.
    // =========================================================================
    public void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);
        if (progressFill)        progressFill.fillAmount = t;
        if (progressPercentText) progressPercentText.text = Mathf.RoundToInt(t * 100f) + "%";
    }

    // =========================================================================
    // SetSalinity - Updates salinity display with localized label.
    // SetSalinity - Cập nhật hiển thị độ mặn với nhãn đa ngôn ngữ.
    // 
    // Format: "Salinity: 0.50 / 0.80" (current / threshold).
    // Định dạng: "Độ mặn: 0.50 / 0.80" (hiện tại / ngưỡng).
    // =========================================================================
    public void SetSalinity(float current, float threshold)
    {
        string salinityLabel = "Salinity";
        if (jsonReader != null)
        {
            var langData = jsonReader.GetCurrentLangData();
            if (langData?.interpretation?.fields != null)
            {
                salinityLabel = langData.interpretation.fields.salinity ?? "Salinity";
            }
        }

        string formattedText = $"{salinityLabel} : {current:0.00} / {threshold:0.00}";

        if (salinityText)    salinityText.text    = formattedText;
        if (salinityTextPro) salinityTextPro.text = formattedText;
    }

    // =========================================================================
    // SetSubject - Sets the icon for current plant/animal/fish.
    // SetSubject - Đặt icon cho cây/động vật/cá hiện tại.
    // 
    // Called by: FarmArea when a new entity is planted.
    // Được gọi bởi: FarmArea khi một thực thể mới được trồng.
    // =========================================================================
    public void SetSubject(Sprite icon)
    {
        if (!subjectImage) return;
        subjectImage.sprite  = icon;
        subjectImage.enabled = (icon != null);

        var c = subjectImage.color; 
        c.a = (icon != null) ? 1f : 0f;
        subjectImage.color = c;
        subjectImage.preserveAspect = true;
    }

    // =========================================================================
    // ResetSeasonScoresPhase - Resets all season scores to zero.
    // ResetSeasonScoresPhase - Reset tất cả điểm mùa về 0.
    // =========================================================================
    public void ResetSeasonScoresPhase()
    {
        _phaseScores[0] = _phaseScores[1] = 0;
        RefreshPhase(SeasonPhase.Rainy1);
        RefreshPhase(SeasonPhase.Dry);
    }

    // =========================================================================
    // SetSeasonScoresPhase - Sets specific scores for each season.
    // SetSeasonScoresPhase - Đặt điểm cụ thể cho từng mùa.
    // =========================================================================
    public void SetSeasonScoresPhase(int rainyVal, int dryVal)
    {
        _phaseScores[0] = Mathf.Max(0, rainyVal);
        _phaseScores[1] = Mathf.Max(0, dryVal);
        RefreshPhase(SeasonPhase.Rainy1);
        RefreshPhase(SeasonPhase.Dry);
    }

    // =========================================================================
    // AddSeasonPointsPhase - Adds points to a specific season's score.
    // AddSeasonPointsPhase - Cộng điểm vào điểm của một mùa cụ thể.
    // 
    // Called by: FarmArea.WireGrowthForAreaTotals() when harvest occurs.
    // Được gọi bởi: FarmArea.WireGrowthForAreaTotals() khi thu hoạch xảy ra.
    // =========================================================================
    public void AddSeasonPointsPhase(SeasonPhase phase, int delta, Sprite iconOverride = null)
    {
        int idx = PhaseToIndex(phase);
        _phaseScores[idx] = Mathf.Max(0, _phaseScores[idx] + delta);

        var ui = (idx == 0) ? rainy : dry;
        if (ui.scoreText) ui.scoreText.text = _phaseScores[idx].ToString();
        SetIconForPhase(idx, iconOverride);
    }

    // =========================================================================
    // RefreshPhase - Updates UI for a specific season phase.
    // RefreshPhase - Cập nhật UI cho một pha mùa cụ thể.
    // =========================================================================
    private void RefreshPhase(SeasonPhase p)
    {
        int idx = PhaseToIndex(p);
        var ui  = (idx == 0) ? rainy : dry;
        if (ui == null) return;

        int val = _phaseScores[idx];
        if (ui.scoreText) ui.scoreText.text = val.ToString();

        if (ui.iconImage)
        {
            if (!ui.iconImage.sprite) ui.iconImage.sprite = ui.defaultIcon;
            ui.iconImage.enabled = (val > 0) && (ui.iconImage.sprite != null);
            ui.iconImage.preserveAspect = true;
        }
    }

    // =========================================================================
    // SetIconForPhase - Sets the icon for a specific season column.
    // SetIconForPhase - Đặt icon cho một cột mùa cụ thể.
    // =========================================================================
    private void SetIconForPhase(int idx, Sprite iconOverride = null)
    {
        var ui = (idx == 0) ? rainy : dry;
        if (ui == null || ui.iconImage == null) return;

        ui.iconImage.sprite  = iconOverride != null ? iconOverride : ui.defaultIcon;
        ui.iconImage.enabled = (_phaseScores[idx] > 0) && (ui.iconImage.sprite != null);
        ui.iconImage.preserveAspect = true;
    }

    // =========================================================================
    // ResetHUDToDefaults - Resets entire HUD to initial state.
    // ResetHUDToDefaults - Reset toàn bộ HUD về trạng thái ban đầu.
    // 
    // Called by: FarmArea.ResetAllPlots() when clearing the area.
    // Được gọi bởi: FarmArea.ResetAllPlots() khi dọn vùng.
    // =========================================================================
    public void ResetHUDToDefaults()
    {
        SetProgress(0f);

        if (descriptionText) descriptionText.text = string.Empty;
        if (salinityText) salinityText.text = string.Empty;
        if (salinityTextPro) salinityTextPro.text = string.Empty;

        ResetSeasonScoresPhase();

        if (subjectImage)
        {
            subjectImage.sprite  = null;
            subjectImage.enabled = false;
            var c = subjectImage.color; 
            c.a = 0f; 
            subjectImage.color = c;
            subjectImage.preserveAspect = true;
        }
    }
}
