using System.Collections.Generic;
using UnityEngine;

// EN: Initial connection parameters sent by GAMA when a player joins.
//     Contains coordinate precision, spawn position, world bounds,
//     hotspot names, and the minimum interval for position updates.
//     Received via key "precision" in HandleServerMessageReceived.
// VI: Tham số kết nối ban đầu do GAMA gửi khi người chơi tham gia.
//     Chứa độ chính xác tọa độ, vị trí spawn, giới hạn thế giới,
//     tên hotspot và khoảng thời gian tối thiểu giữa các lần gửi vị trí.
//     Nhận qua khóa "precision" trong HandleServerMessageReceived.
[System.Serializable]
public class ConnectionParameter
{
    // EN: Coordinate multiplier — all GAMA coordinates are integers scaled by this factor.
    // VI: Hệ số nhân tọa độ — mọi tọa độ GAMA là số nguyên nhân với hệ số này.
    public int precision;
    // EN: Player’s initial spawn position in GAMA CRS [x, y].
    // VI: Vị trí spawn ban đầu của người chơi trong CRS GAMA [x, y].
    public List<int> position;
    // EN: World bounding box dimensions in GAMA CRS [width, height].
    // VI: Kích thước bounding box thế giới trong CRS GAMA [chiều rộng, chiều cao].
    public List<int> world;

    // EN: Named points of interest in the simulation (optional).
    // VI: Các điểm quan tâm được đặt tên trong mô phỏng (tùy chọn).
    public List<string> hotspots;
    // EN: Minimum interval (in precision-scaled ms) between player position updates to GAMA.
    // VI: Khoảng thời gian tối thiểu (ms đã scale theo precision) giữa các lần gửi vị trí lên GAMA.
    public int minPlayerUpdateDuration;

    public static ConnectionParameter CreateFromJSON(string jsonString) {
        return JsonUtility.FromJson<ConnectionParameter>(jsonString);
    }

}