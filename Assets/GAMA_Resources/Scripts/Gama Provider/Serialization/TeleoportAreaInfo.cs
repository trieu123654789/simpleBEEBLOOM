using System.Collections.Generic;
using UnityEngine;

// EN: Teleportation area definition from GAMA. Generates mesh colliders that
//     are assigned to XR TeleportationArea for VR locomotion.
//     Received via key "teleportId" in HandleServerMessageReceived.
// VI: Định nghĩa vùng teleport từ GAMA. Tạo mesh collider được
//     gán cho XR TeleportationArea để di chuyển VR.
//     Nhận qua khóa "teleportId" trong HandleServerMessageReceived.
[System.Serializable]
public class TeleoportAreaInfo
{
    // EN: Y-axis offsets for each polygon (precision-scaled).
    // VI: Offset trục Y cho mỗi polygon (scale theo precision).
    public List<int> offsetYGeom;
    // EN: Polygon vertex data.
    // VI: Dữ liệu đỉnh polygon.
    public List<GAMAPoint> pointsGeom;
    // EN: Extrusion height (precision-scaled).
    // VI: Chiều cao đùn (scale theo precision).
    public int height;
    // EN: Unique teleport area identifier.
    // VI: Định danh vùng teleport duy nhất.
    public string teleportId;

    public static TeleoportAreaInfo CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<TeleoportAreaInfo>(jsonString);
    }

}