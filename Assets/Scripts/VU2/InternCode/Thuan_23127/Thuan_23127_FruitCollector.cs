using UnityEngine;

/// <summary>
/// Script gắn vào túi - quản lý tổng điểm khi thu thập trái cây
/// Yêu cầu: Túi cần có Collider và tag "Bag"
/// </summary>
public class Thuan_23127_FruitCollector : MonoBehaviour
{
    [Header("Điểm số")]
    [SerializeField] private int totalScore = 0;  // Tổng điểm hiện tại
    
    /// <summary>
    /// Lấy tổng điểm hiện tại
    /// </summary>
    public int TotalScore => totalScore;
    
    /// <summary>
    /// Thêm điểm khi thu thập trái cây
    /// </summary>
    /// <param name="points">Số điểm cần thêm</param>
    public void AddScore(int points)
    {
        totalScore += points;
        Debug.Log($"Tổng điểm hiện tại: {totalScore}");
    }
    
    /// <summary>
    /// Reset điểm về 0
    /// </summary>
    public void ResetScore()
    {
        totalScore = 0;
        Debug.Log("Đã reset điểm về 0");
    }
}
