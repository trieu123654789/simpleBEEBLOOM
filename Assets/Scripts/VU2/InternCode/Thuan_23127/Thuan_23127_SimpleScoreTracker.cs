using System;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// ProductScore - Stores score data for one product type across seasons.
// ProductScore - Lưu dữ liệu điểm cho một loại sản phẩm qua các mùa.
// =============================================================================
[Serializable]
public class ProductScore
{
    public string productName;
    public Sprite icon;
    public int rainyScore = 0;
    public int dryScore = 0;
    
    public void AddPoints(int points, bool isRainy)
    {
        if (isRainy) rainyScore += points;
        else dryScore += points;
    }
}

// =============================================================================
// Thuan_23127_SimpleScoreTracker - Tracks detailed scores per product per season.
// Thuan_23127_SimpleScoreTracker - Theo dõi điểm chi tiết theo sản phẩm theo mùa.
//
// This singleton replaces SeasonalSummary for the new grab-based items.
// Works with: David_EggGrab, David_ShrimpGrab, David_Fruit
//
// Singleton này thay thế SeasonalSummary cho các item dùng grab mới.
// Hoạt động với: David_EggGrab, David_ShrimpGrab, David_Fruit
// =============================================================================
public class Thuan_23127_SimpleScoreTracker : MonoBehaviour
{
    // =========================================================================
    // SINGLETON
    // =========================================================================
    public static Thuan_23127_SimpleScoreTracker Instance;
    
    // =========================================================================
    // DATA STORAGE
    // =========================================================================
    
    // Dictionary: productName -> ProductScore
    private Dictionary<string, ProductScore> _scores = new Dictionary<string, ProductScore>();
    
    // Event fired when scores change (for UI refresh)
    public event Action OnScoresChanged;
    
    // =========================================================================
    // SETUP
    // =========================================================================
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    // =========================================================================
    // Track - Records a collection event with product, icon, and points.
    // Track - Ghi lại sự kiện thu hoạch với sản phẩm, icon, và điểm.
    //
    // Called by: EggGrab, ShrimpGrab, David_Fruit when collecting items
    // Được gọi bởi: EggGrab, ShrimpGrab, David_Fruit khi thu hoạch items
    // =========================================================================
    public void Track(string productName, Sprite icon, int points)
    {
        // Determine season
        bool isRainy = GameRulesProvider.Saltwater_Intrusion < 1f;
        
        Debug.Log($"[SimpleScoreTracker] Track: {productName}, Points: {points}, Season: {(isRainy ? "Rainy" : "Dry")}, Icon: {(icon != null ? "✓" : "NULL")}");
        
        // Get or create product entry
        if (!_scores.ContainsKey(productName))
        {
            _scores[productName] = new ProductScore
            {
                productName = productName,
                icon = icon
            };
            Debug.Log($"[SimpleScoreTracker] Created new entry for {productName}");
        }
        
        // Add points to appropriate season
        _scores[productName].AddPoints(points, isRainy);
        
        Debug.Log($"[SimpleScoreTracker] {productName} totals: Rainy={_scores[productName].rainyScore}, Dry={_scores[productName].dryScore}");
        
        // Notify listeners
        OnScoresChanged?.Invoke();
    }
    
    // =========================================================================
    // GetAllScores - Returns list of (icon, rainyScore, dryScore) for UI display.
    // GetAllScores - Trả về danh sách (icon, điểm mưa, điểm khô) để hiển thị UI.
    //
    // Used by: TotalBoard to build score table
    // Được dùng bởi: TotalBoard để xây dựng bảng điểm
    // =========================================================================
    public List<(Sprite icon, int rainy, int dry)> GetAllScores()
    {
        Debug.Log($"[SimpleScoreTracker] GetAllScores() called. Total products: {_scores.Count}");
        
        var result = new List<(Sprite, int, int)>();
        
        foreach (var score in _scores.Values)
        {
            result.Add((score.icon, score.rainyScore, score.dryScore));
            Debug.Log($"[SimpleScoreTracker] - {score.productName}: Rainy={score.rainyScore}, Dry={score.dryScore}, Icon={score.icon != null}");
        }
        
        return result;
    }
    
    // =========================================================================
    // ResetAllData - Clears all tracking data.
    // ResetAllData - Xóa tất cả dữ liệu theo dõi.
    //
    // Called by: Game restart
    // Được gọi bởi: Restart game
    // =========================================================================
    public void ResetAllData()
    {
        _scores.Clear();
        OnScoresChanged?.Invoke();
    }
}
