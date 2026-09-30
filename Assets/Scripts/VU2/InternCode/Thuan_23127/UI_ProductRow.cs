using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// UI_ProductRow - Single row in the end-game summary table (3-phase system).
// UI_ProductRow - Một hàng đơn trong bảng tổng kết cuối game (hệ thống 3 GĐ).
// 
// Displays one product type's data across 3 phases:
//   - Sản lượng (Score/Production) per phase
//   - Diện tích (Area) = harvest count × 10 per phase
// 
// Hiển thị dữ liệu của 1 loại sản phẩm qua 3 giai đoạn:
//   - Sản lượng (Điểm) mỗi giai đoạn
//   - Diện tích = số lần thu hoạch × 10 mỗi giai đoạn
// 
// Instantiated by: Thuan_23127_TotalBoard.Rebuild()
// Được instantiate bởi: Thuan_23127_TotalBoard.Rebuild()
// =============================================================================
public class UI_ProductRow : MonoBehaviour
{
    // =========================================================================
    // UI ELEMENTS
    // CÁC PHẦN TỬ UI
    // =========================================================================
    
    // Product icon (plant/animal/fish sprite).
    // Icon sản phẩm (sprite cây/động vật/cá).
    public Image icon;
    
    // Phase 1 (GĐ1: T11–T1) texts.
    // Các text Giai đoạn 1 (GĐ1: T11–T1).
    [Header("Phase 1 / Giai đoạn 1 (T11–T1)")]
    public Text phase1Score;
    public Text phase1Area;
    
    // Phase 2 (GĐ2: T2–T3) texts.
    // Các text Giai đoạn 2 (GĐ2: T2–T3).
    [Header("Phase 2 / Giai đoạn 2 (T2–T3)")]
    public Text phase2Score;
    public Text phase2Area;
    
    // Phase 3 (GĐ3: T4) texts.
    // Các text Giai đoạn 3 (GĐ3: T4).
    [Header("Phase 3 / Giai đoạn 3 (T4)")]
    public Text phase3Score;
    public Text phase3Area;

    // =========================================================================
    // AREA MULTIPLIER
    // HỆ SỐ DIỆN TÍCH
    // =========================================================================
    
    // Area = harvest count × this multiplier.
    // Diện tích = số lần thu hoạch × hệ số này.
    private const int AREA_MULTIPLIER = 10;

    // =========================================================================
    // SetData - Populates the row with 3-phase product data.
    // SetData - Điền dữ liệu 3 giai đoạn của sản phẩm vào hàng.
    // 
    // Parameters / Tham số:
    //   s: Product icon sprite
    //   scores[3]: Score per phase (sản lượng)
    //   counts[3]: Harvest count per phase (for area calculation)
    // =========================================================================
    public void SetData(Sprite s, int[] scores, int[] counts)
    {
        // Set icon if available.
        // Đặt icon nếu có.
        if (icon)
        {
            icon.sprite = s;
            icon.enabled = (s != null);
            icon.preserveAspect = true;
        }
        
        // Phase 1 (GĐ1: T11–T1)
        if (phase1Score) phase1Score.text = scores[0].ToString();
        if (phase1Area)  phase1Area.text  = (counts[0] * AREA_MULTIPLIER).ToString();
        
        // Phase 2 (GĐ2: T2–T3)
        if (phase2Score) phase2Score.text = scores[1].ToString();
        if (phase2Area)  phase2Area.text  = (counts[1] * AREA_MULTIPLIER).ToString();
        
        // Phase 3 (GĐ3: T4)
        if (phase3Score) phase3Score.text = scores[2].ToString();
        if (phase3Area)  phase3Area.text  = (counts[2] * AREA_MULTIPLIER).ToString();
    }
}