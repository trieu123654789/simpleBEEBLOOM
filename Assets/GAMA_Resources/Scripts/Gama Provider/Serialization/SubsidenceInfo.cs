using UnityEngine;
using System.Collections.Generic;

// EN: Subsidence simulation data from GAMA. Contains water level readings
//     and a subsidence score used to drive the SubsidenceManager.
//     Received via key "subsidences" in HandleServerMessageReceived.
// VI: Dữ liệu mô phỏng sụt lún từ GAMA. Chứa mức nước
//     và điểm số sụt lún dùng để điều khiển SubsidenceManager.
//     Nhận qua khóa "subsidences" trong HandleServerMessageReceived.
[System.Serializable]
public class SubsidenceInfo
{
    // EN: List of subsidence zone identifiers.
    // VI: Danh sách định danh vùng sụt lún.
    public List<string> subsidences;
    // EN: Local water level reading (precision-scaled integer).
    // VI: Mức nước cục bộ (số nguyên đã scale theo precision).
    public int waterLocal;
    // EN: Global water level reading (precision-scaled integer).
    // VI: Mức nước toàn cục (số nguyên đã scale theo precision).
    public int waterGlobal;

    // EN: Subsidence severity score computed by GAMA.
    // VI: Điểm đánh giá mức độ sụt lún do GAMA tính toán.
    public float subsi_score;

    public static SubsidenceInfo CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<SubsidenceInfo>(jsonString);
    }

}


