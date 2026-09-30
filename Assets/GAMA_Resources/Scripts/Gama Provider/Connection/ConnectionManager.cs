using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;

// EN: High-level connection manager that extends WebSocketConnector.
//     Handles the full lifecycle: connect → authenticate → route inbound messages
//     → send outbound RPC calls ("ask") to the GAMA simulation agent.
// VI: Trình quản lý kết nối cấp cao kế thừa WebSocketConnector.
//     Xử lý toàn bộ vòng đời: kết nối → xác thực → định tuyến message đến
//     → gửi lời gọi RPC ("ask") đến agent mô phỏng GAMA.
public class ConnectionManager : WebSocketConnector
{
    // EN: Current connection lifecycle state.
    // VI: Trạng thái hiện tại trong vòng đời kết nối.
    private ConnectionState currentState;
    // EN: Flag to track whether a connection attempt is in progress.
    // VI: Cờ theo dõi xem có đang thử kết nối hay không.
    private bool connectionRequested; 

    // EN: Fired when connection state transitions (DISCONNECTED → PENDING → CONNECTED → AUTHENTICATED).
    // VI: Phát ra khi chuyển trạng thái kết nối (DISCONNECTED → PENDING → CONNECTED → AUTHENTICATED).
    public event Action<ConnectionState> OnConnectionStateChanged;

    // EN: Fired when a simulation output message is received; params = (firstJsonKey, fullJsonContent).
    // VI: Phát ra khi nhận được message output từ mô phỏng; tham số = (khóa JSON đầu tiên, nội dung JSON đầy đủ).
    public event Action<String, String> OnServerMessageReceived;

    // EN: Fired when a "json_state" heartbeat/status message is received from middleware.
    // VI: Phát ra khi nhận message trạng thái "json_state" từ middleware.
    public event Action<JObject> OnConnectionStateReceived;

    // EN: Fired after a connection attempt with success/failure result.
    // VI: Phát ra sau lần thử kết nối, kèm kết quả thành công/thất bại.
    public event Action<bool> OnConnectionAttempted;

    // EN: Singleton instance accessible from anywhere (SimulationManager, UI, etc.).
    // VI: Instance singleton truy cập từ bất kỳ đâu (SimulationManager, UI, v.v.).
    public static ConnectionManager Instance = null;

    // EN: Delimiter for splitting multiple messages when NOT using middleware (direct GAMA connection).
    // VI: Ký tự phân cách để tách nhiều message khi KHÔNG dùng middleware (kết nối trực tiếp GAMA).
    public String MessageSeparator = "|||";

    // EN: Target GAMA agent path for all "ask" RPC calls.
    // VI: Đường dẫn agent GAMA đích cho tất cả lời gọi RPC kiểu "ask".
    private String AgentToSendInfo = "simulation[0].unity_linker[0]";

    
    // ############################################# UNITY FUNCTIONS #############################################
    // EN: Awake — set singleton and resolve middleware preference.
    // VI: Awake — thiết lập singleton và xác định có dùng middleware không.
    void Awake() {
        UseMiddleware = DesktopMode ? UseMiddlewareDM : PlayerPrefs.GetString("MIDDLEWARE").Equals("Y");
        Debug.Log("ConnectionManager: Awake : " + PlayerPrefs.GetString("MIDDLEWARE"));
        Debug.Log("ConnectionManager Awake host: " + PlayerPrefs.GetString("IP") + " PORT: " + PlayerPrefs.GetString("PORT") + " UseMiddleware: "+ UseMiddleware);

        Instance = this;
    }

