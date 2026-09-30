// =============================================================================
// Labels - Localized UI text labels for the game interface.
// Labels - Các nhãn text UI đã bản địa hóa cho giao diện game.
// 
// These strings are used throughout the UI for buttons, headers, and displays.
// Loaded from JSON and accessed via JsonReader.GetCurrentLangData().labels
// 
// Các chuỗi này được dùng khắp UI cho nút, tiêu đề, và hiển thị.
// Được tải từ JSON và truy cập qua JsonReader.GetCurrentLangData().labels
// =============================================================================
[System.Serializable]
public class Labels
{
    // Core UI labels.
    // Nhãn UI cốt lõi.
    public string info;        // "INFO" / "THÔNG TIN"
    public string name;        // "Name" / "Tên"
    public string level;       // "Level" / "Cấp độ"
    public string score;       // "Score" / "Điểm"
    public string playagain;   // "Play Again" / "Chơi lại"
    public string language;    // "Language" / "Ngôn ngữ"
    public string setting;     // "Settings" / "Cài đặt"
    public string salinity;    // "Salinity" / "Độ mặn"
    
    // =========================================================================
    // David's additions for SeasonHUD.
    // Các phần bổ sung của David cho SeasonHUD.
    // =========================================================================
    
    // Water level label for SeasonHUD.
    // Nhãn mực nước cho SeasonHUD.
    public string water_level;      // "River Level" / "Mực nước sông"
    
    // Water level status when high (>= 75%).
    // Trạng thái mực nước khi cao (>= 75%).
    public string full;             // "Full" / "Đầy"
    
    // Water level status when low (< 75%).
    // Trạng thái mực nước khi thấp (< 75%).
    public string low;              // "Low" / "Thấp"
    
    // Rainy season label.
    // Nhãn mùa mưa.
    public string season_rainy;     // "Rainy Season" / "Mùa mưa"
    
    // Dry season label.
    // Nhãn mùa khô.
    public string season_dry;       // "Dry Season" / "Mùa khô"
}