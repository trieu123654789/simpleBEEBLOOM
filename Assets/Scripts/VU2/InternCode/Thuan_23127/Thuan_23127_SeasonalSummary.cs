using System;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// ProductKind - Categorizes entities for summary tracking.
// ProductKind - Ph�n lo?i th?c th? d? theo d�i t?ng k?t.
// =============================================================================
public enum ProductKind { Plant, Animal, Fish }

// =============================================================================
// SeasonalCounters - Stores harvest counts and scores per phase (3 phases).
// SeasonalCounters - Luu tr? s? l?n thu ho?ch v� di?m theo giai do?n (3 G�).
//
// Phase 0 = G�1 (T11�T1), Phase 1 = G�2 (T2�T3), Phase 2 = G�3 (T4)
// =============================================================================
[Serializable]
public class SeasonalCounters
{
    // Icon for this product type.
    // Icon cho lo?i s?n ph?m n�y.
    public Sprite icon;
    
    // Harvest count per phase: [0]=G�1, [1]=G�2, [2]=G�3.
    // S? l?n thu ho?ch theo giai do?n: [0]=G�1, [1]=G�2, [2]=G�3.
    public int[] count = new int[3];
    
    // Total score earned per phase: [0]=G�1, [1]=G�2, [2]=G�3.
    // T?ng di?m ki?m du?c theo giai do?n: [0]=G�1, [1]=G�2, [2]=G�3.
    public int[] score = new int[3];
}

// =============================================================================
// Thuan_23127_SeasonalSummary - Tracks and summarizes all harvests by product type.
// Thuan_23127_SeasonalSummary - Theo d�i v� t?ng k?t t?t c? thu ho?ch theo lo?i SP.
// 
// 3-phase system:
//   Phase 0 (G�1): T11�T1 (MonthIndex 1�3)
//   Phase 1 (G�2): T2�T3  (MonthIndex 4�5)
//   Phase 2 (G�3): T4     (MonthIndex 6)
//
// H? th?ng 3 giai do?n:
//   G� 0 (G�1): T11�T1 (MonthIndex 1�3)
//   G� 1 (G�2): T2�T3  (MonthIndex 4�5)
//   G� 2 (G�3): T4     (MonthIndex 6)
// =============================================================================
public class Thuan_23127_SeasonalSummary : MonoBehaviour
{
    // =========================================================================
    // SINGLETON INSTANCE
    // INSTANCE SINGLETON
    // =========================================================================
    public static Thuan_23127_SeasonalSummary Instance;

    // =========================================================================
    // DATA STORAGE
    // LUU TR? D? LI?U
    // =========================================================================
    
    // Dictionary mapping "ProductKind:ID" to counters.
    // Dictionary map "ProductKind:ID" sang b? d?m.
    private readonly Dictionary<string, SeasonalCounters> _map = new();
    
    // Event fired when data changes (for UI refresh).
    // S? ki?n du?c b?n khi d? li?u thay d?i (d? refresh UI).
    public event Action OnChanged;

    // =========================================================================
    // Awake - Singleton setup.
    // Awake - Thi?t l?p Singleton.
    // =========================================================================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    // =========================================================================
    // CurrentPhaseIndex - Returns current phase index (0, 1, or 2).
    // CurrentPhaseIndex - Tr? v? index giai do?n hi?n t?i (0, 1, ho?c 2).
    //
    // Based on CurrentMonthIndex from RulesoftheGame_VU2_1:
    //   MonthIndex 1�3 ? Phase 0 (G�1: T11�T1)
    //   MonthIndex 4�5 ? Phase 1 (G�2: T2�T3)
    //   MonthIndex 6   ? Phase 2 (G�3: T4)
    //
    // D?a tr�n CurrentMonthIndex t? RulesoftheGame_VU2_1:
    //   MonthIndex 1�3 ? Phase 0 (G�1: T11�T1)
    //   MonthIndex 4�5 ? Phase 1 (G�2: T2�T3)
    //   MonthIndex 6   ? Phase 2 (G�3: T4)
    // =========================================================================
    private static int CurrentPhaseIndex()
    {
        int month = GameRulesProvider.CurrentMonthIndex;
        if (month <= 3) return 0;  // G�1: T11, T12, T1
        if (month <= 5) return 1;  // G�2: T2, T3
        return 2;                   // G�3: T4
    }

