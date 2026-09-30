using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// WaterType - Classifies farm zones by water salinity level.
// WaterType - Phân loại vùng nông trại theo mức độ mặn của nước.
// =============================================================================
public enum WaterType
{
    Fresh,  // Fresh water (inside dyke) / Nước ngọt (trong đê)
    Salt    // Brackish/Salt water (outside dyke) / Nước lợ/mặn (ngoài đê)
}

// =============================================================================
// FarmArea - Manages a farming zone with multiple planting plots.
// FarmArea - Quản lý một vùng nông trại với nhiều ô trồng.
// 
// Each FarmArea represents a distinct farming zone with:
// - Multiple plot points for planting
// - Its own salinity based on water type and season
// - Tracking of all plants and their scores
// - HUD display for this zone
// 
// Mỗi FarmArea đại diện cho một vùng nông trại riêng biệt với:
// - Nhiều điểm ô để trồng
// - Độ mặn riêng dựa trên loại nước và mùa
// - Theo dõi tất cả cây và điểm của chúng
// - Hiển thị HUD cho vùng này
// =============================================================================
public class FarmArea : MonoBehaviour
{
    // =========================================================================
    // PLOT CONFIGURATION
    // CẤU HÌNH Ô TRỒNG
    // =========================================================================
    [Header("Setup")]
    // Array of positions where plants can be placed.
    // Mảng các vị trí có thể đặt cây.
    public Transform[] plotPoints;

    // =========================================================================
    // REFERENCES
    // THAM CHIẾU
    // =========================================================================
    [Header("Refs")]
    // JSON data reader for plant/animal/fish data.
    // Bộ đọc dữ liệu JSON cho thông tin cây/động vật/cá.
    public Thuan_23127_JsonReader jsonReader;
    
    // HUD display for this farm area.
    // Hiển thị HUD cho vùng nông trại này.
    public Thuan_23127_AreaHUD hud;

    // =========================================================================
    // INTERNAL STATE
    // TRẠNG THÁI NỘI BỘ
    // =========================================================================
    
    // Tracks which plots are currently occupied.
    // Theo dõi ô nào đang có cây.
    private bool[] isPlanted;

    // -------------------------------------------------------------------------
    // Currently bound plant for HUD display (progress/salinity).
    // Cây hiện đang được bind để hiển thị HUD (tiến độ/độ mặn).
    // -------------------------------------------------------------------------
    private Thuan_23127_PlantGrowth _boundGrowth;
    private Action<float> _onProg;
    private Action<float, float> _onSalt;
    private Action<Thuan_23127_PlantGrowth.State> _onState;
    private Action _onAboutToDestroy;

    // -------------------------------------------------------------------------
    // Season totals for this area: Index 0=Rainy1, 1=Dry, 2=Rainy2
    // Tổng điểm theo mùa của vùng này: Index 0=Mưa1, 1=Khô, 2=Mưa2
    // -------------------------------------------------------------------------
    private readonly int[] _seasonTotals = new int[3];

    // -------------------------------------------------------------------------
    // List of all active plants in this area.
    // Danh sách tất cả cây đang hoạt động trong vùng này.
    // -------------------------------------------------------------------------
    private readonly List<Thuan_23127_PlantGrowth> _growths = new List<Thuan_23127_PlantGrowth>();

    // =========================================================================
    // SALINITY CONFIGURATION
    // CẤU HÌNH ĐỘ MẶN
    // =========================================================================
    [Header("Area Seasonal Salinity")]
    [Range(0, 5)] 
    // Salinity during rainy season (lower = fresher water).
    // Độ mặn trong mùa mưa (thấp hơn = nước ngọt hơn).
    public float rainySalinity = 0.5f;
    
    [Range(0, 5)]
    // Salinity during transition phase 2 (T2–T3, medium salinity).
    // Độ mặn trong giai đoạn chuyển tiếp 2 (T2–T3, độ mặn trung bình).
    public float midSalinity = 1.0f;
    
    [Range(0, 5)] 
    // Salinity during dry season (higher = saltier water).
    // Độ mặn trong mùa khô (cao hơn = nước mặn hơn).
    public float drySalinity = 1.5f;

    
    // If true, use area-specific salinity. If false, use global salinity.
    // Nếu true, dùng độ mặn riêng của vùng. Nếu false, dùng độ mặn toàn cục.
    public bool useAreaSeasonalSalinity = true;