    // EN: Start — initialize to DISCONNECTED, which triggers auto-connect via TryConnectionToServer.
    // VI: Start — khởi tạo trạng thái DISCONNECTED, sẽ tự động gọi TryConnectionToServer.
    void Start() {
        
        Debug.Log("START");
        UpdateConnectionState(ConnectionState.DISCONNECTED);
        connectionRequested = false;

    }

    
    // ############################################# CONNECTION HANDLER #############################################
    // EN: Central state machine for connection lifecycle. Side effects per state:
    //     AUTHENTICATED → sends "new_connection" to register this player in GAMA.
    //     DISCONNECTED  → auto-triggers TryConnectionToServer().
    // VI: Máy trạng thái trung tâm cho vòng đời kết nối. Tác vụ phụ theo trạng thái:
    //     AUTHENTICATED → gửi "new_connection" để đăng ký player trong GAMA.
    //     DISCONNECTED  → tự động gọi TryConnectionToServer().
    public void UpdateConnectionState(ConnectionState newState) {
        
        switch (newState) {
            case ConnectionState.PENDING:
                Debug.Log("ConnectionManager: UpdateConnectionState -> PENDING");
                break;
            case ConnectionState.CONNECTED:
                Debug.Log("ConnectionManager: UpdateConnectionState -> CONNECTED");
                break;
            case ConnectionState.AUTHENTICATED:
                Debug.Log("ConnectionManager: UpdateConnectionState -> AUTHENTICATED");
                 Dictionary<string, string> args = new Dictionary<string, string> {
                    {"id", ConnectionManager.Instance.GetConnectionId()} };

                ConnectionManager.Instance.SendExecutableAsk("new_connection", args);
                break;
            case ConnectionState.DISCONNECTED:
                Debug.Log("ConnectionManager: UpdateConnectionState -> DISCONNECTED");
                TryConnectionToServer();
                break;
            default:
                break;
        }

        currentState = newState;
        OnConnectionStateChanged?.Invoke(newState);        
    }

    // ############################################# HANDLERS #############################################

    // EN: Called when WebSocket connection is established.
    //     In middleware mode: sends a "connection" handshake with player ID + heartbeat preference.
    //     In direct mode: no handshake needed — GAMA accepts immediately.
    // VI: Được gọi khi kết nối WebSocket thành công.
    //     Chế độ middleware: gửi handshake "connection" với player ID + cài đặt heartbeat.
    //     Chế độ trực tiếp: không cần handshake — GAMA chấp nhận ngay.
    protected override void HandleConnectionOpen(object sender, System.EventArgs e)
    {
        if (UseMiddleware)
        {
            var jsonId = new Dictionary<string, string> {
                {"type", "connection"},
                { "id", StaticInformation.getId() },
                { "set_heartbeat", UseHeartbeat ? "true": "false" }
            }; 
            string jsonStringId = JsonConvert.SerializeObject(jsonId);
            SendMessageToServer(jsonStringId, new Action<bool>((success) => {
                if (success) { }
            }));
            Debug.Log("ConnectionManager: Connection opened");
        }
       
    }

    // EN: Main inbound message dispatcher. Two protocols:
    //     MIDDLEWARE mode:
    //       "ping"        → reply "pong" (keepalive)
    //       "json_state"  → update auth state (CONNECTED / AUTHENTICATED)
    //       "json_output" → extract payload, find first JSON key, forward to OnServerMessageReceived
    //     DIRECT mode:
    //       "SimulationOutput" → split by MessageSeparator, forward each part to OnServerMessageReceived
    // VI: Bộ phân phối message đến chính. Hai giao thức:
    //     Chế độ MIDDLEWARE:
    //       "ping"        → trả lời "pong" (keepalive)
    //       "json_state"  → cập nhật trạng thái xác thực (CONNECTED / AUTHENTICATED)
    //       "json_output" → trích payload, tìm khóa JSON đầu, chuyển tiếp qua OnServerMessageReceived
    //     Chế độ TRỰC TIẾP:
    //       "SimulationOutput" → tách theo MessageSeparator, chuyển tiếp từng phần qua OnServerMessageReceived
    protected override void HandleReceivedMessage(object sender, MessageEventArgs e)
    {
        
        if (e.IsText)
        {
           
            //Debug.Log("e.Data: " + e.Data);
            JObject jsonObj = JObject.Parse(e.Data);
            string type = (string)jsonObj["type"];
           
        
            if (UseMiddleware)
            {
                switch (type)
                {
                    case "ping":
                        var jsonId = new Dictionary<string, string> {{"type", "pong"}};
                        string jsonStringId = JsonConvert.SerializeObject(jsonId);
                        SendMessageToServer(jsonStringId, new Action<bool>((success) => {
                            if (success) { }
                        }));
                        break;
                    case "json_state":
                        OnConnectionStateReceived?.Invoke(jsonObj);
                        bool authenticated = (bool)jsonObj["in_game"];
                        bool connected = (bool)jsonObj["connected"];

                        if (authenticated && connected)
                        {
                            if (!IsConnectionState(ConnectionState.AUTHENTICATED))
                            {
                                Debug.Log("ConnectionManager: Player successfully authenticated");
                                UpdateConnectionState(ConnectionState.AUTHENTICATED);
                            }

                        }
                        else if (connected && !authenticated)
                        {
                            if (!IsConnectionState(ConnectionState.CONNECTED))
                            {
                                connectionRequested = false;
                                Debug.Log("ConnectionManager: Successfully connected, waiting for authentication...");
                                UpdateConnectionState(ConnectionState.CONNECTED);
                                OnConnectionAttempted?.Invoke(true);
                            }
                            else
                            {
                                Debug.LogWarning("ConnectionManager: Already connected, waiting for authentication...");
                            }

                        } 
                        break;  

                    case "json_output":
                        JObject content = (JObject)jsonObj["contents"];
                        String firstKey = content.Properties().Select(pp => pp.Name).FirstOrDefault();
                        OnServerMessageReceived?.Invoke(firstKey, content.ToString());
                        break;

                    default:
                        break;
                }
            } 
            else if (type.Equals("SimulationOutput"))
            {
                JValue content = (JValue)jsonObj["content"];
               // Debug.Log("MessageSeparator: " + MessageSeparator);
                foreach (String mes in content.ToString().Split(MessageSeparator))
                {
                    if (!mes.IsNullOrEmpty())
                        OnServerMessageReceived?.Invoke(null, mes);
                }
            }
        }
    }

