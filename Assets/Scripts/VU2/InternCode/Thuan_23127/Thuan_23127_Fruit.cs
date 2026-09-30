using UnityEngine;

/// <summary>
/// Script gắn vào trái cây - xử lý khi chạm vào túi
/// Yêu cầu: Trái cây cần có Collider (đánh dấu Is Trigger) và Rigidbody
/// </summary>
public class Thuan_23127_Fruit : MonoBehaviour
{
    [Header("Cài đặt điểm")]
    [SerializeField] private int pointValue = 10;  // Điểm số khi thu thập trái cây này
    
    [Header("Tag của túi")]
    [SerializeField] private string bagTag = "Bag";  // Tag của túi để nhận diện
    
    /// <summary>
    /// Xử lý khi trigger collider chạm nhau (Is Trigger = true)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem object chạm vào có phải là túi không
        if (other.CompareTag(bagTag))
        {
            CollectFruit();
        }
    }
    
    /// <summary>
    /// Xử lý khi collider va chạm (Is Trigger = false)
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // Kiểm tra xem object chạm vào có phải là túi không
        if (collision.gameObject.CompareTag(bagTag))
        {
            CollectFruit();
        }
    }
    
    /// <summary>
    /// Thu thập trái cây - log điểm và biến mất
    /// </summary>
    private void CollectFruit()
    {
        // Debug log điểm số
        Debug.Log($"Đã thu thập trái cây! +{pointValue} điểm");
        
        // Biến mất trái cây (destroy game object)
        Destroy(gameObject);
    }
}
