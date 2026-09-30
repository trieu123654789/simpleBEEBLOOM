// =============================================================================
// Plant - Data model for plant entities loaded from JSON.
// Plant - Mô hình dữ liệu cho các thực thể cây được tải từ JSON.
// 
// This class represents agricultural plants like rice, durian, coconut.
// Data is loaded from Resources/data.json via JsonReader.
// 
// Lớp này đại diện cho các cây nông nghiệp như lúa, sầu riêng, dừa.
// Dữ liệu được tải từ Resources/data.json qua JsonReader.
// 
// Example plants:
// - ID 1: Durian (Sầu riêng) - salinity_threshold: 0.8
// - ID 10: Coconut (Dừa) - salinity_threshold: 1.2
// - ID 11: Rice (Lúa) - salinity_threshold: 0.4
// =============================================================================
[System.Serializable]
public class Plant
{
    // Unique identifier for this plant type.
    // Định danh duy nhất cho loại cây này.
    public int id;
    
    // Display name (localized, e.g., "Sầu riêng" or "Durian").
    // Tên hiển thị (đa ngôn ngữ, ví dụ: "Sầu riêng" hoặc "Durian").
    public string tag_name;
    
    // Time in seconds for plant to fully grow.
    // Thời gian tính bằng giây để cây phát triển hoàn toàn.
    public int growth_time;
    
    // Status descriptions at different growth stages.
    // Mô tả trạng thái ở các giai đoạn phát triển khác nhau.
    public string[] status;
    
    // Base economic value (points before salinity adjustment).
    // Giá trị kinh tế gốc (điểm trước khi điều chỉnh độ mặn).
    public int economic_benefits;
    
    // Detailed information about the plant.
    // Thông tin chi tiết về cây.
    public string information;
    
    // Maximum salinity (‰) this plant can tolerate.
    // Above this threshold, score is reduced proportionally.
    // Độ mặn tối đa (‰) cây này có thể chịu được.
    // Trên ngưỡng này, điểm bị giảm tỷ lệ.
    public float salinity_threshold;
    
    // Time in seconds for harvest animation.
    // Thời gian tính bằng giây cho animation thu hoạch.
    public float harvest_time;
}
