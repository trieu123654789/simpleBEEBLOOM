// =============================================================================
// Animal - Data model for livestock entities loaded from JSON.
// Animal - Mô hình dữ liệu cho các thực thể vật nuôi được tải từ JSON.
// 
// This class represents farm animals like chickens, ducks, pigs.
// Data is loaded from Resources/data.json via JsonReader.
// 
// Lớp này đại diện cho vật nuôi như gà, vịt, heo.
// Dữ liệu được tải từ Resources/data.json qua JsonReader.
// 
// Example animals:
// - ID 3: Chicken (Gà) - scoring uses percentage-based table
// - ID 4: Duck (Vịt) - threshold-based salinity adjustment
// 
// Note: Animals use percentage-based scoring in GetTableBasedScore():
// - Fresh+Rainy: 85% of base value
// - Fresh+Dry: 80% of base value
// - Salt+Rainy: 75% of base value
// - Salt+Dry: 60% of base value
// =============================================================================
[System.Serializable]
public class Animal
{
    // Unique identifier for this animal type.
    // Định danh duy nhất cho loại động vật này.
    public int id;
    
    // Display name (localized, e.g., "Gà" or "Chicken").
    // Tên hiển thị (đa ngôn ngữ, ví dụ: "Gà" hoặc "Chicken").
    public string tag_name;
    
    // Time in seconds for animal to mature.
    // Thời gian tính bằng giây để động vật trưởng thành.
    public int growth_time;
    
    // Status descriptions at different growth stages.
    // Mô tả trạng thái ở các giai đoạn phát triển khác nhau.
    public string[] status;
    
    // Base economic value (points before zone/season adjustment).
    // Giá trị kinh tế gốc (điểm trước khi điều chỉnh vùng/mùa).
    public int economic_benefits;
    
    // Detailed information about the animal.
    // Thông tin chi tiết về động vật.
    public string information;
    
    // Maximum salinity (‰) this animal can tolerate.
    // Animals are generally less affected by salinity than plants.
    // Độ mặn tối đa (‰) động vật này có thể chịu được.
    // Động vật thường ít bị ảnh hưởng bởi độ mặn hơn cây.
    public float salinity_threshold;
    
    // Time in seconds for harvest animation.
    // Thời gian tính bằng giây cho animation thu hoạch.
    public float harvest_time;
}