using UnityEngine;
using System.Collections;

public class Thuan_23127_WaterPumpController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject waterDropPrefab; // Kéo prefab hạt nước vào đây
    [SerializeField] private Transform nozzlePoint;      // Vị trí vòi phun (nơi nước chui ra)
    [SerializeField] private float fireRate = 0.5f;      // Tốc độ bắn (giây/giọt)
    [SerializeField] private float dropSpeed = 4f;       // Tốc độ bay của nước

    private Vector3 waterTarget;
    private bool isActive = false;

    // Hàm này để Script Installization gọi khi vừa đặt máy bơm xuống
    public void InitializePump(Vector3 targetPos)
    {
        waterTarget = targetPos;
        isActive = true;
        StartCoroutine(SpawningWaterRoutine());
    }

    IEnumerator SpawningWaterRoutine()
    {
        while (isActive)
        {
            SpawnWaterDrop();
            yield return new WaitForSeconds(fireRate);
        }
    }

    private void SpawnWaterDrop()
    {
        if (waterDropPrefab == null) return;

        // Vị trí sinh ra: Nếu có nozzlePoint thì dùng, không thì dùng chính vị trí máy bơm
        Vector3 spawnPos = nozzlePoint != null ? nozzlePoint.position : transform.position;

        // Tạo hạt nước
        GameObject drop = Instantiate(waterDropPrefab, spawnPos, Quaternion.identity);
        
        // Cài đặt đích đến cho hạt nước
        Thuan_23127_WaterDropBehavior dropScript = drop.GetComponent<Thuan_23127_WaterDropBehavior>();
        if (dropScript != null)
        {
            dropScript.Setup(waterTarget, dropSpeed);
        }
    }
}