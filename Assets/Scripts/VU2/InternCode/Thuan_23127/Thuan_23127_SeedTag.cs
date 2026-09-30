using UnityEngine;

// =============================================================================
// Thuan_23127_SeedTag - Identifies a seed prefab and links it to JSON data.
// Thuan_23127_SeedTag - Xác định prefab hạt giống và liên kết với dữ liệu JSON.
// 
// This component is attached to every plantable prefab (seed/sapling/fish/animal).
// It tells the FarmArea which JSON data to use when planting.
// 
// Component này được gắn vào mọi prefab có thể trồng (hạt/cây con/cá/động vật).
// Nó cho FarmArea biết dữ liệu JSON nào để dùng khi trồng.
// 
// USAGE:
// - Set exactly ONE of the IDs (plantId, animalId, or fishId)
// - Leave others at -1 (not used)
// - IDs correspond to data in the JSON file
// 
// CÁCH DÙNG:
// - Đặt ĐÚNG MỘT ID (plantId, animalId, hoặc fishId)
// - Để các ID khác ở -1 (không dùng)
// - ID tương ứng với dữ liệu trong file JSON
// 
// Example IDs:
// - plantId = 1: Durian (Sầu riêng)
// - plantId = 10: Coconut (Dừa)
// - fishId = 2: Red Tilapia (Cá điêu hồng)
// - fishId = 5: Black Tiger Shrimp (Tôm sú)
// - animalId = 3: Chicken (Gà)
// =============================================================================
public class Thuan_23127_SeedTag : MonoBehaviour
{
    // =========================================================================
    // ENTITY ID REFERENCES
    // THAM CHIẾU ID THỰC THỂ
    // 
    // Only set ONE of these. Leave others at -1.
    // Chỉ đặt MỘT trong số này. Để các cái khác ở -1.
    // =========================================================================
    
    [Tooltip("Plant ID in JSON data. Set -1 if this is not a plant.")]
    // ID for plant entities (rice, durian, coconut, etc.).
    // ID cho các thực thể cây (lúa, sầu riêng, dừa, v.v.).
    public int plantId = -1;
    
    [Tooltip("Animal ID in JSON data. Set -1 if this is not an animal.")]
    // ID for livestock entities (chicken, duck, etc.).
    // ID cho các thực thể vật nuôi (gà, vịt, v.v.).
    public int animalId = -1; 
    
    [Tooltip("Fish ID in JSON data. Set -1 if this is not a fish.")]
    // ID for aquatic entities (fish, shrimp, etc.).
    // ID cho các thực thể thủy sản (cá, tôm, v.v.).
    public int fishId = -1;

    // =========================================================================
    // HUD DISPLAY
    // HIỂN THỊ HUD
    // =========================================================================
    [Header("HUD visuals")]
    // Icon displayed in FarmArea HUD when this entity is planted.
    // Icon hiển thị trên HUD FarmArea khi thực thể này được trồng.
    public Sprite hudIcon;
}