    // EN: Called when the WebSocket connection closes. If a connect attempt was in progress,
    //     it is treated as a failed attempt. Then transitions back to DISCONNECTED.
    // VI: Được gọi khi kết nối WebSocket bị đóng. Nếu đang thử kết nối,
    //     coi như thất bại. Sau đó chuyển về trạng thái DISCONNECTED.
    protected override void HandleConnectionClosed(object sender, CloseEventArgs e) {
        // checks if the connection was closed just after a connection request
        Debug.Log("ConnectionManager: HandleConnectionClosed");
        if (connectionRequested) {
            connectionRequested = false;
            OnConnectionAttempted?.Invoke(false);
            Debug.Log("ConnectionManager: Failed to connect to server");
        }
        UpdateConnectionState(ConnectionState.DISCONNECTED);
    }

    // ############################################# UTILITY FUNCTIONS #############################################
    // EN: Initiate connection. In middleware mode: just opens socket.
    //     In direct mode: opens socket, immediately sends "create_init_player"
    //     and transitions to AUTHENTICATED (no middleware auth handshake).
    // VI: Khởi tạo kết nối. Chế độ middleware: chỉ mở socket.
    //     Chế độ trực tiếp: mở socket, gửi ngay "create_init_player"
    //     và chuyển thẳng sang AUTHENTICATED (không cần handshake middleware).
    public void TryConnectionToServer() {
        if(IsConnectionState(ConnectionState.DISCONNECTED)) {
            Debug.Log("ConnectionManager: Attempting to connect to " + (UseMiddleware?"middleware":"GAMA")+ ": ws://" + host + ":" + port + "/");
            connectionRequested = true;
            UpdateConnectionState(ConnectionState.PENDING);

            GetSocket().Connect();
             
            if (! UseMiddleware)  
            {
                Debug.Log("Create player direct :" + ConnectionManager.Instance.GetConnectionId());

                  Dictionary<string, string> args = new Dictionary<string, string> {
                    {"id", "\""+ConnectionManager.Instance.GetConnectionId()+"\""}
                  };
                  SendExecutableAsk("create_init_player", args);

                 
                UpdateConnectionState(ConnectionState.AUTHENTICATED); 

            }
        } else {
            Debug.LogWarning("ConnectionManager: Already connected to middleware: " + this.currentState);
        }
        
    }
     
    public void DisconnectFromServer() {
        if(!IsConnectionState(ConnectionState.DISCONNECTED)) {
            Debug.Log("ConnectionManager: Disconnecting from middleware...");
            GetSocket().Close();
            UpdateConnectionState(ConnectionState.DISCONNECTED);
        } else {
            Debug.LogWarning("ConnectionManager: Already disconnected from middleware");
        }
    }

    public bool IsConnectionState(ConnectionState currentState) {
        return this.currentState == currentState;
    }

