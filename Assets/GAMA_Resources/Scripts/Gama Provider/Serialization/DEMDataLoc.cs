using System.Collections.Generic;
using UnityEngine;

// EN: Partial DEM update from GAMA. Updates a subregion of a Terrain’s
//     heightmap starting at (indexX, indexY). Supports dynamic terrain
//     modification during simulation.
//     Received via key "indexX" in HandleServerMessageReceived.
// VI: Cập nhật DEM từng phần từ GAMA. Cập nhật vùng con của heightmap
//     Terrain bắt đầu tại (indexX, indexY). Hỗ trợ thay đổi địa hình
//     động trong quá trình mô phỏng.
//     Nhận qua khóa "indexX" trong HandleServerMessageReceived.
[System.Serializable]
public class DEMDataLoc
{
    // EN: Height grid rows for the subregion patch.
    // VI: Các hàng lưới độ cao cho miếng vá vùng con.
    public List<Row> rows;
    // EN: Terrain name to target.
    // VI: Tên Terrain đích.
    public string id;
    // EN: Starting X index in the heightmap grid.
    // VI: Chỉ số X bắt đầu trong lưới heightmap.
    public int indexX; 
    // EN: Starting Y index in the heightmap grid.
    // VI: Chỉ số Y bắt đầu trong lưới heightmap.
    public int indexY;
    // EN: Maximum height value for normalization (may rescale existing terrain).
    // VI: Giá trị độ cao tối đa để chuẩn hóa (có thể co giãn terrain hiện tại).
    public int valMax;
  

    public static DEMDataLoc CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<DEMDataLoc>(jsonString);
    }
     
} 
 