    // =========================================================================
    // Key - Generates unique key for product lookup.
    // Key - T?o key duy nh?t d? tra c?u s?n ph?m.
    // =========================================================================
    private static string Key(ProductKind kind, int id) => $"{kind}:{id}";

    // =========================================================================
    // GetOrCreate - Gets existing counters or creates new ones.
    // GetOrCreate - L?y b? d?m hi?n c� ho?c t?o m?i.
    // =========================================================================
    private SeasonalCounters GetOrCreate(ProductKind kind, int id, Sprite icon)
    {
        var key = Key(kind, id);
        if (!_map.TryGetValue(key, out var c))
        {
            c = new SeasonalCounters { icon = icon };
            _map[key] = c;
        }
        // Update icon if not set yet.
        // C?p nh?t icon n?u chua c�.
        if (c.icon == null && icon != null) c.icon = icon;
        return c;
    }

    // =========================================================================
    // Track - Registers a plant/animal/fish for harvest tracking.
    // Track - �ang k� m?t c�y/d?ng v?t/c� d? theo d�i thu ho?ch.
    // 
    // Called by: FarmArea.PlantInternal() when planting.
    // �u?c g?i b?i: FarmArea.PlantInternal() khi tr?ng.
    // 
    // When the entity is harvested:
    // - Determine current phase from CurrentMonthIndex
    // - Increment count for that phase
    // - Add points to phase total
    // - Fire OnChanged event for UI refresh
    // 
    // Khi th?c th? du?c thu ho?ch:
    // - X�c d?nh giai do?n hi?n t?i t? CurrentMonthIndex
    // - Tang s? d?m cho giai do?n d�
    // - C?ng di?m v�o t?ng giai do?n
    // - B?n s? ki?n OnChanged d? refresh UI
    // =========================================================================
    public void Track(Thuan_23127_PlantGrowth growth, Thuan_23127_SeedTag tag)
    {
        if (!growth || !tag) return;

        // Determine product kind and ID from seed tag.
        // X�c d?nh lo?i s?n ph?m v� ID t? seed tag.
        ProductKind kind;
        int id;
        if      (tag.plantId  > 0) { kind = ProductKind.Plant;  id = tag.plantId;  }
        else if (tag.animalId > 0) { kind = ProductKind.Animal; id = tag.animalId; }
        else if (tag.fishId   > 0) { kind = ProductKind.Fish;   id = tag.fishId;   }
        else return;

        // Subscribe to harvest event.
        // �ang k� s? ki?n thu ho?ch.
        growth.OnHarvested += points =>
        {
            int phase = CurrentPhaseIndex();  // 0=G�1, 1=G�2, 2=G�3
            var st = GetOrCreate(kind, id, tag.hudIcon);
            st.count[phase] += 1;
            st.score[phase] += points;
            OnChanged?.Invoke();
        };

        // No need to explicitly unsubscribe - object will be GC'd with its events.
        // Kh�ng c?n h?y dang k� - object s? du?c GC c�ng v?i events c?a n�.
        growth.OnAboutToDestroy += () => { /* no-op */ };
    }
    
    // =========================================================================
    // TrackDirect - Direct tracking for grab-based items (Egg, Shrimp, Fruit).
    // TrackDirect - Theo d�i tr?c ti?p cho items d�ng grab (Tr?ng, T�m, Qu?).
    // 
    // Called by: David_EggGrab, David_ShrimpGrab, David_Fruit when collecting.
    // �u?c g?i b?i: David_EggGrab, David_ShrimpGrab, David_Fruit khi thu ho?ch.
    // 
    // This bypasses PlantGrowth and directly records points.
    // �i?u n�y b? qua PlantGrowth v� ghi nh?n di?m tr?c ti?p.
    // =========================================================================
    public void TrackDirect(string productName, Sprite icon, int points)
    {
        // Use product name as unique key (e.g., "Egg", "Shrimp", "Coconut")
        var key = $"Direct:{productName}";
        
        int phase = CurrentPhaseIndex(); // 0=G�1, 1=G�2, 2=G�3
        var counters = GetOrCreate_Direct(key, icon);
        
        counters.score[phase] += points;
        counters.count[phase] += 1;
        
        OnChanged?.Invoke();
    }
    
