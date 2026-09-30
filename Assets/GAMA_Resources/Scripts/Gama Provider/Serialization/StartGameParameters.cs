using System.Collections.Generic;
using UnityEngine;

// EN: Game timing parameters sent by GAMA when the game begins.
//     Defines the duration of the preparation phase and the defense phase.
//     Received via key "startGame" in HandleServerMessageReceived.
// VI: Tham số thời gian game do GAMA gửi khi bắt đầu game.
//     Xác định thời lượng giai đoạn chuẩn bị và giai đoạn phòng thủ.
//     Nhận qua khóa "startGame" trong HandleServerMessageReceived.
[System.Serializable]
public class StartGameParameters
{
    // EN: Duration of the preparation phase (seconds or GAMA time units).
    // VI: Thời lượng giai đoạn chuẩn bị (giây hoặc đơn vị thời gian GAMA).
    public int time_prep;
    // EN: Duration of the defense phase.
    // VI: Thời lượng giai đoạn phòng thủ.
    public int time_def;

    public static StartGameParameters CreateFromJSON(string jsonString) {
        return JsonUtility.FromJson<StartGameParameters>(jsonString);
    }

}