    // =========================================================================
    // AREA TYPE (Fresh or Salt water zone)
    // LOẠI VÙNG (Vùng nước ngọt hoặc mặn)
    // =========================================================================
    [Header("Area Type")]
    // Determines scoring modifiers for plants in this zone.
    // Xác định hệ số điểm cho cây trong vùng này.
    public WaterType waterType = WaterType.Fresh;
    
    // =========================================================================
    // SERVER INTEGRATION
    // TÍCH HỢP SERVER
    // =========================================================================
    [Header("Server")]
    // Unique ID for this area when reporting to server.
    // ID duy nhất cho vùng này khi báo cáo lên server.
    [SerializeField] private string serverAreaId = "area_a";

    // =========================================================================
    // GetAreaSalinity - Returns current salinity for this zone (3-phase).
    // GetAreaSalinity - Trả về độ mặn hiện tại cho vùng này (3 giai đoạn).
    // 
    // Phase 1 (T11–T1, Intrusion=0.0) → rainySalinity   (nước ngọt)
    // Phase 2 (T2–T3,  Intrusion=0.5) → midSalinity     (xâm nhập nhẹ)
    // Phase 3 (T4,     Intrusion=1.0) → drySalinity     (xâm nhập nặng)
    //
    // Called by: PlantGrowth, David_SeasonHUD, server reporting.
    // Được gọi bởi: PlantGrowth, David_SeasonHUD, báo cáo server.
    // =========================================================================
    public float GetAreaSalinity()
    {
        if (!useAreaSeasonalSalinity)
            return Thuan_23127_GameManager.Instance
                ? Thuan_23127_GameManager.Instance.GetSeasonSalinity()
                : 0f;

        // Map Saltwater_Intrusion → 3 specific salinity values.
        // Ánh xạ Saltwater_Intrusion → 3 giá trị độ mặn cụ thể.
        float intrusion = GameRulesProvider.Saltwater_Intrusion;
        if (intrusion < 0.1f) return rainySalinity;   // Phase 1 — nước ngọt
        if (intrusion < 1f)   return midSalinity;     // Phase 2 — xâm nhập nhẹ
        return drySalinity;                            // Phase 3 — xâm nhập nặng
    }

    // =========================================================================
    // CurrentPhase - Determines current season phase from salinity value.
    // CurrentPhase - Xác định pha mùa hiện tại từ giá trị độ mặn.
    // =========================================================================
    private SeasonPhase CurrentPhase()
    {
        var s = GameRulesProvider.Saltwater_Intrusion;
        if (Mathf.Approximately(s, 0f)) return SeasonPhase.Rainy1;
        if (Mathf.Approximately(s, 1f)) return SeasonPhase.Dry;
        return SeasonPhase.Rainy2;
    }

    // =========================================================================
    // Start - Initialize plot tracking array.
    // Start - Khởi tạo mảng theo dõi ô trồng.
    // =========================================================================
    private void Start()
    {
        isPlanted = new bool[plotPoints.Length];
    }

    // =========================================================================
    // UnbindCurrent - Removes HUD binding from current plant.
    // UnbindCurrent - Gỡ binding HUD khỏi cây hiện tại.
    // 
    // Why: Prevents memory leaks from event listeners when plant is destroyed.
    // Tại sao: Ngăn rò rỉ bộ nhớ từ event listener khi cây bị hủy.
    // =========================================================================
    private void UnbindCurrent()
    {
        if (!_boundGrowth || !hud) return;

        // Remove all event subscriptions.
        // Gỡ tất cả đăng ký sự kiện.
        _boundGrowth.OnProgressChanged -= _onProg;
        _boundGrowth.OnSalinityChanged -= _onSalt;
        _boundGrowth.OnStateChanged -= _onState;
        _boundGrowth.OnAboutToDestroy -= _onAboutToDestroy;
        _boundGrowth.OnHealthTextChanged -= hud.SetDescription;
        
        _boundGrowth = null;
        _onProg = null; 
        _onSalt = null; 
        _onState = null; 
        _onAboutToDestroy = null;
    }

