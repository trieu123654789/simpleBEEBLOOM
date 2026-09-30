using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// Thuan_23127_JsonReader - Loads and manages game data from JSON.
// Thuan_23127_JsonReader - Tải và quản lý dữ liệu game từ JSON.
// 
// This class handles:
// - Loading plant/animal/fish data from JSON resource files
// - Multi-language support (vi, en, fr, th)
// - UI text updates for localization
// - Providing data lookup by ID for PlantGrowth system
// 
// Lớp này xử lý:
// - Tải dữ liệu cây/động vật/cá từ file JSON resource
// - Hỗ trợ đa ngôn ngữ (vi, en, fr, th)
// - Cập nhật text UI cho bản địa hóa
// - Cung cấp tra cứu dữ liệu theo ID cho hệ thống PlantGrowth
// =============================================================================
public class Thuan_23127_JsonReader : MonoBehaviour
{
    // =========================================================================
    // UI REFERENCES
    // THAM CHIẾU UI
    // =========================================================================
    [Header("UI Setup")]
    // Player name display.
    // Hiển thị tên người chơi.
    public Text nameText;
    
    // Level display.
    // Hiển thị cấp độ.
    public Text levelText;
    
    // Current score display (in-game).
    // Hiển thị điểm hiện tại (trong game).
    public Text scoreText;
    
    // Info text (generic).
    // Text thông tin (chung).
    public Text infoText;
    
    // Score display on end game screen.
    // Hiển thị điểm trên màn hình kết thúc.
    public Text scoreTextEndGame;
    
    // "Play Again" button text.
    // Text nút "Chơi lại".
    public Text playAgainText;
    
    // "Settings" button text.
    // Text nút "Cài đặt".
    public Text settingText;
    
    // Current salinity display.
    // Hiển thị độ mặn hiện tại.
    public Text salinityText;
    
    // Score display in details panel.
    // Hiển thị điểm trong panel chi tiết.
    public Text scoreTextDetails;

    // =========================================================================
    // CONFIGURATION
    // CẤU HÌNH
    // =========================================================================
    [Header("Config")]
    // JSON file name in Resources folder (without .json extension).
    // Tên file JSON trong thư mục Resources (không có đuôi .json).
    public string fileName = "data";
    
    // Current language code: "vi", "en", "fr", "th".
    // Mã ngôn ngữ hiện tại: "vi", "en", "fr", "th".
    public string currentLang = "vi";
    
    // Parsed JSON data root object.
    // Object gốc dữ liệu JSON đã parse.
    public Root root;
    
    // Raw JSON string (cached).
    // Chuỗi JSON thô (được cache).
    private string _jsonString;

    // =========================================================================
    // Start - Load JSON data and apply initial language settings.
    // Start - Tải dữ liệu JSON và áp dụng cài đặt ngôn ngữ ban đầu.
    // =========================================================================
    protected virtual void Start()
    {
        // Load JSON from Resources folder.
        // Tải JSON từ thư mục Resources.
        var resourceName = Path.GetFileNameWithoutExtension(fileName);
        var jsonFile = Resources.Load<TextAsset>(resourceName);
        _jsonString = jsonFile.text;
        
        // Parse JSON into Root object.
        // Parse JSON thành object Root.
        root = JsonUtility.FromJson<Root>(_jsonString);

        // Register with GameManager singleton.
        // Đăng ký với singleton GameManager.
        var gm = Thuan_23127_GameManager.Instance;
        if (gm) gm.jsonReader = this;

        // Apply language to UI.
        // Áp dụng ngôn ngữ cho UI.
        ApplyLanguage();
    }

    // =========================================================================
    // SetLanguageByIndex - Changes language based on dropdown index.
    // SetLanguageByIndex - Đổi ngôn ngữ dựa trên index dropdown.
    // 
    // Called by: UI Dropdown onClick.
    // Được gọi bởi: UI Dropdown onClick.
    // 
    // Index mapping: 0=vi, 1=en, 2=fr, 3=th
    // =========================================================================
    public void SetLanguageByIndex(int index)
    {
        switch (index)
        {
            case 0: currentLang = "vi"; break;
            case 1: currentLang = "en"; break;
            case 2: currentLang = "fr"; break;
            case 3: currentLang = "th"; break;
            default: currentLang = "vi"; break;
        }
        Debug.Log($"[Lang] Dropdown -> index={index}, currentLang={currentLang}");
        ApplyLanguage();
    }

    // =========================================================================
    // GetCurrentLangData - Returns the Lang object for current language.
    // GetCurrentLangData - Trả về object Lang cho ngôn ngữ hiện tại.
    // 
    // Falls back to Vietnamese if requested language is not available.
    // Fallback về tiếng Việt nếu ngôn ngữ được yêu cầu không có sẵn.
    // =========================================================================
    public Lang GetCurrentLangData()
    {
        if (root == null) return null;

        var code = string.IsNullOrEmpty(currentLang) ? "vi" : currentLang.ToLowerInvariant();
        
        // Try to get requested language.
        // Thử lấy ngôn ngữ được yêu cầu.
        var pick = code switch
        {
            "vi" => root.vi,
            "en" => root.en,
            "fr" => root.fr,
            "th" => root.th,
            _    => null
        };
        
        if (pick != null) return pick;
        
        // Fallback chain: vi -> en -> fr -> th.
        // Chuỗi fallback: vi -> en -> fr -> th.
        if (root.vi != null) return root.vi;
        if (root.en != null) return root.en;
        if (root.fr != null) return root.fr;
        if (root.th != null) return root.th;
        return null;
    }