    private SeasonalCounters GetOrCreate_Direct(string key, Sprite icon)
    {
        if (!_map.TryGetValue(key, out var c))
        {
            c = new SeasonalCounters { icon = icon };
            _map[key] = c;
        }
        if (c.icon == null && icon != null) c.icon = icon;
        return c;
    }

    // =========================================================================
    // GetAllPhaseData - Returns full 3-phase data for all product types.
    // GetAllPhaseData - Tr? v? d? li?u d?y d? 3 giai do?n cho t?t c? SP.
    // 
    // Returns: List of tuples:
    //   (key, icon, score[3], count[3])
    //   - key    = product identifier (e.g., "Direct:Durian", "Plant:1")
    //   - score[i] = total score in phase i
    //   - count[i] = harvest count in phase i (area = count � 10)
    // 
    // Tr? v?: Danh s�ch tuple:
    //   (key, icon, score[3], count[3])
    //   - key    = d?nh danh s?n ph?m (v� d?: "Direct:Durian", "Plant:1")
    //   - score[i] = t?ng di?m ? giai do?n i
    //   - count[i] = s? l?n thu ho?ch ? giai do?n i (di?n t�ch = count � 10)
    // =========================================================================
    public List<(string key, Sprite icon, int[] scores, int[] counts)> GetAllPhaseData()
    {
        var list = new List<(string, Sprite, int[], int[])>();
        foreach (var kv in _map)
        {
            var c = kv.Value;
            // Clone arrays to prevent external mutation.
            // Clone m?ng d? tr�nh thay d?i t? b�n ngo�i.
            int[] scores = { c.score[0], c.score[1], c.score[2] };
            int[] counts = { c.count[0], c.count[1], c.count[2] };
            list.Add((kv.Key, c.icon, scores, counts));
        }
        return list;
    }

    // =========================================================================
    // GetAllScores - LEGACY wrapper, returns 2-season data for compatibility.
    // GetAllScores - Wrapper CU, tr? v? d? li?u 2 m�a tuong th�ch.
    // =========================================================================
    public List<(Sprite icon, int rainy, int dry)> GetAllScores()
    {
        var list = new List<(Sprite, int, int)>();
        foreach (var kv in _map.Values)
            list.Add((kv.icon, kv.score[0], kv.score[1] + kv.score[2]));
        return list;
    }

    // =========================================================================
    // GetAllCounts - Returns harvest counts for all product types (3 phases).
    // GetAllCounts - Tr? v? s? l?n thu ho?ch (3 giai do?n).
    // =========================================================================
    public List<(Sprite icon, int phase1, int phase2, int phase3)> GetAllCounts()
    {
        var list = new List<(Sprite, int, int, int)>();
        foreach (var kv in _map.Values)
            list.Add((kv.icon, kv.count[0], kv.count[1], kv.count[2]));
        return list;
    }
    
    // =========================================================================
    // ResetAllData - Clears all tracking data.
    // ResetAllData - Xóa tất cả dữ liệu theo dõi.
    // 
    // Called by: Game restart to clear statistics.
    // Được gọi bởi: Restart game để xóa thống kê.
    // =========================================================================
    public void ResetAllData()
    {
        _map.Clear();
        
        OnChanged?.Invoke();
        
        Debug.Log("SeasonalSummary: All data has been reset");
    }

    // =========================================================================
    // GetTotalHarvestCount - Returns total harvest count across all 3 phases
    //                        for keys containing the given product name.
    // GetTotalHarvestCount - Trả về tổng số lần thu hoạch qua 3 giai đoạn
    //                        cho các key chứa tên sản phẩm cho trước.
    // =========================================================================
    public int GetTotalHarvestCount(string productName)
    {
        int total = 0;
        foreach (var kv in _map)
        {
            if (kv.Key.Contains(productName))
            {
                var c = kv.Value;
                total += c.count[0] + c.count[1] + c.count[2];
            }
        }
        return total;
    }

}
