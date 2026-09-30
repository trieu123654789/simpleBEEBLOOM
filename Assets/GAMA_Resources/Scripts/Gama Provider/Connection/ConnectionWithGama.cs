using UnityEngine;
using WebSocketSharp;
using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

// EN: Lightweight standalone WebSocket client for direct GAMA communication
//     (bypassing the middleware). Provides SendExecutableAsk() with the same
//     JSON protocol as ConnectionManager but without lifecycle management.
//     NOTE: This class is largely superseded by ConnectionManager; kept for
//     simple test scenarios or standalone tools.
// VI: Client WebSocket nhẹ độc lập cho giao tiếp trực tiếp với GAMA
//     (bỏ qua middleware). Cung cấp SendExecutableAsk() với cùng giao thức
//     JSON như ConnectionManager nhưng không có quản lý vòng đời.
//     GHI CHÚ: Lớp này phần lớn đã bị thay thế bởi ConnectionManager; giữ lại
//     cho các kịch bản test đơn giản hoặc công cụ độc lập.
public class ConnectionWithGama : MonoBehaviour
{
    // EN: Server IP address.
    // VI: Địa chỉ IP server.
    protected string ip;
    // EN: Server port.
    // VI: Cổng server.
    protected string port;
    // EN: Target GAMA agent path for RPC calls.
    // VI: Đường dẫn agent GAMA đích cho lời gọi RPC.
    private String AgentToSendInfo = "simulation[0].unity_linker[0]";
    // EN: The raw WebSocket instance.
    // VI: Đối tượng WebSocket thô.
    protected WebSocket socket;
    // EN: Delimiter for multi-message payloads in direct mode.
    // VI: Ký tự phân cách cho payload nhiều message trong chế độ trực tiếp.
    protected String MessageSeparator = "|||";

  
    protected void SendMessageToServer(string message, Action<bool> successCallback)
    {
        socket.SendAsync(message, successCallback);
    }

    public void SendExecutableAsk(string action, Dictionary<string, string> arguments)
    {
        string argsJSON = JsonConvert.SerializeObject(arguments);
        Dictionary<string, string> jsonExpression = null;
        jsonExpression = new Dictionary<string, string> {
            {"type", "ask"},
            {"action", action},
            {"args", argsJSON},
            {"agent", AgentToSendInfo }
        };

        string jsonStringExpression = JsonConvert.SerializeObject(jsonExpression);

        SendMessageToServer(jsonStringExpression, new Action<bool>((success) => {
            if (!success)
            {
                Debug.LogError("ConnectionManager: Failed to send executable expression");
            }
        }));
    }
} 