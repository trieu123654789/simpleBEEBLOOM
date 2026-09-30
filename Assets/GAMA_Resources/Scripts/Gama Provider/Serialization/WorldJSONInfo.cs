using System.Collections.Generic;
using UnityEngine;

// EN: Initial world geometry data from GAMA. Contains named objects, their positions,
//     property IDs, polygon geometries with Y offsets, and multiplayer ranking info.
//     Received via key "pointsLoc" in HandleServerMessageReceived (only parsed once).
// VI: Dữ liệu hình học thế giới ban đầu từ GAMA. Chứa đối tượng có tên, vị trí,
//     ID thuộc tính, hình học polygon với offset Y, và thông tin xếp hạng multiplayer.
//     Nhận qua khóa "pointsLoc" trong HandleServerMessageReceived (chỉ parse một lần).
[System.Serializable]
public class WorldJSONInfo
{
    // EN: Flat list of coordinates for agent locations [x1,y1, x2,y2, ...].
    // VI: Danh sách tọa độ phẳng cho vị trí agent [x1,y1, x2,y2, ...].
    public List<int> position;
    // EN: Object names corresponding to each position entry.
    // VI: Tên đối tượng tương ứng với mỗi mục vị trí.
    public List<string> names;
    // EN: Subset of names that should be preserved across updates.
    // VI: Tập con các tên cần giữ lại qua các lần cập nhật.
    public List<string> keepNames;
    // EN: Property ID for each object — maps to PropertiesGAMA in propertyMap.
    // VI: ID thuộc tính cho mỗi đối tượng — ánh xạ tới PropertiesGAMA trong propertyMap.
    public List<string> propertyID;
    // EN: Agent location points in GAMA coordinate system.
    // VI: Điểm vị trí agent trong hệ tọa độ GAMA.
    public List<GAMAPoint> pointsLoc;

    // EN: Y-axis offsets for each polygon geometry (precision-scaled).
    // VI: Offset trục Y cho mỗi hình học polygon (scale theo precision).
    public List<int> offsetYGeom;
    // EN: Polygon vertex data for 3D geometry generation.
    // VI: Dữ liệu đỉnh polygon để tạo hình học 3D.
    public List<GAMAPoint> pointsGeom;

    // EN: Player ranking scores (multiplayer).
    // VI: Điểm xếp hạng người chơi (multiplayer).
    public List<int> ranking;
    // EN: Player names/IDs (multiplayer).
    // VI: Tên/ID người chơi (multiplayer).
    public List<string> players;
    // EN: Number of interaction tokens available.
    // VI: Số lượng token tương tác khả dụng.
    public int numTokens;
    // EN: Whether this world data represents the initial snapshot (first load).
    // VI: Dữ liệu thế giới này có phải là bản chụp ban đầu (lần tải đầu tiên) không.
    public bool isInit;

    public static WorldJSONInfo CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<WorldJSONInfo>(jsonString);
    } 

} 


// EN: A 2D/3D point in GAMA coordinate reference system, stored as integer list.
//     c = [x, y] for 2D or [x, y, z] for 3D, all precision-scaled.
// VI: Một điểm 2D/3D trong hệ tọa độ GAMA, lưu dạng danh sách số nguyên.
//     c = [x, y] cho 2D hoặc [x, y, z] cho 3D, tất cả đã scale theo precision.
[System.Serializable]
public class GAMAPoint
{
    public List<int> c;
}