    // =========================================================================
    // GetCurrentLangCode - Returns current language code string.
    // GetCurrentLangCode - Trả về chuỗi mã ngôn ngữ hiện tại.
    // =========================================================================
    public string GetCurrentLangCode() => string.IsNullOrEmpty(currentLang) ? "vi" : currentLang;

    // =========================================================================
    // Data accessors - Get plant/animal/fish lists for current language.
    // Accessor dữ liệu - Lấy danh sách cây/động vật/cá cho ngôn ngữ hiện tại.
    // =========================================================================
    private List<Plant>  GetCurrentLangPlants()    => GetCurrentLangData()?.plants;
    private List<Animal> GetCurrentLangAnimals()   => GetCurrentLangData()?.livestock;
    private List<Fish>   GetCurrentLangFish()      => GetCurrentLangData()?.fish;

    // =========================================================================
    // ID-based lookups - Used by PlantGrowth.Init() to get entity data.
    // Tra cứu theo ID - Dùng bởi PlantGrowth.Init() để lấy dữ liệu thực thể.
    // =========================================================================
    
    // Get plant data by ID (e.g., 1 = Durian, 10 = Coconut).
    // Lấy dữ liệu cây theo ID (ví dụ: 1 = Sầu riêng, 10 = Dừa).
    public Plant  GetPlantById(int id)     => GetCurrentLangPlants()?.FirstOrDefault(p => p.id == id);
    
    // Get livestock data by ID (e.g., 3 = Chicken).
    // Lấy dữ liệu vật nuôi theo ID (ví dụ: 3 = Gà).
    public Animal GetLivestockById(int id) => GetCurrentLangAnimals()?.FirstOrDefault(a => a.id == id);
    
    // Get fish data by ID (e.g., 2 = Red Tilapia, 5 = Shrimp).
    // Lấy dữ liệu cá theo ID (ví dụ: 2 = Cá điêu hồng, 5 = Tôm).
    public Fish   GetFishById(int id)      => GetCurrentLangFish()?.FirstOrDefault(f => f.id == id);
    
    // =========================================================================
    // ApplyLanguage - Updates all UI text elements with current language.
    // ApplyLanguage - Cập nhật tất cả text UI với ngôn ngữ hiện tại.
    // =========================================================================
    private void ApplyLanguage()
    {
        if (root == null) return;

        var l = GetCurrentLangData();
        if (l == null) return;

        // Update static labels.
        // Cập nhật các nhãn cố định.
        if (infoText)  infoText.text  = l.labels?.info  ?? "INFO";
        if (nameText)  nameText.text  = $"{l.labels?.name ?? "Name"}: {l.gameplay?.name}";
        if (levelText) levelText.text = $"{l.labels?.level ?? "Level"}: {l.gameplay?.level}";
        if (settingText) settingText.text = l.labels?.setting ?? "Setting";
        if (playAgainText) playAgainText.text = l.labels?.playagain ?? "Play Again";

        // Update salinity display.
        // Cập nhật hiển thị độ mặn.
        var gm = Thuan_23127_GameManager.Instance;
        if (gm) UpdateSalinityUI(gm.GetSeasonSalinity());
        
        // Update score displays.
        // Cập nhật hiển thị điểm.
        var label = l.labels?.score ?? "Score";
        var currentScore = gm ? gm.Score : 0;

        if (scoreText)        scoreText.text        = $"{label}: {currentScore}";
        if (scoreTextEndGame) scoreTextEndGame.text = $"{label}: {currentScore}";
        if (scoreTextDetails) scoreTextDetails.text = $"{label}: {currentScore}";
    }
    
    // =========================================================================
    // OnEnable - Re-register with GameManager when enabled.
    // OnEnable - Đăng ký lại với GameManager khi được bật.
    // =========================================================================
    private void OnEnable()
    {
        var gm = Thuan_23127_GameManager.Instance;
        if (gm) gm.jsonReader = this; 
    }
    
    // =========================================================================
    // UpdateSalinityUI - Updates salinity display with localized label.
    // UpdateSalinityUI - Cập nhật hiển thị độ mặn với nhãn đa ngôn ngữ.
    // 
    // Called by: GameManager when salinity changes.
    // Được gọi bởi: GameManager khi độ mặn thay đổi.
    // =========================================================================
    public void UpdateSalinityUI(float salinity)
    {
        var l = GetCurrentLangData();
        string label = l?.labels?.salinity ?? "Salinity";
        if (salinityText) salinityText.text = $"{label}: {salinity:0.00}";
    }
}
