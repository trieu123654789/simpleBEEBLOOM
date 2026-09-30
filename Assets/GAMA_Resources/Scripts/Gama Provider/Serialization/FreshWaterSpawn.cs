using UnityEngine;
using System.Collections.Generic;

// EN: Spawn rate update for fresh-water pumpers, sent periodically by GAMA.
//     Maps pumper IDs (Unity InstanceID as string) to their new spawn rates.
//     Received via key "pumpers" in HandleServerMessageReceived.
// VI: Cập nhật tốc độ spawn cho các pumper nước ngọt, GAMA gửi định kỳ.
//     Ánh xạ ID pumper (InstanceID Unity dạng string) với spawn rate mới.
//     Nhận qua khóa "pumpers" trong HandleServerMessageReceived.
[System.Serializable]
public class FreshWaterSpawn
{
    // EN: Pumper InstanceID strings — parallel array with spawnrates.
    // VI: Chuỗi InstanceID của pumper — mảng song song với spawnrates.
    public List<string> pumpers;
    // EN: New spawn rates (precision-scaled, halved in SimulationManager) for each pumper.
    // VI: Tốc độ spawn mới (scale theo precision, chia 2 trong SimulationManager) cho từng pumper.
    public List<int> spawnrates;

    public static FreshWaterSpawn CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<FreshWaterSpawn>(jsonString);
    }

}


