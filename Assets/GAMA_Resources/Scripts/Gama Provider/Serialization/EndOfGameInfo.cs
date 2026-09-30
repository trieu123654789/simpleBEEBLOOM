
using UnityEngine;

// EN: End-of-game message from GAMA containing a summary string.
//     Stored in StaticInformation.endOfGame and displayed on the result screen.
//     Received via key "endOfGame" in HandleServerMessageReceived.
// VI: Message kết thúc game từ GAMA chứa chuỗi tổng kết.
//     Lưu vào StaticInformation.endOfGame và hiển thị trên màn hình kết quả.
//     Nhận qua khóa "endOfGame" trong HandleServerMessageReceived.
[System.Serializable]
public class EndOfGameInfo
{
    // EN: Human-readable game result text (e.g. "You won!" / "Flood reached critical level").
    // VI: Văn bản kết quả game dạng đọc được (vd: "Bạn thắng!" / "Lũ đạt mức nguy hiểm").
    public string endOfGame;

    public static EndOfGameInfo CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<EndOfGameInfo>(jsonString);
    } 

} 


