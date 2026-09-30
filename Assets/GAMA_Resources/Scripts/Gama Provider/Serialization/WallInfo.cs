using System.Collections.Generic;
using UnityEngine;

// EN: Invisible wall geometry from GAMA. Defines collision-only boundaries.
//     Received via key "wallId" in HandleServerMessageReceived.
//     NOTE: manageWalls() is currently commented out in SimulationManager.
// VI: Hình học tường vô hình từ GAMA. Định nghĩa ranh giới chỉ có collision.
//     Nhận qua khóa "wallId" trong HandleServerMessageReceived.
//     GHI CHÚ: manageWalls() hiện đang bị comment trong SimulationManager.
[System.Serializable]
public class WallInfo
{
    // EN: Unique wall identifier.
    // VI: Định danh tường duy nhất.
    public string wallId;
    // EN: Y-axis offsets for each wall polygon (precision-scaled).
    // VI: Offset trục Y cho mỗi polygon tường (scale theo precision).
    public List<int> offsetYGeom;
    // EN: Extrusion height (precision-scaled).
    // VI: Chiều cao đùn (scale theo precision).
    public int height;
    // EN: Polygon vertex data for wall geometry.
    // VI: Dữ liệu đỉnh polygon cho hình học tường.
    public List<GAMAPoint> pointsGeom;

    public static WallInfo CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<WallInfo>(jsonString);
    }

}
