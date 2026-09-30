using UnityEngine;
using System.Collections.Generic;

// EN: Spawn rate update for enemy spawners, sent periodically by GAMA.
//     Maps spawner IDs (Unity InstanceID as string) to their new spawn rates.
//     Received via key "enemyspawners" in HandleServerMessageReceived.
// VI: Cập nhật tốc độ spawn cho các enemy spawner, GAMA gửi định kỳ.
//     Ánh xạ ID spawner (InstanceID Unity dạng string) với spawn rate mới.
//     Nhận qua khóa "enemyspawners" trong HandleServerMessageReceived.
[System.Serializable]
public class EnemySpawnerInfo
{
    // EN: Spawner InstanceID strings — parallel array with spawnrates.
    // VI: Chuỗi InstanceID của spawner — mảng song song với spawnrates.
    public List<string> enemyspawners;
    // EN: New spawn rates (precision-scaled integers) for each spawner.
    // VI: Tốc độ spawn mới (số nguyên scale theo precision) cho từng spawner.
    public List<int> spawnrates;

    public static EnemySpawnerInfo CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<EnemySpawnerInfo>(jsonString);
    }

}