    // EN: Send a raw GAML expression to be evaluated on the server.
    //     On consecutive failures beyond threshold, force-disconnect.
    // VI: Gửi biểu thức GAML thô để server đánh giá.
    //     Nếu thất bại liên tiếp vượt ngưỡng, buộc ngắt kết nối.
    public void SendExecutableExpression(string expression) {
        Dictionary<string, string> jsonExpression = null;
        jsonExpression = new Dictionary<string, string> {
            {"type", "expression"},
            {"expr", expression}
        };

        string jsonStringExpression = JsonConvert.SerializeObject(jsonExpression);
        SendMessageToServer(jsonStringExpression, new Action<bool>((success) => {
            if (!success) {
                numErrors++;
                Debug.LogError("ConnectionManager: Failed to send executable expression");
                if (numErrors > numErrorsBeforeDeconnection)
                {
                    GetSocket().Close();
                   currentState = (ConnectionState.DISCONNECTED);
                    numErrors = 0;
                }
            } else
            {
                numErrors = 0;
            }
        }));
    }

    // EN: Send an "ask" RPC to a specific GAMA agent (AgentToSendInfo).
    //     This is the primary method for Unity→GAMA communication.
    //     Serializes arguments to JSON, wraps in {type:"ask", action, args, agent}.
    //     On consecutive failures beyond threshold, force-disconnect.
    // VI: Gửi RPC kiểu "ask" đến agent GAMA cụ thể (AgentToSendInfo).
    //     Đây là phương thức chính để giao tiếp Unity→GAMA.
    //     Serialize tham số thành JSON, bọc trong {type:"ask", action, args, agent}.
    //     Nếu thất bại liên tiếp vượt ngưỡng, buộc ngắt kết nối.
    public void SendExecutableAsk(string action, Dictionary<string,string> arguments)
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
                numErrors++;
                Debug.LogError("ConnectionManager: Failed to send executable ask");
                if (numErrors > numErrorsBeforeDeconnection)
                {
                    GetSocket().Close();
                    currentState = (ConnectionState.DISCONNECTED);
                    numErrors = 0;
                }
            } else
            {
                numErrors = 0;
            }
    }));
    }

    // EN: Send a graceful "disconnect_properly" message before closing the socket.
    // VI: Gửi message "disconnect_properly" trước khi đóng socket.
    public void DisconnectProperly() {
        Dictionary<string,string> jsonExpression = new Dictionary<string,string> {
            {"type", "disconnect_properly"}
        };
        string jsonStringExpression = JsonConvert.SerializeObject(jsonExpression);
        SendMessageToServer(jsonStringExpression, new Action<bool>((success) => {
            if (!success) {
                Debug.LogError("ConnectionManager: Failed to send disconnect message");
            }
            else {
                DisconnectFromServer();
            }
        }));
    }

    // EN: Return the unique player ID (derived from local IP).
    // VI: Trả về ID người chơi duy nhất (lấy từ IP cục bộ).
    public string GetConnectionId() {
        return StaticInformation.getId();
    }

    // EN: Whether routing through middleware proxy.
    // VI: Có đang định tuyến qua middleware proxy không.
    public bool getUseMiddleware()
    {
        return UseMiddleware;
    }

    // EN: Force-reset to DISCONNECTED and re-attempt connection.
    // VI: Buộc reset về DISCONNECTED và thử kết nối lại.
    public void Reconnect()
    {
        Debug.Log("Reconnect");
        currentState = ConnectionState.DISCONNECTED;
        TryConnectionToServer();
    }


}


// EN: Connection lifecycle states between Unity and GAMA server/middleware.
// VI: Các trạng thái trong vòng đời kết nối giữa Unity và server/middleware GAMA.
public enum ConnectionState {
    // EN: No active connection.
    // VI: Chưa có kết nối.
    DISCONNECTED,
    // EN: Socket.Connect() called, waiting for handshake.
    // VI: Đã gọi Socket.Connect(), đang chờ bắt tay.
    PENDING, 
    // EN: WebSocket open, waiting for middleware authentication.
    // VI: WebSocket đã mở, đang chờ xác thực từ middleware.
    CONNECTED,
    // EN: Fully authenticated — ready to exchange simulation data.
    // VI: Đã xác thực hoàn toàn — sẵn sàng trao đổi dữ liệu mô phỏng.
    AUTHENTICATED
}
