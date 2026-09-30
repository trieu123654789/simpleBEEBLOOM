using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour, ISpawner
{
    // EN: Prefab of the enemy to spawn.
    // VI: Prefab enemy sẽ được tạo ra.
    [SerializeField] private GameObject spawnPrefab;

    // EN: Time interval (seconds) between each spawn. Also used as a divisor to calculate spawnCount in StartAutoSpawn.
    // VI: Khoảng thời gian (giây) giữa mỗi lần spawn. Cũng dùng làm số chia để tính spawnCount trong StartAutoSpawn.
    // *** TIP: Giảm giá trị này để spawn nhanh hơn (nhiều enemy hơn trong cùng thời gian). ***
    [SerializeField] private float spawnRate = 1.0f;

    // EN: Waypoints the enemy will follow after spawning.
    // VI: Các điểm đường đi mà enemy sẽ theo sau khi xuất hiện.
    [SerializeField] private List<Transform> wayPoints;

    // EN: Total number of enemies this spawner will create before stopping.
    // VI: Tổng số enemy spawner này tạo ra trước khi dừng.
    // *** TIP: Tăng giá trị mặc định này để spawn nhiều enemy hơn nếu không dùng StartAutoSpawn. ***
    public int spawnCount = 5;

    // EN: Minimum number of enemies to spawn (lower bound guard).
    // VI: Số enemy tối thiểu (giới hạn dưới, tránh về 0).
    private int minSpawnCount = 1;

    // EN: Maximum number of enemies to spawn (upper bound in StartAutoSpawn formula).
    // VI: Số enemy tối đa (giới hạn trên trong công thức StartAutoSpawn).
    // *** TIP: ĐÂY LÀ ĐIỂM QUAN TRỌNG — hiện tại = 1, tăng lên để cho phép nhiều enemy hơn. ***
    private int maxSpawnCount = 50;

    // EN: Counter tracking how many enemies have been spawned so far.
    // VI: Biến đếm số enemy đã được spawn.
    private int count = 0;

    // --- Getters / Properties ---

    // EN: Returns the name of the spawn prefab.
    // VI: Trả về tên prefab enemy.
    public string SpawnName
    {
        get { return spawnPrefab.name; }
    }

    // EN: Gets or sets the spawn rate (interval in seconds).
    // VI: Lấy hoặc đặt tốc độ spawn (khoảng thời gian giữa các lần spawn, tính bằng giây).
    public float SpawnRate
    {
        get { return spawnRate; }
        set { spawnRate = value; }
    }

    // EN: Instantiates one enemy at this spawner's position, assigns waypoints, then increments counter.
    //     Stops repeating when spawnCount is reached.
    // VI: Tạo một enemy tại vị trí spawner, gán waypoints, rồi tăng biến đếm.
    //     Dừng lặp khi đạt đủ spawnCount.
    public void Spawn()
    {
        if (!spawnPrefab) return;

        GameObject spawn = Instantiate(spawnPrefab, transform.position, Quaternion.identity, this.gameObject.transform);
        spawn.GetComponent<EnemyController>().SetDestination(wayPoints);
        count++;
        if (count >= spawnCount)
        {
            CancelInvoke();
        }
    }

    // EN: Restarts spawning using spawnRate to derive how many enemies to create.
    //     Formula: spawnCount = spawnRate * 0.5 (floored), minimum = minSpawnCount.
    // VI: Khởi động lại việc spawn, dùng spawnRate để tính số lượng enemy.
    //     Công thức: spawnCount = spawnRate * 0.5 (làm tròn xuống), tối thiểu = minSpawnCount.
    // *** TIP: Thay hệ số 0.5 bằng số lớn hơn (ví dụ 2.0f) để tăng số enemy khi restart. ***
    public void ReStartAutoSpawn(int amount)
    {
        CancelInvoke("Spawn");
        // Sửa: tôn trọng tham số `amount` từ caller (bootstrapper). Nếu amount<=0
        // mới rơi về công thức cũ (spawnRate*0.5).
        if (amount > 0)
            spawnCount = Mathf.Max(minSpawnCount, amount);
        else
            spawnCount = spawnRate == 0 ? minSpawnCount : Mathf.Max(minSpawnCount, (int)(spawnRate * 0.5f));
        count = 0;
        // Spawn từng con cách nhau theo spawnRate (không hải ra cuộn nhanh 0.5s/con).
        InvokeRepeating("Spawn", 0.1f, Mathf.Max(0.1f, spawnRate));
        Debug.Log("[EnemySpawner] rate " + spawnRate + " cnt " + spawnCount);
    }

    // EN: Starts spawning for the first time.
    //     spawnCount = clamp(amount / spawnRate, minSpawnCount, maxSpawnCount).
    //     Since maxSpawnCount = 1, result is always 1 unless you raise maxSpawnCount.
    // VI: Bắt đầu spawn lần đầu tiên.
    //     spawnCount = clamp(amount / spawnRate, minSpawnCount, maxSpawnCount).
    //     Vì maxSpawnCount = 1, kết quả luôn là 1 — hãy tăng maxSpawnCount để thay đổi.
    // *** TIP: Tăng maxSpawnCount (ví dụ = 50) để cho phép công thức tính ra số lớn hơn. ***
    public void StartAutoSpawn(GameObject spawn, int amount)
    {
        spawnPrefab = spawn;
        spawnCount = spawnRate == 0 ? minSpawnCount : Mathf.Max(minSpawnCount, Mathf.Min(maxSpawnCount, (int)(amount / spawnRate)));
        count = 0;
        InvokeRepeating("Spawn", .5f, spawnRate);
    }

}

