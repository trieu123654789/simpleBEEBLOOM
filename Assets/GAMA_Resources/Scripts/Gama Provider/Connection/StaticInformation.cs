using System.Net;
using System;

// EN: Static helper that generates and caches a unique player ID based on
//     the last octet of the machine’s local IP address (e.g. "Player_42").
//     Used as the persistent identity for all GAMA communication.
// VI: Lớp static tạo và cache ID người chơi duy nhất dựa trên
//     octet cuối của địa chỉ IP cục bộ (vd: "Player_42").
//     Dùng làm định danh xuyên suốt cho mọi giao tiếp với GAMA.
public static class StaticInformation
{
    // EN: End-of-game summary text received from GAMA, displayed on the result screen.
    // VI: Văn bản tổng kết cuối game nhận từ GAMA, hiển thị trên màn hình kết quả.
    public static string endOfGame { get; set; }

    // EN: Cached player ID; generated once on first call to getId().
    // VI: ID người chơi được cache; tạo một lần khi gọi getId() lần đầu.
    private static string connectionId;

    // EN: Return the player’s unique ID. On first call, resolves hostname → IP
    //     and extracts the last IP octet as the player number.
    // VI: Trả về ID duy nhất của người chơi. Lần gọi đầu, phân giải hostname → IP
    //     và trích octet cuối của IP làm số hiệu người chơi.
    public static string getId() {

        if (connectionId == null || connectionId.Length == 0)
        {
            string hostName = Dns.GetHostName(); // Retrive the Name of HOST
           try
            {
                string myIP = Dns.GetHostByName(hostName).AddressList[0].MapToIPv4().ToString();


                string lastIP = myIP.Contains(".") ? myIP.Split(".")[3] : "0";
                connectionId = "Player_" + lastIP;// + lastIP;
            } catch
            {
                connectionId = hostName;
            }
           
        }
        return connectionId;
    }
}