    // =========================================================================
    // WireGrowthForAreaTotals - Registers a plant for area score tracking.
    // WireGrowthForAreaTotals - Đăng ký một cây để theo dõi điểm của vùng.
    // 
    // When this plant is harvested:
    // 1. Points are added to the current season's total for this area
    // 2. HUD is updated with new score
    // 3. Cleanup happens when plant is destroyed
    // 
    // Khi cây này được thu hoạch:
    // 1. Điểm được cộng vào tổng mùa hiện tại của vùng này
    // 2. HUD được cập nhật với điểm mới
    // 3. Dọn dẹp khi cây bị hủy
    // =========================================================================
    private void WireGrowthForAreaTotals(Thuan_23127_PlantGrowth g)
    {
        if (!g) return;
        
        // Inject salinity provider so plant knows this area's salinity.
        // Tiêm provider độ mặn để cây biết độ mặn của vùng này.
        g.SetSalinityProvider(GetAreaSalinity);

        // Subscribe to harvest event - add points to season total.
        // Đăng ký sự kiện thu hoạch - cộng điểm vào tổng mùa.
        g.OnHarvested += (points) =>
        {
            if (!GameRulesProvider.GameActive) return; // Game ended, ignore.
            if (points <= 0) return;
            
            var phase = CurrentPhase();  // Rainy1/Dry/Rainy2
            int idx = (int)phase;
            _seasonTotals[idx] += points;

            // Update HUD with new season score.
            // Cập nhật HUD với điểm mùa mới.
            if (hud) hud.AddSeasonPointsPhase(phase, points, null);
        };

        // Cleanup when plant is about to be destroyed.
        // Dọn dẹp khi cây sắp bị hủy.
        g.OnAboutToDestroy += () =>
        {
            g.OnHarvested -= null;
            _growths.Remove(g);
        };

        _growths.Add(g);
    }

    // =========================================================================
    // BindGrowthToHUD - Binds a plant to HUD for progress/salinity display.
    // BindGrowthToHUD - Bind một cây vào HUD để hiển thị tiến độ/độ mặn.
    // 
    // Note: This is for individual plant display, separate from area totals.
    // Lưu ý: Đây là để hiển thị cây riêng lẻ, tách biệt khỏi tổng vùng.
    // =========================================================================
    private void BindGrowthToHUD(Thuan_23127_PlantGrowth growth)
    {
        if (!hud || !growth) return;
        
        // Unbind any previously bound plant.
        // Gỡ bind cây đã bind trước đó.
        UnbindCurrent();

        // Set salinity provider for this plant.
        // Đặt provider độ mặn cho cây này.
        growth.SetSalinityProvider(GetAreaSalinity);

        hud.Show(true);
        hud.SetProgress(0f);

        // Create event handlers.
        // Tạo các handler sự kiện.
        _onProg = hud.SetProgress;
        _onSalt = hud.SetSalinity;
        _onState = s => { if (s == Thuan_23127_PlantGrowth.State.Done) hud.SetProgress(0f); };
        _onAboutToDestroy = () => { UnbindCurrent(); };

        // Subscribe to plant events.
        // Đăng ký các sự kiện của cây.
        growth.OnProgressChanged += _onProg;
        growth.OnSalinityChanged += _onSalt;
        growth.OnStateChanged += _onState;
        growth.OnAboutToDestroy += _onAboutToDestroy;
        growth.OnHealthTextChanged += hud.SetDescription;

        _boundGrowth = growth;
        
        // Trigger initial salinity update.
        // Kích hoạt cập nhật độ mặn ban đầu.
        growth.UpdateSalinityEvent();
    }

    // =========================================================================
    // PlantAll - Plants the given prefab in all empty plots.
    // PlantAll - Trồng prefab được cho vào tất cả ô trống.
    // 
    // Called by: UI buttons, game logic when seeding an area.
    // Được gọi bởi: Nút UI, logic game khi gieo hạt một vùng.
    // =========================================================================
    public void PlantAll(GameObject plantPrefab)
    {
        // Only allow planting when game is active.
        // Chỉ cho phép trồng khi game đang hoạt động.
        if (!GameRulesProvider.GameActive) return;
        PlantInternal(plantPrefab, fillAll: true);
    }

