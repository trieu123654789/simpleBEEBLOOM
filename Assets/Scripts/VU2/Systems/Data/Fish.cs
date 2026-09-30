// =============================================================================
// Fish - Data model for aquatic entities loaded from JSON.
// Fish - Mô hình dữ liệu cho các thực thể thủy sản được tải từ JSON.
// 
// This class represents fish and shellfish like tilapia, shrimp.
// Data is loaded from Resources/data.json via JsonReader.
// 
// Lớp này đại diện cho cá và động vật có vỏ như cá rô phi, tôm.
// Dữ liệu được tải từ Resources/data.json qua JsonReader.
// 
// Example fish:
// - ID 2: Red Tilapia (Cá điêu hồng) - prefers fresh water
// - ID 5: Black Tiger Shrimp (Tôm sú) - prefers brackish/salt water
// - ID 6: White Leg Shrimp (Tôm thẻ chân trắng) - prefers brackish/salt water
// 
// Special rules:
// - Shrimp (ID 5, 6) give 0 points in fresh water zones
// - Red Tilapia (ID 2) gives 0 points in salt water zones
// =============================================================================
[System.Serializable]
public class Fish
{
    // Unique identifier for this fish type.
    // Định danh duy nhất cho loại cá này.
    public int id;
    
    // Display name (localized, e.g., "Cá điêu hồng" or "Red Tilapia").
    // Tên hiển thị (đa ngôn ngữ, ví dụ: "Cá điêu hồng" hoặc "Red Tilapia").
    public string tag_name;
    
    // Time in seconds for fish to fully mature.
    // Thời gian tính bằng giây để cá trưởng thành hoàn toàn.
    public int growth_time;
    
    // Status descriptions at different growth stages.
    // Mô tả trạng thái ở các giai đoạn phát triển khác nhau.
    public string[] status;
    
    // Base economic value (points before zone/salinity adjustment).
    // Giá trị kinh tế gốc (điểm trước khi điều chỉnh vùng/độ mặn).
    public int economic_benefits;
    
    // Detailed information about the fish species.
    // Thông tin chi tiết về loài cá.
    public string information;
    
    // Maximum salinity (‰) this fish can tolerate.
    // Độ mặn tối đa (‰) loại cá này có thể chịu được.
    public float salinity_threshold;
    
    // Time in seconds for harvest animation.
    // Thời gian tính bằng giây cho animation thu hoạch.
    public float harvest_time;
}