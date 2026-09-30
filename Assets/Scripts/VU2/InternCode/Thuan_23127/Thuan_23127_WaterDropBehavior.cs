using UnityEngine;

public class Thuan_23127_WaterDropBehavior : MonoBehaviour
{
    private Vector3 startPoint;
    private Vector3 endPoint;
    private float moveSpeed;
    private float journeyLength;
    private float startTime;
    private float arcHeight = 2.0f; // Độ cao của vòng cung nước
    private bool isSetup = false;   // Guard: true after Setup() called / Cờ: true sau khi Setup() được gọi

    public void Setup(Vector3 target, float speed)
    {
        startPoint = transform.position;
        endPoint = target;
        moveSpeed = speed;
        journeyLength = Vector3.Distance(startPoint, endPoint);
        startTime = Time.time;
        isSetup = true;
        
        // Xoay đầu hạt nước hướng về đích
        transform.LookAt(endPoint);
    }

    private  void Update()
    {
        // Don't move if Setup() hasn't been called (prevents NaN).
        // Không di chuyển nếu Setup() chưa được gọi (tránh NaN).
        if (!isSetup) return;

        // Tính toán quãng đường đã đi được (từ 0 đến 1)
        var distCovered = (Time.time - startTime) * moveSpeed;
        var fractionOfJourney = distCovered / journeyLength;

        if (fractionOfJourney >= 1)
        {
            // Đã đến nơi -> Gọi hàm nổ nước rồi hủy
            SpawnSplashEffect();
            Destroy(gameObject);
            return;
        }

        // --- CÔNG THỨC DI CHUYỂN PARABOL ---
        // 1. Tìm vị trí thẳng hàng (Linear)
        var currentPos = Vector3.Lerp(startPoint, endPoint, fractionOfJourney);

        // 2. Cộng thêm độ cao (Cong lên ở giữa và thấp dần về 2 đầu)
        // Hàm Mathf.Sin(fraction * PI) sẽ trả về 0 ở đầu, 1 ở giữa, 0 ở cuối -> Tạo hình vòng cung
        currentPos.y += Mathf.Sin(fractionOfJourney * Mathf.PI) * arcHeight;

        // Cập nhật vị trí
        transform.position = currentPos;
    }

    private void SpawnSplashEffect()
    {
        // Code sinh ra hiệu ứng nước bắn tung tóe ở đây
        
        
    }
}