    // =========================================================================
    // PlantInternal - Core planting logic: instantiate, setup, wire events.
    // PlantInternal - Logic trồng cốt lõi: instantiate, setup, wire events.
    // 
    // Steps:
    // 1. Get data from SeedTag (plant/animal/fish ID)
    // 2. Look up data in JSON
    // 3. Instantiate prefab at plot position
    // 4. Setup PlantGrowth component
    // 5. Wire to area totals and HUD
    // 6. Initialize with data
    // 
    // Các bước:
    // 1. Lấy dữ liệu từ SeedTag (ID cây/động vật/cá)
    // 2. Tra cứu dữ liệu trong JSON
    // 3. Instantiate prefab tại vị trí ô
    // 4. Setup component PlantGrowth
    // 5. Wire vào tổng vùng và HUD
    // 6. Khởi tạo với dữ liệu
    // =========================================================================
    private void PlantInternal(GameObject plantPrefab, bool fillAll)
    {
        if (plantPrefab == null || jsonReader == null) return;

        // Get SeedTag component to identify plant type.
        // Lấy component SeedTag để xác định loại cây.
        var tag = plantPrefab.GetComponent<Thuan_23127_SeedTag>();
        if (tag == null) { Debug.LogWarning("Prefab thiếu SeedTag."); return; }

        // Look up data based on tag IDs.
        // Tra cứu dữ liệu dựa trên ID trong tag.
        var plantData = (tag.plantId > 0) ? jsonReader.GetPlantById(tag.plantId) : null;
        var fishData = (tag.fishId > 0) ? jsonReader.GetFishById(tag.fishId) : null;
        var animalData = (tag.animalId > 0) ? jsonReader.GetLivestockById(tag.animalId) : null;
        
        if (plantData == null && fishData == null && animalData == null)
        { 
            Debug.LogWarning("Không tìm thấy dữ liệu phù hợp trong JSON."); 
            return; 
        }

        // Iterate through all plots.
        // Duyệt qua tất cả các ô.
        for (var i = 0; i < plotPoints.Length; i++)
        {
            // Skip if already planted.
            // Bỏ qua nếu đã trồng.
            if (isPlanted[i]) continue;

            var parent = plotPoints[i];
            
            // Instantiate prefab as child of plot.
            // Instantiate prefab làm con của ô.
            var go = Instantiate(plantPrefab, parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = plantPrefab.transform.localScale;
            
            go.SetActive(true);

            // Get or add PlantGrowth component.
            // Lấy hoặc thêm component PlantGrowth.
            var growth = go.GetComponent<Thuan_23127_PlantGrowth>() ?? go.AddComponent<Thuan_23127_PlantGrowth>();

            // Setup XR interactable colliders.
            // Setup collider cho XR interactable.
            var xrInteractable = go.GetComponent<XRSimpleInteractable>();
            if (xrInteractable != null && xrInteractable.colliders.Count == 0)
            {
                var colliders = go.GetComponentsInChildren<Collider>();
                foreach (var col in colliders)
                {
                    if (!xrInteractable.colliders.Contains(col))
                        xrInteractable.colliders.Add(col);
                }
            }

            // Re-enable hover health to find panel.
            // Bật lại hover health để tìm panel.
            var hoverHealth = go.GetComponent<Thuan_23127_HoverHealthXR>();
            if (hoverHealth != null)
            {
                hoverHealth.enabled = false;
                hoverHealth.enabled = true;
            }

            // Wire plant to area score tracking.
            // Wire cây vào theo dõi điểm vùng.
            WireGrowthForAreaTotals(growth);

            // Track in seasonal summary.
            // Theo dõi trong tổng kết mùa.
            var summary = Thuan_23127_SeasonalSummary.Instance;
            if (summary) summary.Track(growth, tag);
            
            // Bind to HUD for progress display.
            // Bind vào HUD để hiển thị tiến độ.
            BindGrowthToHUD(growth);

            // Initialize with correct data type.
            // Khởi tạo với loại dữ liệu đúng.
            var readerForThis = fillAll ? null : jsonReader;
            if (plantData != null) growth.Init(plantData, this, i, readerForThis);
            else if (animalData != null) growth.Init(animalData, this, i, readerForThis);
            else if (fishData != null) growth.Init(fishData, this, i, readerForThis);

            // Set HUD icon.
            // Đặt icon HUD.
            if (hud) hud.SetSubject(tag.hudIcon);

            isPlanted[i] = true;
            
            // If not filling all, stop after first plant.
            // Nếu không trồng tất cả, dừng sau khi trồng 1 cây.
            if (!fillAll) break;
        }
    }

    // =========================================================================
    // FreePlot - Marks a plot as available after plant is destroyed.
    // FreePlot - Đánh dấu ô là trống sau khi cây bị hủy.
    // 
    // Called by: PlantGrowth when it destroys itself after harvest delay.
    // Được gọi bởi: PlantGrowth khi tự hủy sau delay thu hoạch.
    // =========================================================================
    public void FreePlot(int index)
    {
        if (index >= 0 && index < isPlanted.Length) 
            isPlanted[index] = false;
    }
    
    // =========================================================================
    // FreezeHUD - Hides HUD when game ends.
    // FreezeHUD - Ẩn HUD khi game kết thúc.
    // =========================================================================
    public void FreezeHUD()
    {
        UnbindCurrent();         
        if (hud) hud.Show(false);
    }

    // =========================================================================
    // ResetAllPlots - Clears all plants and resets area state.
    // ResetAllPlots - Xóa tất cả cây và reset trạng thái vùng.
    // 
    // Called by: RestartGame, testing, or manual reset.
    // Được gọi bởi: RestartGame, testing, hoặc reset thủ công.
    // =========================================================================
    public void ResetAllPlots()
    {
        UnbindCurrent();

        // Remove all event subscriptions.
        // Gỡ tất cả đăng ký sự kiện.
        foreach (var g in _growths)
        {
            if (g) g.OnHarvested -= null;
        }
        _growths.Clear();

        // Destroy all plants in all plots.
        // Hủy tất cả cây trong tất cả ô.
        for (int i = 0; i < plotPoints.Length; i++)
        {
            var p = plotPoints[i];
            if (!p) continue;
            for (int c = p.childCount - 1; c >= 0; c--)
                Destroy(p.GetChild(c).gameObject);
            isPlanted[i] = false;
        }

        // Reset season totals.
        // Reset tổng mùa.
        _seasonTotals[0] = _seasonTotals[1] = _seasonTotals[2] = 0;
        
        if (hud)
        {
            hud.SetProgress(0f);
            hud.SetSeasonScoresPhase(0, 0);
            hud.Show(true);
        }
    }
    
    // =========================================================================
    // GetAllGrowths - Returns a snapshot copy of all active plants.
    // GetAllGrowths - Trả về bản sao snapshot của tất cả cây đang hoạt động.
    // 
    // Why copy: Safe iteration during modifications.
    // Tại sao copy: Duyệt an toàn trong khi có thay đổi.
    // =========================================================================
    public List<Thuan_23127_PlantGrowth> GetAllGrowths()
    {
        return new List<Thuan_23127_PlantGrowth>(_growths);
    }

    // =========================================================================
    // SettleAndClearForNewSeason - Force harvest all plants for season change.
    // SettleAndClearForNewSeason - Ép thu hoạch tất cả cây khi đổi mùa.
    // 
    // Called by: GameRulesProvider.SetPhase() in Seasonal scoring mode.
    // Được gọi bởi: GameRulesProvider.SetPhase() trong chế độ Seasonal.
    // 
    // Steps:
    // 1. Force harvest all active plants (calculates remaining score)
    // 2. Clear plot tracking
    // 3. Reset HUD
    // 
    // Các bước:
    // 1. Ép thu hoạch tất cả cây đang hoạt động (tính điểm còn lại)
    // 2. Xóa theo dõi ô
    // 3. Reset HUD
    // =========================================================================
    public void SettleAndClearForNewSeason()
    {
        // Get snapshot to avoid modification during iteration.
        // Lấy snapshot để tránh thay đổi khi đang duyệt.
        var snapshot = GetAllGrowths();
        
        foreach (var g in snapshot)
        {
            if (!g) continue;
            // Ensure plant uses this area's salinity for final calculation.
            // Đảm bảo cây dùng độ mặn của vùng này cho tính toán cuối.
            g.SetSalinityProvider(GetAreaSalinity);
            g.ForceHarvestImmediateAndDestroy();
        }
        _growths.Clear();

        // Reset season totals and HUD.
        // Reset tổng mùa và HUD.
        _seasonTotals[0] = _seasonTotals[1] = _seasonTotals[2] = 0;
        if (hud)
        {
            hud.SetProgress(0f);
            hud.SetSeasonScoresPhase(0, 0);
            hud.SetDescription(string.Empty);
            hud.Show(true);
        }
    }
    
    // =========================================================================
    // GetCurrentSalinityForServer - Returns salinity for server reporting.
    // GetCurrentSalinityForServer - Trả về độ mặn để báo cáo server.
    // =========================================================================
    public float GetCurrentSalinityForServer()
    {
        return GetAreaSalinity();
    }

    // =========================================================================
    // GetServerAreaId - Returns unique ID for this area (for server).
    // GetServerAreaId - Trả về ID duy nhất cho vùng này (cho server).
    // =========================================================================
    public string GetServerAreaId()
    {
        return string.IsNullOrEmpty(serverAreaId) ? name : serverAreaId;
    }
}
