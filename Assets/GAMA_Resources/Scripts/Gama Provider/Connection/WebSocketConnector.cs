using System;
using UnityEngine;
using WebSocketSharp;

// EN: Abstract base class for WebSocket communication with GAMA server.
//     Manages the raw socket lifecycle (connect / send / close) and delegates
//     message handling to concrete subclasses (e.g. ConnectionManager).
// VI: Lớp trừu tượng cơ sở cho giao tiếp WebSocket với server GAMA.
//     Quản lý vòng đời socket thô (connect / send / close) và ủy thác
//     xử lý message cho lớp con cụ thể (vd: ConnectionManager).
public abstract class WebSocketConnector : MonoBehaviour
{
    // EN: IP address of the GAMA server or middleware.
    // VI: Địa chỉ IP của server GAMA hoặc middleware.
     protected string host ;
    // EN: Port number for the WebSocket connection.
    // VI: Số hiệu cổng cho kết nối WebSocket.
     protected string port;

    // EN: Whether to route through the middleware proxy (true) or connect directly to GAMA (false).
    // VI: Có định tuyến qua middleware proxy (true) hay kết nối trực tiếp vào GAMA (false).
    protected bool UseMiddleware; 

    // EN: The underlying WebSocket instance (from WebSocketSharp library).
    // VI: Đối tượng WebSocket bên dưới (từ thư viện WebSocketSharp).
    private WebSocket socket;

    // EN: Send periodic heartbeat pings (middleware mode only).
    // VI: Gửi ping heartbeat định kỳ (chỉ dùng trong chế độ middleware).
    protected bool UseHeartbeat = true; //only for middleware mode
    // EN: If true, use hardcoded localhost settings for PC testing.
    // VI: Nếu true, dùng cài đặt localhost cố định cho test trên PC.
    protected bool DesktopMode = false;
    // EN: If true, ignore PlayerPrefs and use DefaultIP / DefaultPort below.
    // VI: Nếu true, bỏ qua PlayerPrefs và dùng DefaultIP / DefaultPort bên dưới.
    protected bool fixedProperties = true;
    // EN: Fallback IP when fixedProperties is true.
    // VI: IP mặc định khi fixedProperties là true.
    protected string DefaultIP = "localhost"; //"localhost";//"192.168.1.68"; 10.16.14.40 (test)"192.168.0.50"// "192.168.0.50"
    // EN: Fallback port when fixedProperties is true.
    // VI: Port mặc định khi fixedProperties là true.
    protected string DefaultPort = "8080";
    // EN: Whether to use middleware in Desktop Mode.
    // VI: Có dùng middleware trong chế độ Desktop không.
    protected bool UseMiddlewareDM = true;

    // EN: Max consecutive send errors before forcing disconnect.
    // VI: Số lỗi gửi liên tiếp tối đa trước khi buộc ngắt kết nối.
    protected int numErrorsBeforeDeconnection = 10;
    // EN: Current consecutive error counter.
    // VI: Bộ đếm lỗi liên tiếp hiện tại.
    protected int numErrors = 0;

    // EN: Resolve IP/port from PlayerPrefs or hardcoded defaults, then create
    //     the WebSocket and wire up abstract event handlers for subclasses.
    // VI: Xác định IP/port từ PlayerPrefs hoặc giá trị mặc định cố định, sau đó
    //     tạo WebSocket và gán các event handler trừu tượng cho lớp con.
    void OnEnable() {
       
        port = PlayerPrefs.GetString("PORT"); 
        host = PlayerPrefs.GetString("IP");

        if (DesktopMode)
        {
            UseMiddleware = UseMiddlewareDM;
            host = "localhost";

            if (UseMiddleware)
            {
                port = "8080";
            }
            else 
            {
                port = "1000";
            }
            
        } else if (fixedProperties)
        {
            UseMiddleware = UseMiddlewareDM;
            host = DefaultIP;
            port = DefaultPort;
            
        }
        Debug.Log("WebSocketConnector host: " + host + " PORT: " + port + " MIDDLEWARE:" + UseMiddleware);

        // EN: Create WebSocket to ws://IP:Port/ and register abstract event handlers.
        //     Subclasses define behavior for: open, message received, connection closed.
        // VI: Tạo WebSocket tới ws://IP:Port/ và đăng ký các event handler trừu tượng.
        //     Lớp con sẽ định nghĩa hành vi khi: kết nối thành công, nhận tin, đóng kết nối.
        socket = new WebSocket("ws://" + host + ":" + port + "/");
        socket.OnOpen += HandleConnectionOpen;
        //socket.OnMessage += HandleReceivedMessage;
        socket.OnMessage += (sender, e) =>
        {
            if (e.IsText)
                //Debug.Log("[WebSocket Received] " + e.Data);
            HandleReceivedMessage(sender, e);
        };
        socket.OnClose += HandleConnectionClosed;
        
    }

   // EN: Close WebSocket on GameObject destruction to prevent resource leaks.
   // VI: Đóng WebSocket khi GameObject bị hủy để tránh rò rỉ tài nguyên.
   void OnDestroy() {
       socket.Close();
    }

    // ############################## HANDLERS ##############################
    // EN: Abstract handlers — subclasses MUST override to define protocol behavior.
    // VI: Các handler trừu tượng — lớp con PHẢI override để định nghĩa hành vi giao thức.
    protected abstract void HandleConnectionOpen(object sender, System.EventArgs e);
    protected abstract void HandleReceivedMessage(object sender, MessageEventArgs e);
    protected abstract void HandleConnectionClosed(object sender, CloseEventArgs e);

    // #######################################################################
    // EN: Send a message asynchronously to the server. Validates socket liveness
    //     and message content before sending. The callback reports success/failure.
    // VI: Gửi message bất đồng bộ lên server. Kiểm tra socket còn sống và nội dung
    //     message hợp lệ trước khi gửi. Callback trả về kết quả thành công/thất bại.
    protected void SendMessageToServer(string message, Action<bool> successCallback)
    {

        if (!socket.IsAlive)
        {
            // Debug.LogError("WebSocket is not connected. Cannot send message: " + message);
            // successCallback(false);
            return;
        }
        if (message == null || message.Length == 0)
        {
            // Debug.LogError("Message is null or empty. Cannot send message.");
            // successCallback(false);
            return;
        }
        socket.SendAsync(message, successCallback);
    }

    // EN: Expose the raw WebSocket for subclass use (e.g. Connect, Close).
    // VI: Cung cấp WebSocket thô cho lớp con sử dụng (vd: Connect, Close).
    protected WebSocket GetSocket() {
        return socket;
    }

    // EN: Basic IP format validation (checks for 4 octets separated by dots).
    // VI: Kiểm tra định dạng IP cơ bản (4 octet phân cách bởi dấu chấm).
    private bool ValidIp(string ip)
    {
        if (ip == null || ip.Length == 0) return false;
        string[] ipb = ip.Split(".");
        return (ipb.Length != 4);
    }
}
