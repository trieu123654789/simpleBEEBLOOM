using System.Collections.Generic;
using UnityEngine;

// EN: Simple flag message from GAMA to enable/disable player movement.
//     Received via key "enableMove" in HandleServerMessageReceived.
// VI: Message cờ đơn giản từ GAMA để bật/tắt di chuyển người chơi.
//     Nhận qua khóa "enableMove" trong HandleServerMessageReceived.
[System.Serializable]
public class EnableMoveInfo
{
    // EN: True = player can move; false = player is frozen.
    // VI: True = người chơi được di chuyển; false = người chơi bị đóng băng.
    public bool enableMove;

    public static EnableMoveInfo CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<EnableMoveInfo>(jsonString);
    } 

} 


