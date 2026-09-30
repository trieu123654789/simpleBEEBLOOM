using System.Collections.Generic;
using UnityEngine;

// EN: Full Digital Elevation Model (DEM) data from GAMA. Replaces the entire
//     heightmap of a named Terrain with the provided grid values.
//     Received via key "rows" in HandleServerMessageReceived.
// VI: Dữ liệu Mô hình Số Độ cao (DEM) đầy đủ từ GAMA. Thay thế toàn bộ
//     heightmap của Terrain được đặt tên bằng lưới giá trị được cung cấp.
//     Nhận qua khóa "rows" trong HandleServerMessageReceived.
[System.Serializable]
public class DEMData
{
    // EN: Height grid rows — each Row has a list of integer heights.
    // VI: Các hàng lưới độ cao — mỗi Row có danh sách số nguyên độ cao.
    public List<Row> rows;
    // EN: Terrain name to target.
    // VI: Tên Terrain đích.
    public string id;
    // EN: Maximum height value (used to normalize heights to 0–1 range).
    // VI: Giá trị độ cao tối đa (dùng để chuẩn hóa độ cao về khoảng 0–1).
    public int valMax;
    // EN: Terrain width in world units.
    // VI: Chiều rộng Terrain theo đơn vị thế giới.
    public int sizeX;
    // EN: Terrain depth in world units.
    // VI: Chiều sâu Terrain theo đơn vị thế giới.
    public int sizeY;

    public static DEMData CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<DEMData>(jsonString);
    }

}


// EN: A single row of integer height values in the DEM grid.
// VI: Một hàng đơn của giá trị độ cao số nguyên trong lưới DEM.
[System.Serializable]
public class Row
{
    // EN: Height values for each column in this row.
    // VI: Giá trị độ cao cho mỗi cột trong hàng này.
    public List<int> h;
}




