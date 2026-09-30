# BÁO CÁO CHI TIẾT

## Tích hợp nền tảng mô phỏng GAMA với Unity cho mô hình Agent-Based trong quản lý tài nguyên nông nghiệp

### *Integrating the GAMA Simulation Platform with Unity for Agent-Based Modeling in Agricultural Resource Management*

---

**Đơn vị thực hiện:**
- Viện Nghiên cứu Phát triển (IRD — Institut de Recherche pour le Développement), Pháp
- Trường Đại học Cần Thơ (CTU — Can Tho University), Việt Nam

**Nền tảng công nghệ:** Unity 2022.3.5f1 | GAMA Agent-Based Modeling Platform | WebSocket (websocket-sharp.dll) | Meta Quest VR | XR Interaction Toolkit 2.5.2 | Newtonsoft.Json

**Dự án:** SIMPLE VU2 — *Simulation for Interactive Modeling and Participatory Learning Environment — Version 2*

---

## MỤC LỤC

1. [Giới thiệu](#1-giới-thiệu)
2. [Tổng quan kiến trúc tích hợp](#2-tổng-quan-kiến-trúc-tích-hợp)
3. [Tầng kết nối — Connection Layer](#3-tầng-kết-nối--connection-layer)
4. [Tầng mô phỏng — Simulation Layer](#4-tầng-mô-phỏng--simulation-layer)
5. [Tầng tuần tự hóa dữ liệu — Serialization Layer](#5-tầng-tuần-tự-hóa-dữ-liệu--serialization-layer)
6. [Hệ thống tác nhân trong quản lý tài nguyên nông nghiệp](#6-hệ-thống-tác-nhân-trong-quản-lý-tài-nguyên-nông-nghiệp)
7. [Giao thức truyền thông Unity–GAMA](#7-giao-thức-truyền-thông-unitygama)
8. [Hệ thống chuyển đổi tọa độ](#8-hệ-thống-chuyển-đổi-tọa-độ)
9. [Luồng dữ liệu mô phỏng](#9-luồng-dữ-liệu-mô-phỏng)
10. [Cầu nối GAMA Bridge — Tích hợp gameplay nông nghiệp](#10-cầu-nối-gama-bridge--tích-hợp-gameplay-nông-nghiệp)
11. [Hệ thống tác nhân phòng thủ nông nghiệp](#11-hệ-thống-tác-nhân-phòng-thủ-nông-nghiệp)
12. [Hệ thống hình học và địa hình](#12-hệ-thống-hình-học-và-địa-hình)
13. [Phân tích hiệu năng và tối ưu hóa](#13-phân-tích-hiệu-năng-và-tối-ưu-hóa)
14. [Đánh giá và thảo luận](#14-đánh-giá-và-thảo-luận)
15. [Kết luận](#15-kết-luận)
16. [Tài liệu tham khảo](#16-tài-liệu-tham-khảo)

---

## 1. GIỚI THIỆU

### 1.1. Bối cảnh

Mô hình hóa dựa trên tác nhân (Agent-Based Modeling — ABM) là phương pháp tiếp cận mạnh mẽ để mô phỏng các hệ thống phức tạp trong quản lý tài nguyên nông nghiệp. Nền tảng GAMA (GIS & Agent-based Modeling Architecture) — được phát triển bởi IRD và các đối tác quốc tế — cung cấp khả năng mô hình hóa không gian với tích hợp dữ liệu GIS, trong khi Unity là công cụ hàng đầu cho trực quan hóa 3D và tương tác VR (Taillandier et al., 2019).

Tuy nhiên, việc tích hợp hai nền tảng này đặt ra nhiều thách thức kỹ thuật: sự khác biệt về hệ tọa độ, giao thức truyền thông bất đồng bộ, đồng bộ hóa trạng thái giữa hai runtime engine, và yêu cầu xử lý thời gian thực cho ứng dụng VR.

### 1.2. Mục tiêu báo cáo

Báo cáo này trình bày chi tiết:

1. **Kiến trúc tích hợp đa tầng** giữa GAMA và Unity trong dự án SIMPLE VU2
2. **Giao thức truyền thông JSON-RPC** qua WebSocket với hai chế độ (middleware/trực tiếp)
3. **Hệ thống tác nhân** mô phỏng xâm nhập mặn và cơ chế phòng thủ nông nghiệp
4. **Luồng dữ liệu hai chiều** — từ mô hình ABM trên GAMA đến trực quan hóa trên Unity và ngược lại
5. **Phân tích hiệu năng** và chiến lược tối ưu cho ứng dụng thời gian thực

### 1.3. Phạm vi

Hệ thống SIMPLE VU2 áp dụng mô hình ABM để mô phỏng quản lý tài nguyên nông nghiệp vùng Đồng bằng sông Cửu Long (ĐBSCL), bao gồm:
- Mô phỏng xâm nhập mặn theo thời gian thực (enemy agents)
- Hệ thống phòng thủ thủy lợi (water pump agents)
- Hàng rào cây xanh sinh học (tree barrier agents)
- Quản lý sản xuất nông nghiệp theo mùa vụ và độ mặn

---

## 2. TỔNG QUAN KIẾN TRÚC TÍCH HỢP

### 2.1. Kiến trúc tổng thể

Hệ thống được thiết kế theo kiến trúc **5 tầng**, phân tách rõ ràng trách nhiệm giữa các thành phần:

```
┌═══════════════════════════════════════════════════════════════════════════┐
║                     KIẾN TRÚC TÍCH HỢP GAMA–UNITY                      ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                         ║
║  ┌────────────────────────────────────────────────────────────────────┐  ║
║  │ TẦNG 5: TRÌNH BÀY (Presentation Layer)                           │  ║
║  │   Meta Quest VR │ XR Interaction Toolkit │ URP Rendering          │  ║
║  │   Tương tác nông trại │ Thu hoạch VR │ HUD thời tiết/mùa         │  ║
║  └────────────────────────────────┬───────────────────────────────────┘  ║
║                                   │                                     ║
║  ┌────────────────────────────────┴───────────────────────────────────┐  ║
║  │ TẦNG 4: CẦU NỐI GAMA (GAMA Bridge Layer)                         │  ║
║  │   GAMABridgeLevel1.cs │ Đồng bộ KPI │ Quản lý vòng đời game      │  ║
║  │   Gửi: vị trí, điểm, thời gian │ Nhận: spawn rate, subsidence    │  ║
║  └────────────────────────────────┬───────────────────────────────────┘  ║
║                                   │                                     ║
║  ┌────────────────────────────────┴───────────────────────────────────┐  ║
║  │ TẦNG 3: MÔ PHỎNG (Simulation Layer)                               │  ║
║  │   SimulationManager.cs (1728 dòng — Orchestrator trung tâm)       │  ║
║  │   ┌──────────────┬──────────────┬───────────────────────────────┐  │  ║
║  │   │ Solo Mode    │ Multi Mode   │ Interaction Mode              │  │  ║
║  │   │ (Đèn, Hover) │ (InitRefs)  │ (Virtual stubs)              │  │  ║
║  │   └──────────────┴──────────────┴───────────────────────────────┘  │  ║
║  │   CoordinateConverter │ PolygonGenerator │ GAMAGeometryLoader     │  ║
║  └────────────────────────────────┬───────────────────────────────────┘  ║
║                                   │                                     ║
║  ┌────────────────────────────────┴───────────────────────────────────┐  ║
║  │ TẦNG 2: TUẦN TỰ HÓA (Serialization Layer)                        │  ║
║  │   15 DTO classes: ConnectionParameter │ PropertiesGAMA │          │  ║
║  │   WorldJSONInfo │ DEMData │ DEMDataLoc │ EnemySpawnerInfo │       │  ║
║  │   FreshWaterSpawn │ SubsidenceInfo │ AnimationInfo │              │  ║
║  │   TeleportAreaInfo │ WallInfo │ EndOfGameInfo │ ...               │  ║
║  └────────────────────────────────┬───────────────────────────────────┘  ║
║                                   │                                     ║
║  ┌────────────────────────────────┴───────────────────────────────────┐  ║
║  │ TẦNG 1: KẾT NỐI (Connection Layer)                                │  ║
║  │   WebSocketConnector.cs (Abstract base)                            │  ║
║  │   ConnectionManager.cs (Singleton — State Machine)                 │  ║
║  │   ConnectionWithGama.cs (Lightweight standalone client)            │  ║
║  │   WebSocket: websocket-sharp.dll │ JSON: Newtonsoft.Json           │  ║
║  └────────────────────────────────┬───────────────────────────────────┘  ║
║                                   │ WebSocket (ws://)                   ║
║  ┌────────────────────────────────┴───────────────────────────────────┐  ║
║  │                     GAMA SERVER                                    │  ║
║  │   Agent-Based Model │ GIS Data │ Mô phỏng xâm nhập mặn          │  ║
║  │   Mô hình đa tác nhân │ Thủy triều │ Sụt lún │ Địch (nước mặn)  │  ║
║  └────────────────────────────────────────────────────────────────────┘  ║
║                                                                         ║
╚═══════════════════════════════════════════════════════════════════════════╝
```

### 2.2. Các thành phần chính

| Thành phần | File | Dòng code | Vai trò |
|-----------|------|:---------:|---------|
| **WebSocketConnector** | `WebSocketConnector.cs` | ~150 | Lớp cơ sở trừu tượng quản lý vòng đời WebSocket |
| **ConnectionManager** | `ConnectionManager.cs` | ~426 | Singleton — máy trạng thái kết nối, định tuyến message |
| **SimulationManager** | `SimulationManager.cs` | ~1728 | Orchestrator trung tâm — xử lý dữ liệu 2 chiều |
| **CoordinateConverter** | `CoordinateConverter.cs` | ~104 | Chuyển đổi tọa độ GAMA CRS ↔ Unity world |
| **PolygonGenerator** | `PolygonGenerator.cs` | ~200 | Tạo mesh 3D từ tọa độ đa giác GAMA |
| **GAMAGeometryLoader** | `GAMAGeometryLoader.cs` | ~211 | Import hình học thế giới từ GAMA |
| **GAMAGeometryExport** | `GAMAGeometryExport.cs` | ~150 | Export mesh Unity sang GAMA |
| **GAMABridgeLevel1** | `GAMABridgeLevel1.cs` | ~387 | Cầu nối Level 1 — đồng bộ KPI gameplay |
| **15 DTO classes** | `Serialization/*.cs` | ~300 | Tuần tự hóa/giải tuần tự hóa dữ liệu JSON |

### 2.3. Sơ đồ phụ thuộc thành phần

```
                    ┌─────────────────────┐
                    │  GAMABridgeLevel1   │
                    │  (Game-specific)     │
                    └──────────┬──────────┘
                               │ uses
                    ┌──────────┴──────────┐
                    │ SimulationManager   │
                    │ (Central Orchest.)  │
                    └──┬───┬───┬───┬──┬──┘
                       │   │   │   │  │
          ┌────────────┘   │   │   │  └────────────┐
          │                │   │   │               │
   ┌──────┴──────┐  ┌─────┴───┴───┴──┐    ┌──────┴──────┐
   │ Connection  │  │  15 DTO Classes │    │ Coordinate  │
   │ Manager     │  │  (Serialization)│    │ Converter   │
   │ (Singleton) │  └────────────────┘    └──────┬──────┘
   └──────┬──────┘                               │
          │                               ┌──────┴──────┐
   ┌──────┴──────┐                        │  Polygon    │
   │ WebSocket   │                        │  Generator  │
   │ Connector   │                        └─────────────┘
   │ (Abstract)  │
   └──────┬──────┘
          │ websocket-sharp.dll
   ┌──────┴──────┐
   │ GAMA Server │
   └─────────────┘
```

---

## 3. TẦNG KẾT NỐI — CONNECTION LAYER

### 3.1. WebSocketConnector — Lớp cơ sở trừu tượng

`WebSocketConnector` cung cấp nền tảng quản lý vòng đời WebSocket cho toàn bộ hệ thống kết nối:

```csharp
// Lớp cơ sở trừu tượng — quản lý socket, gửi/nhận message
public abstract class WebSocketConnector : MonoBehaviour
{
    // Cấu hình kết nối
    protected string host;              // IP server (từ PlayerPrefs hoặc hardcoded)
    protected string port;              // Port (middleware: 8080, direct: 1000)
    protected bool UseMiddleware;       // Định tuyến qua proxy hay kết nối trực tiếp
    protected bool UseHeartbeat;        // Bật keepalive ping/pong
    protected bool DesktopMode;         // Chế độ desktop (không dùng VR)
    protected int numErrorsBeforeDeconnection; // Ngưỡng lỗi trước khi ngắt kết nối
    
    // Socket instance
    private WebSocket socket;
    
    // Gửi message bất đồng bộ
    protected void SendMessageToServer(string message, Action<bool> successCallback)
    {
        if (!socket.IsAlive || message == null || message.Length == 0) return;
        socket.SendAsync(message, successCallback);
    }
    
    // Các phương thức ảo để lớp con override
    protected abstract void HandleConnectionOpen(object sender, EventArgs e);
    protected abstract void HandleReceivedMessage(object sender, MessageEventArgs e);
    protected abstract void HandleConnectionClosed(object sender, CloseEventArgs e);
}
```

**Các trường cấu hình:**

| Trường | Kiểu | Mặc định | Mô tả |
|--------|------|----------|-------|
| `host` | `string` | `"192.168.88.148"` | Địa chỉ IP server GAMA/middleware |
| `port` | `string` | `"8080"` / `"1000"` | Port kết nối (middleware/trực tiếp) |
| `UseMiddleware` | `bool` | Từ PlayerPrefs | Có sử dụng middleware proxy không |
| `UseHeartbeat` | `bool` | `true` | Bật cơ chế keepalive |
| `numErrorsBeforeDeconnection` | `int` | `10` | Số lỗi liên tiếp trước khi tự ngắt |

### 3.2. ConnectionManager — Máy trạng thái Singleton

`ConnectionManager` kế thừa `WebSocketConnector`, triển khai máy trạng thái 4 trạng thái quản lý toàn bộ vòng đời kết nối:

#### 3.2.1. Sơ đồ máy trạng thái

```
  ┌──────────────┐           TryConnectionToServer()
  │ DISCONNECTED │ ──────────────────────────────────► ┌──────────┐
  │              │                                      │ PENDING  │
  │  (Tự động    │ ◄────── HandleConnectionClosed() ── │          │
  │   thử lại)   │                                      │ (Socket  │
  └──────────────┘                                      │ Connect) │
        ▲                                               └────┬─────┘
        │                                                    │
        │                                    Socket open     │
        │                                                    ▼
        │                                              ┌───────────┐
        │         HandleConnectionClosed()             │ CONNECTED │
        └───────────────────────────────────────────── │           │
                                                       │(Middleware│
                                                       │ handshake│
                                                       │ đã xong) │
                                                       └─────┬────┘
                                                             │
                                          json_state:        │
                                          in_game=true       │
                                                             ▼
                                                     ┌──────────────┐
                                                     │AUTHENTICATED │
                                                     │              │
                                                     │ (Sẵn sàng    │
                                                     │  trao đổi)   │
                                                     └──────────────┘
```

#### 3.2.2. Hai chế độ kết nối

**Chế độ Middleware (UseMiddleware = true):**

```
Unity                    Middleware Proxy                 GAMA Server
  │                            │                              │
  │── ws://host:8080/ ────────►│                              │
  │                            │                              │
  │── {type:"connection",     ─►│                              │
  │    id: playerID,           │                              │
  │    set_heartbeat:"true"}   │                              │
  │                            │                              │
  │◄── {type:"json_state",  ──│                              │
  │     connected: true,       │                              │
  │     in_game: false}        │                              │
  │                            │── Forward to GAMA ──────────►│
  │                            │                              │
  │◄── {type:"json_state",  ──│◄── Auth confirmed ──────────│
  │     connected: true,       │                              │
  │     in_game: true}         │                              │
  │                            │                              │
  │   [AUTHENTICATED]          │                              │
  │                            │                              │
  │── {type:"ask", action,   ──►── Forward ──────────────────►│
  │    args, agent}            │                              │
  │                            │                              │
  │◄── {type:"json_output", ──│◄── SimulationOutput ────────│
  │     contents: {...}}       │                              │
  │                            │                              │
  │◄── {type:"ping"} ────────│  (Keepalive mỗi ~30s)        │
  │── {type:"pong"} ─────────►│                              │
```

**Chế độ Trực tiếp (UseMiddleware = false):**

```
Unity                                            GAMA Server
  │                                                   │
  │── ws://host:1000/ ──────────────────────────────►│
  │                                                   │
  │── {type:"ask",                                    │
  │    action:"create_init_player",              ────►│
  │    args:{id:"playerID"},                          │
  │    agent:"simulation[0].unity_linker[0]"}         │
  │                                                   │
  │   [Chuyển thẳng sang AUTHENTICATED]               │
  │                                                   │
  │◄── {type:"SimulationOutput",                 ────│
  │     content: "msg1|||msg2|||msg3"}                │
  │                                                   │
  │   (Split bằng "|||" separator)                    │
```

#### 3.2.3. Hệ thống sự kiện (Event System)

`ConnectionManager` sử dụng mô hình Observer với 4 sự kiện:

```csharp
// Sự kiện khi trạng thái kết nối thay đổi
public event Action<ConnectionState> OnConnectionStateChanged;

// Sự kiện khi nhận message mô phỏng (key JSON đầu tiên, nội dung đầy đủ)
public event Action<String, String> OnServerMessageReceived;

// Sự kiện khi nhận trạng thái từ middleware (json_state)
public event Action<JObject> OnConnectionStateReceived;

// Sự kiện sau khi thử kết nối (thành công/thất bại)
public event Action<bool> OnConnectionAttempted;
```

**Luồng đăng ký sự kiện:**

```
SimulationManager.OnEnable()
    ├── ConnectionManager.OnServerMessageReceived += HandleServerMessageReceived
    ├── ConnectionManager.OnConnectionStateChanged += HandleConnectionStateChanged
    └── ConnectionManager.OnConnectionAttempted += HandleConnectionAttempted

SimulationManager.OnDisable()
    ├── ConnectionManager.OnServerMessageReceived -= HandleServerMessageReceived
    ├── ConnectionManager.OnConnectionStateChanged -= HandleConnectionStateChanged
    └── ConnectionManager.OnConnectionAttempted -= HandleConnectionAttempted
```

### 3.3. Cơ chế gửi tin — SendExecutableAsk

Đây là phương thức trung tâm cho giao tiếp Unity → GAMA, sử dụng giao thức JSON-RPC:

```csharp
public void SendExecutableAsk(string action, Dictionary<string, string> arguments)
{
    // 1. Serialize arguments thành JSON
    string argsJSON = JsonConvert.SerializeObject(arguments);
    
    // 2. Tạo envelope message với cấu trúc chuẩn
    Dictionary<string, string> jsonExpression = new Dictionary<string, string> {
        {"type", "ask"},                              // Loại message: RPC call
        {"action", action},                           // Tên action trên GAMA
        {"args", argsJSON},                           // Tham số đã serialize
        {"agent", "simulation[0].unity_linker[0]"}    // Agent đích trên GAMA
    };
    
    // 3. Gửi bất đồng bộ qua WebSocket
    string jsonStringExpression = JsonConvert.SerializeObject(jsonExpression);
    SendMessageToServer(jsonStringExpression, successCallback);
    
    // 4. Xử lý lỗi: ngắt kết nối nếu vượt ngưỡng lỗi liên tiếp
}
```

**Cấu trúc JSON envelope:**

```json
{
    "type": "ask",
    "action": "update_player_pos",
    "args": "{\"id\":\"192.168.1.100\",\"score\":\"85\",\"remaining_time\":\"120\"}",
    "agent": "simulation[0].unity_linker[0]"
}
```

### 3.4. Cơ chế phục hồi lỗi

Hệ thống triển khai **circuit breaker pattern** đơn giản:

```
Gửi message → Thành công → Reset numErrors = 0
                          ↘
                           Thất bại → numErrors++
                                       │
                              numErrors > threshold (10)?
                              ├── Không → Tiếp tục gửi
                              └── Có → Force disconnect
                                        │
                                        ▼
                                  DISCONNECTED
                                  (Tự động thử lại)
```

---

## 4. TẦNG MÔ PHỎNG — SIMULATION LAYER

### 4.1. SimulationManager — Orchestrator trung tâm

`SimulationManager` (1728 dòng) là thành phần quan trọng nhất của hệ thống, đóng vai trò **orchestrator trung tâm** điều phối toàn bộ luồng dữ liệu hai chiều giữa Unity và GAMA.

#### 4.1.1. Máy trạng thái Game (GameState)

```csharp
public enum GameState
{
    MENU,           // Màn hình chính, chờ kết nối
    WAITING,        // Đã kết nối, chờ xác thực
    LOADING_DATA,   // Đang tải dữ liệu thế giới từ GAMA
    GAME,           // Gameplay chính — đồng bộ liên tục
    END,            // Kết thúc game
    CRASH           // Mất kết nối đột ngột
}
```

**Sơ đồ chuyển trạng thái:**

```
┌──────┐                            ┌─────────┐
│ MENU │── ConnectionAttempted ────►│ WAITING │
└──────┘   (success=true)           └────┬────┘
                                         │
                            AUTHENTICATED │
                                         ▼
                                  ┌──────────────┐
                                  │ LOADING_DATA │
                                  │              │
                                  │ Poll:        │
                                  │ send_init_   │
                                  │ data mỗi 2s  │
                                  └──────┬───────┘
                                         │
                          Nhận precision  │
                          + properties    │
                          + pointsLoc     │
                                         ▼
                                    ┌────────┐
                                    │  GAME  │◄──┐
                                    │        │   │
                                    │ Đồng bộ│   │ Reconnect
                                    │ 0.5s   │   │
                                    └───┬────┘───┘
                                        │
                          ┌─────────────┤
                          │             │
                          ▼             ▼
                    ┌─────────┐   ┌─────────┐
                    │   END   │   │  CRASH  │
                    └─────────┘   └─────────┘
```

#### 4.1.2. Hệ thống bộ đếm thời gian gửi dữ liệu

`SimulationManager` sử dụng **3 bộ đếm thời gian lệch pha** (staggered timers) để phân phối tải mạng đều đặn:

```csharp
// Trong FixedUpdate() — chạy mỗi physics tick
if (TimerSendPositionEnemy > 0) TimerSendPositionEnemy -= Time.deltaTime;
if (TimerSendPositionFW > 0)    TimerSendPositionFW -= Time.deltaTime;
if (TimerSendPosition > 0)      TimerSendPosition -= Time.deltaTime;

// Gửi lệch pha — tránh gửi đồng thời
if (TimerSendPositionEnemy <= 0)
{
    sendEnemies();                              // Gửi vị trí enemy
    TimerSendPositionEnemy = TimeSendPosition;  // Reset = 0.5s
}

if (TimerSendPositionFW <= 0)
{
    sendFreshWater();                           // Gửi vị trí nước ngọt
    TimerSendPositionFW = TimeSendPosition;     // Reset = 0.5s
}

if (TimerSendPosition <= 0)
{
    updatePlayerPos();                          // Gửi vị trí + KPI người chơi
    TimerSendPosition = TimeSendPosition;       // Reset = 0.5s
}
```

**Biểu đồ thời gian gửi dữ liệu:**

```
t=0.0    t=0.1    t=0.2    t=0.3    t=0.4    t=0.5    t=0.6
  │        │        │        │        │        │        │
  ├─Enemy──┤        │        │        │        ├─Enemy──┤
  │        ├──FW────┤        │        │        │        ├──FW──
  │        │        ├─Player─┤        │        │        │
  │        │        │        │        │        │        │
  ▼        ▼        ▼        ▼        ▼        ▼        ▼
  ─────────────────────────────────────────────────────────►
                    Thời gian (giây)
```

#### 4.1.3. Hệ thống cờ xử lý trì hoãn (Deferred Processing Flags)

Do message WebSocket đến trên thread riêng, `SimulationManager` dùng **12 cờ boolean** để chuyển xử lý sang main thread trong `FixedUpdate()`:

```csharp
// Khai báo cờ (thread-safe flags)
private bool sendPlayerReady = false;
private bool receivedPrecision = false;
private bool receivedProperties = false;
private bool receivedPointsLoc = false;
private bool receivedDEM = false;
private bool receivedDEMLoc = false;
private bool receivedEnemySpawner = false;
private bool receivedFreshWater = false;
private bool receivedSubsidence = false;
private bool receivedAnimation = false;
private bool receivedTeleportation = false;
private bool receivedWalls = false;

// Xử lý trong FixedUpdate() — LUÔN trên main thread
void FixedUpdate()
{
    if (receivedPrecision)
    {
        receivedPrecision = false;
        ProcessPrecisionData();       // Khởi tạo CoordinateConverter
    }
    if (receivedProperties)
    {
        receivedProperties = false;
        ProcessPropertiesData();      // Tải prefab, vật liệu
    }
    if (receivedPointsLoc)
    {
        receivedPointsLoc = false;
        GenerateGeometries();         // Khởi tạo hình học thế giới
    }
    if (receivedEnemySpawner)
    {
        receivedEnemySpawner = false;
        UpdateEnemySpawnRates();      // Cập nhật tốc độ spawn
    }
    // ... tương tự cho các cờ khác
}
```

### 4.2. Các biến thể SimulationManager

Hệ thống sử dụng **mô hình kế thừa (inheritance)** để mở rộng chức năng:

```
SimulationManagerInteraction (Abstract — virtual stubs)
        │
        ├── SimulationManagerSolo (Chế độ 1 người)
        │     ├── Toggle đèn ngày/đêm (Main Button)
        │     ├── Hotspot highlighting (đánh dấu đỏ)
        │     └── Hover feedback (xanh dương khi rê chuột)
        │
        └── SimulationManagerMulti (Chế độ nhiều người)
              └── InitReferences(player, ground) — bootstrap runtime
```

**SimulationManagerInteraction** định nghĩa 4 hook ảo:

```csharp
public abstract class SimulationManagerInteraction : SimulationManager
{
    // Khi tia XR chạm vào vật thể
    protected virtual void HoverEnterInteraction(string objectName) { }
    
    // Khi tia XR rời khỏi vật thể
    protected virtual void HoverExitInteraction(string objectName) { }
    
    // Nút chính trên controller (toggle đèn, v.v.)
    protected virtual void TriggerMainButton() { }
    
    // Xử lý message tùy chỉnh từ GAMA
    protected virtual void ManageOtherMessages(string firstKey, string content) { }
}
```

---

## 5. TẦNG TUẦN TỰ HÓA DỮ LIỆU — SERIALIZATION LAYER

### 5.1. Tổng quan 15 DTO Classes

Hệ thống sử dụng **15 Data Transfer Object (DTO) classes** để tuần tự hóa/giải tuần tự hóa dữ liệu JSON trao đổi với GAMA:

```
┌──────────────────────────────────────────────────────────────────────┐
│                    SERIALIZATION LAYER (15 DTOs)                     │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────────┐  ┌─────────────────────┐                   │
│  │ ConnectionParameter │  │ PropertiesGAMA      │                   │
│  │ • precision         │  │ • id, hasPrefab     │                   │
│  │ • position (spawn)  │  │ • hasCollider       │                   │
│  │ • world (bounds)    │  │ • isInteractable    │                   │
│  └─────────────────────┘  │ • size, height      │                   │
│                            │ • material, RGBA    │                   │
│  ┌─────────────────────┐  │ • toFollow          │                   │
│  │ WorldJSONInfo       │  └─────────────────────┘                   │
│  │ • position[]        │                                             │
│  │ • names[]           │  ┌─────────────────────┐                   │
│  │ • propertyID[]      │  │ DEMData             │                   │
│  │ • pointsLoc[]       │  │ • rows[][]          │                   │
│  │ • pointsGeom[]      │  │ (Full heightmap)    │                   │
│  │ • offsetYGeom[]     │  └─────────────────────┘                   │
│  │ • ranking, players  │                                             │
│  └─────────────────────┘  ┌─────────────────────┐                   │
│                            │ DEMDataLoc          │                   │
│  ┌─────────────────────┐  │ • rows[][]          │                   │
│  │ EnemySpawnerInfo    │  │ • indexX, indexY     │                   │
│  │ • enemyspawners[]   │  │ (Partial patch)     │                   │
│  │ • spawnrates[]      │  └─────────────────────┘                   │
│  └─────────────────────┘                                             │
│                            ┌─────────────────────┐                   │
│  ┌─────────────────────┐  │ AnimationInfo        │                   │
│  │ FreshWaterSpawn     │  │ • names[]            │                   │
│  │ • pumpers[]         │  │ • parameters[]       │                   │
│  │ • spawnrates[]      │  │ • values[]           │                   │
│  └─────────────────────┘  │ • triggers[]         │                   │
│                            └─────────────────────┘                   │
│  ┌─────────────────────┐                                             │
│  │ SubsidenceInfo      │  ┌─────────────────────┐                   │
│  │ • waterLevelLocal   │  │ TeleportAreaInfo     │                   │
│  │ • waterLevelGlobal  │  │ • teleportId         │                   │
│  │ • score             │  │ • teleportPoints[]   │                   │
│  │ • seaLevel          │  │ • teleportHeight     │                   │
│  │ • seaLevelGlobal    │  └─────────────────────┘                   │
│  └─────────────────────┘                                             │
│                            ┌─────────────────────┐                   │
│  ┌─────────────────────┐  │ WallInfo             │                   │
│  │ EndOfGameInfo       │  │ • points[]           │                   │
│  │ • endOfGame         │  │ (Invisible colliders)│                   │
│  └─────────────────────┘  └─────────────────────┘                   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### 5.2. Các DTO quan trọng chi tiết

#### 5.2.1. ConnectionParameter — Khởi tạo hệ thống tọa độ

```csharp
[System.Serializable]
public class ConnectionParameter
{
    public int precision;           // Hệ số tỷ lệ tọa độ (thường = 1000)
    public List<int> position;      // Vị trí spawn người chơi [x, y, z]
    public List<int> world;         // Biên giới thế giới [minX, minY, maxX, maxY]
}
```

**Ý nghĩa `precision`:** Tất cả tọa độ trong GAMA được nhân với `precision` (1000) để truyền dưới dạng số nguyên, tránh mất độ chính xác khi serialize. Unity phải chia lại khi sử dụng.

#### 5.2.2. PropertiesGAMA — Định nghĩa kiểu đối tượng

```csharp
[System.Serializable]
public class PropertiesGAMA
{
    public string id;               // ID kiểu đối tượng duy nhất
    public bool hasCollider;        // Có thêm collider không
    public bool isInteractable;     // Có tương tác XR không
    public bool isGrabable;         // Có thể nhặt (XRGrabInteractable) không
    public bool hasPrefab;          // Dùng prefab hay tạo polygon mesh
    public string prefab;           // Đường dẫn prefab trong Resources/
    public int size;                // Kích thước (÷ precision)
    public int yOffset;             // Offset Y (÷ precision)
    public int rotationCoeff;       // Hệ số xoay (÷ precision)
    public int height;              // Chiều cao đùn (extrusion) (÷ precision)
    public bool is3D;               // Đùn 3D hay phẳng
    public string material;         // Tên vật liệu trong Resources/
    public byte red, green, blue, alpha; // Màu RGBA
    public bool toFollow;           // Theo dõi & gửi vị trí về GAMA

    [NonSerialized]
    public GameObject prefabObj;    // Tham chiếu prefab đã tải (runtime)
}
```

**Bảng quy tắc khởi tạo đối tượng:**

| hasPrefab | hasCollider | isInteractable | isGrabable | Kết quả |
|:---------:|:-----------:|:--------------:|:----------:|---------|
| ✓ | ✓ | ✓ | ✓ | Prefab + BoxCollider + XRGrabInteractable + Rigidbody |
| ✓ | ✓ | ✓ | ✗ | Prefab + BoxCollider + XRSimpleInteractable |
| ✓ | ✓ | ✗ | ✗ | Prefab + BoxCollider (chỉ va chạm) |
| ✗ | ✓ | ✗ | ✗ | PolygonMesh + MeshCollider |
| ✗ | ✗ | ✗ | ✗ | PolygonMesh chỉ hiển thị |

#### 5.2.3. WorldJSONInfo — Dữ liệu hình học thế giới

```csharp
[System.Serializable]
public class WorldJSONInfo
{
    public List<int> position;          // Tọa độ phẳng [x1,y1, x2,y2, ...]
    public List<string> names;          // Tên đối tượng
    public List<string> propertyID;     // ID kiểu → ánh xạ đến PropertiesGAMA.id
    public List<GAMAPoint> pointsLoc;   // Vị trí agent (điểm)
    public List<GAMAPoint> pointsGeom;  // Đỉnh đa giác (polygon vertices)
    public List<int> offsetYGeom;       // Offset Y cho mỗi đa giác (÷ precision)
    public List<int> ranking;           // Bảng xếp hạng (multiplayer)
    public List<string> players;        // ID người chơi (multiplayer)
    public bool isInit;                 // Cờ snapshot khởi tạo
}
```

#### 5.2.4. EnemySpawnerInfo — Cập nhật tốc độ sinh tác nhân mặn

```csharp
[System.Serializable]
public class EnemySpawnerInfo
{
    public List<string> enemyspawners;  // Danh sách InstanceID của spawner
    public List<int> spawnrates;        // Tốc độ spawn tương ứng (÷ precision)
}
```

#### 5.2.5. SubsidenceInfo — Dữ liệu sụt lún & mực nước

```csharp
[System.Serializable]
public class SubsidenceInfo
{
    public int waterLevelLocal;     // Mực nước cục bộ
    public int waterLevelGlobal;    // Mực nước toàn cục
    public int score;               // Điểm sụt lún (hiệu ứng hình ảnh)
    public int seaLevel;            // Mực nước biển cục bộ
    public int seaLevelGlobal;      // Mực nước biển toàn cục
}
```

---

## 6. HỆ THỐNG TÁC NHÂN TRONG QUẢN LÝ TÀI NGUYÊN NÔNG NGHIỆP

### 6.1. Mô hình tác nhân tổng quan

Hệ thống SIMPLE VU2 triển khai mô hình ABM với **4 loại tác nhân chính** tương tác trong không gian nông nghiệp:

```
┌═══════════════════════════════════════════════════════════════════════┐
║              MÔ HÌNH TÁC NHÂN NÔNG NGHIỆP (ABM)                    ║
╠═══════════════════════════════════════════════════════════════════════╣
║                                                                     ║
║  ┌──────────────┐    Tấn công     ┌──────────────┐                 ║
║  │ ENEMY AGENT  │ ──────────────► │ TREE BARRIER │                 ║
║  │ (Nước mặn)   │                 │ (Hàng rào    │                 ║
║  │              │ ◄────── Bẫy ─── │  cây xanh)   │                 ║
║  │ • Di chuyển   │                 │              │                 ║
║  │   theo waypoint│                │ • Bắt 1 enemy│                 ║
║  │ • Tấn công    │                 │ • HP giảm dần│                 ║
║  │   cây/công    │                 │ • Thả khi    │                 ║
║  │   trình       │                 │   chết       │                 ║
║  └──────┬───────┘                 └──────────────┘                 ║
║         │                                                           ║
║         │ Trung hòa bởi                                             ║
║         ▼                                                           ║
║  ┌──────────────┐    Sinh ra bởi   ┌──────────────┐                ║
║  │ ALLY AGENT   │ ◄────────────── │ WATER PUMP   │                ║
║  │ (Nước ngọt)  │                 │ (Máy bơm)    │                ║
║  │              │                 │              │                 ║
║  │ • Tìm & tiêu │                 │ • Spawn nước  │                ║
║  │   diệt nước  │                 │   ngọt định   │                ║
║  │   mặn        │                 │   kỳ          │                ║
║  │ • OnTrigger  │                 │ • GAMA điều   │                ║
║  │   Enter      │                 │   chỉnh rate  │                ║
║  └──────────────┘                 └──────────────┘                ║
║                                                                     ║
║       ┌──────────────────────────────────────┐                     ║
║       │ PLAYER AGENT (Người chơi VR)         │                     ║
║       │ • Trồng/Thu hoạch                     │                     ║
║       │ • Đặt máy bơm + cây rào              │                     ║
║       │ • Quyết định chiến lược mùa vụ       │                     ║
║       └──────────────────────────────────────┘                     ║
║                                                                     ║
╚═══════════════════════════════════════════════════════════════════════╝
```

### 6.2. Enemy Agent — Tác nhân nước mặn

**Class:** `Enemy.cs` — đại diện cho thực thể xâm nhập mặn, được spawn bởi `EnemySpawner`.

```csharp
public class Enemy : MonoBehaviour, IDamageable, IDamage
{
    // Chỉ số cơ bản
    [SerializeField] private int health = 2;            // Máu
    [SerializeField] private float moveSpeed = 2f;      // Tốc độ di chuyển
    [SerializeField] private float attackInterval = 5f; // Chu kỳ tấn công (giây)
    [SerializeField] private float attackRange = 2f;    // Bán kính tấn công
    [SerializeField] private int attackDamage = 3;      // Sát thương mỗi đòn
}
```

**Vòng đời tác nhân Enemy:**

```
EnemySpawner.Spawn()
    │
    ▼
┌── Khởi tạo ──┐
│ • HP = health │
│ • Speed sync  │
│   NavMeshAgent│
│ • Màu = Salty │
│   (#1B2FE0)   │
└──────┬────────┘
       │
       ▼
┌── Di chuyển ──┐
│ Theo waypoints│
│ (NavMesh)     │◄────── GAMA cập nhật vị trí mỗi 0.5s
│               │
│ Mỗi khoảng   │
│ attackInterval│
│ → OverlapSphere
│ → Tấn công    │
│   IDamageable │
└──────┬────────┘
       │
       ├── Bị Ally trung hòa (OnTriggerEnter)
       │   │
       │   ▼
       │   HP -= damage
       │   Màu chuyển dần → Neutral (#4CDDD2)
       │   │
       │   HP <= 0?
       │   │
       │   ▼
       │   Die():
       │   • Tag → "Water"
       │   • Layer → "Water"
       │   • StatisticsManager.IncreaseEnemyKillCount()
       │   • Destroy(3s)
       │
       └── Đến cuối waypoint → EnemyController xử lý
```

**Cơ chế trực quan hóa trạng thái:**

| Trạng thái | Màu bóng (_Shadow_Color) | Ý nghĩa |
|-----------|:---:|---------|
| Sống khỏe (100% HP) | `#1B2FE0` (Xanh đậm) | Nước mặn đậm đặc |
| Bị tấn công (50% HP) | Lerp → `#4CDDD2` | Đang bị pha loãng |
| Chết (0% HP) | `#4CDDD2` (Xanh nhạt) | Đã trung hòa thành nước |

### 6.3. EnemySpawner — Nguồn sinh tác nhân mặn

**Class:** `EnemySpawner.cs` — điểm xuất hiện của tác nhân nước mặn, tốc độ spawn được GAMA điều khiển.

```csharp
public class EnemySpawner : MonoBehaviour, ISpawner
{
    [SerializeField] private GameObject spawnPrefab;    // Prefab enemy
    [SerializeField] private float spawnRate = 1.0f;    // Chu kỳ spawn (giây)
    [SerializeField] private List<Transform> wayPoints; // Đường đi
    
    private int spawnCount = 10;        // Số lượng tối đa
    private int maxSpawnCount = 50;     // Giới hạn trên
    
    // GAMA gọi ReStartAutoSpawn() để cập nhật tốc độ
    public void ReStartAutoSpawn(int amount)
    {
        spawnCount = spawnRate == 0 ? minSpawnCount 
                   : Mathf.Max(minSpawnCount, (int)(spawnRate * 0.5));
        count = 0;
        InvokeRepeating("Spawn", 0.1f, 0.5f);
    }
}
```

**Công thức tính số lượng spawn:**

$$N_{spawn} = \max\left(N_{min},\; \left\lfloor R_{spawn} \times 0{,}5 \right\rfloor\right)$$

Trong đó:
- $N_{spawn}$ : Số lượng enemy sẽ spawn
- $R_{spawn}$ : Tốc độ spawn (từ GAMA, đã ÷ precision)
- $N_{min} = 1$ : Giới hạn dưới

### 6.4. Water Pump (Barrack) — Tác nhân máy bơm phòng thủ

**Class:** `Barrack.cs` — công trình phòng thủ chính, sinh ra tác nhân nước ngọt (Ally) để trung hòa nước mặn.

**Vòng đời:**

```
Người chơi đặt máy bơm (BuildSystem)
    │
    ▼
┌── Start() ──────────────────────────────┐
│ 1. Khởi tạo HP                          │
│ 2. Kiểm tra máy bơm lân cận            │
│    (OverlapSphere r=5m)                 │
│    → Nếu có: HP -= 20 (phạt dồn cụm)  │
│ 3. Đăng ký với GAMA:                    │
│    SimulationManager.createMovePumper()  │
└──────────────┬──────────────────────────┘
               │
               ▼
┌── Update() (mỗi frame) ────────────────┐
│ • Cập nhật HUD marker                   │
│ • Đếm ngược spawnRate                   │
│ • Nếu hết timer + CanSpawn():           │
│   → Instantiate(spawnPrefab, spawnPoint)│
│   → GameManagerScript.reduceNumPump()   │
│ • GAMA cập nhật SpawnRate định kỳ       │
└──────────────┬──────────────────────────┘
               │
               ├── TakeDamage() → Enemy tấn công
               │   HP -= damage
               │   HP <= 0? → Die()
               │
               ▼
┌── Die() ────────────────────────────────┐
│ 1. Spawn subsidencePrefab (hố sụt)      │
│ 2. Gọi GAMA: ("delete_water_pump")      │
│ 3. Phá hủy Ally lân cận (10m)           │
│ 4. Destroy(gameObject, 2s)               │
└──────────────────────────────────────────┘
```

**Đăng ký với GAMA server:**

```csharp
// Trong Barrack.Start()
SimulationManager sm = FindObjectOfType<SimulationManager>();
if (sm != null)
    sm.createMovePumper(gameObject);

// SimulationManager.createMovePumper()
public void createMovePumper(GameObject pump)
{
    string instanceID = pump.GetInstanceID().ToString();
    waterPumps[instanceID] = pump.GetComponent<Barrack>();
    
    // Gửi vị trí máy bơm đến GAMA (tọa độ đã chuyển đổi)
    Vector3 pos = pump.transform.position;
    int gamaX = converter.toGAMACRS_X(pos);
    int gamaY = converter.toGAMACRS_Y(pos);
    
    Dictionary<string, string> args = new Dictionary<string, string> {
        {"id", instanceID},
        {"x", gamaX.ToString()},
        {"y", gamaY.ToString()}
    };
    ConnectionManager.Instance.SendExecutableAsk("move_create_pumper", args);
}
```

### 6.5. TreeBarrier — Tác nhân hàng rào cây xanh

**Class:** `TreeBarrier.cs` — cơ chế phòng thủ sinh học, bẫy và giữ chân tác nhân nước mặn.

**Cơ chế hoạt động:**

```
                Vùng trigger
                (SphereCollider, r=3m)
                      ┌───────┐
                      │       │
         Enemy ──────►│ TREE  │
         di chuyển    │BARRIER│
                      │       │
                      └───┬───┘
                          │
                ┌─────────┴─────────┐
                │  OnTriggerEnter   │
                │  hoặc             │
                │  OverlapSphere    │
                │  (fallback scan)  │
                └─────────┬─────────┘
                          │
                  Enemy.CompareTag("Enemy")?
                  controller.IsTrapped == false?
                          │ Có
                          ▼
                ┌─────────────────────┐
                │ TrapEnemy()         │
                │ • trappedEnemy = obj│
                │ • controller.       │
                │   SetTrapped(true)  │
                │ (Dừng di chuyển)    │
                └─────────┬───────────┘
                          │
                    Mỗi frame:
                          │
                ┌─────────┴───────────┐
                │ • Kéo enemy về gốc  │
                │   cây (MoveTowards) │
                │ • Ăn mòn:           │
                │   HP -= corrosion   │
                │   × deltaTime       │
                │ • Cập nhật visual:  │
                │   Xanh → Vàng → Nâu│
                │   Scale: 100%→80%   │
                └─────────┬───────────┘
                          │
                ┌─────────┴───────────┐
                │                     │
               HP=0               Enemy chết
                │                     │
                ▼                     ▼
          Die():               ReleaseEnemy():
          • Thả enemy          • controller.
          • Thông báo GAMA:      SetTrapped(false)
            deleteTreeBarrier  • Sẵn sàng bắt
          • Anim "Tree_Die"      con khác
          • Destroy(2s)
```

**Hệ thống trực quan theo HP:**

$$\text{Color}(t) = \text{Lerp}\left(\text{deadColor},\; \text{healthyColor},\; \frac{HP_{current}}{HP_{max}}\right)$$

$$\text{Scale}(t) = \text{initialScale} \times \text{Lerp}\left(\text{minScale},\; 1{,}0,\; \frac{HP_{current}}{HP_{max}}\right)$$

| HP (%) | Màu sắc | Scale | Animation |
|:------:|---------|:-----:|-----------|
| 100%–51% | Trắng/Xanh (healthyColor) | 100% | `Tree_Good` |
| 50%–1% | Vàng → Nâu | 90%→80% | `Tree_Bad` |
| 0% | Nâu (deadColor: #664019) | 80% | `Tree_Die` |

---

## 7. GIAO THỨC TRUYỀN THÔNG UNITY–GAMA

### 7.1. Bảng message gửi đi (Unity → GAMA)

| # | Action | Tham số | Tần suất | Mục đích |
|:-:|--------|---------|:--------:|---------|
| 1 | `create_init_player` | `id` | 1 lần | Khởi tạo player trong GAMA (Direct mode) |
| 2 | `new_connection` | `id` | 1 lần | Đăng ký kết nối mới (Middleware mode) |
| 3 | `send_init_data` | — | Poll 2s | Yêu cầu dữ liệu thế giới (LOADING_DATA) |
| 4 | `player_ready` | — | 1 lần | Thông báo sẵn sàng vào game |
| 5 | `update_player_pos` | `id, score, dtree, fwater, remaining_time, life_tree, quality` | 0.5s | Cập nhật vị trí + KPI người chơi |
| 6 | `update_salty_water` | `swsStr, xsStr, ysStr` | 0.5s | Vị trí enemy spawner (CSV) |
| 7 | `update_fresh_water` | `fwsStr, fxsStr, fysStr` | 0.5s | Vị trí ally (CSV) |
| 8 | `create_enemy_spawners` | `esStr, exsStr, eysStr` | 1 lần | Đăng ký tất cả enemy spawner |
| 9 | `move_create_pumper` | `id, x, y` | Per pump | Đăng ký máy bơm mới |
| 10 | `delete_water_pump` | `id` | Per death | Thông báo máy bơm bị phá |
| 11 | `create_tree_barrier` | `id, x, y` | Per tree | Đăng ký cây rào mới |
| 12 | `delete_tree_barrier` | `id` | Per death | Thông báo cây rào chết |
| 13 | `create_trees` | `treesStr, txsStr, tysStr` | 1 lần | Đăng ký tất cả cây trên bản đồ |
| 14 | `receive_geometries` | `geoms` | Editor | Export hình học Unity → GAMA |
| 15 | `player_finish_game` | — | 1 lần | Kết thúc game |

### 7.2. Bảng message nhận vào (GAMA → Unity)

| # | Key JSON | DTO Class | Xử lý | Mục đích |
|:-:|----------|-----------|-------|---------|
| 1 | `precision` | `ConnectionParameter` | 1 lần | Hệ số tỷ lệ + vị trí spawn |
| 2 | `properties` | `AllProperties` | 1 lần | Định nghĩa kiểu đối tượng |
| 3 | `pointsLoc` | `WorldJSONInfo` | 1 lần | Hình học thế giới + vị trí agent |
| 4 | `rows` | `DEMData` | Liên tục | Cập nhật heightmap toàn bộ |
| 5 | `rows_update` | `DEMDataLoc` | Liên tục | Cập nhật heightmap cục bộ |
| 6 | `enemyspawners` | `EnemySpawnerInfo` | Liên tục | Tốc độ spawn enemy |
| 7 | `pumpers` | `FreshWaterSpawn` | Liên tục | Tốc độ spawn nước ngọt |
| 8 | `subsidences` | `SubsidenceInfo` | Liên tục | Mực nước + sụt lún |
| 9 | `triggers` | `AnimationInfo` | Liên tục | Lệnh animation cho geometry |
| 10 | `teleportation` | `TeleportAreaInfo` | Liên tục | Vùng dịch chuyển XR |
| 11 | `walls` | `WallInfo` | Liên tục | Tường va chạm vô hình |
| 12 | `endOfGame` | `EndOfGameInfo` | 1 lần | Tín hiệu kết thúc game |
| 13 | `ranking` | (trong WorldJSONInfo) | Liên tục | Bảng xếp hạng multiplayer |
| 14 | `indexX` | (trong DEMDataLoc) | Liên tục | Tọa độ X patch heightmap |
| 15 | `teleportId` | (trong TeleportAreaInfo) | Liên tục | ID vùng dịch chuyển |

### 7.3. Định dạng nén CSV

Để giảm tải mạng, vị trí nhiều thực thể được nén thành **3 chuỗi CSV** thay vì mảng JSON đối tượng:

```
Thay vì:
[
    {"id": "123", "x": 100, "y": 200},
    {"id": "456", "x": 150, "y": 250},
    {"id": "789", "x": 175, "y": 300}
]

Sử dụng:
{
    "swsStr":  "123,456,789",
    "xsStr":   "100,150,175",
    "ysStr":   "200,250,300"
}
```

**Ưu điểm:**
- Giảm ~60% kích thước payload so với JSON đối tượng
- Giảm overhead deserialize (chỉ cần `String.Split`)
- Phù hợp cho đồng bộ tần suất cao (0.5s)

### 7.4. Ví dụ luồng message hoàn chỉnh

**Kịch bản: Người chơi đặt máy bơm → GAMA cập nhật tốc độ spawn**

```
t=0.0s  [Unity] Người chơi đặt máy bơm tại (10, 0, 5)
        │
        ├── Barrack.Start() → SimulationManager.createMovePumper()
        │
t=0.1s  [Unity → GAMA]
        {
            "type": "ask",
            "action": "move_create_pumper",
            "args": "{\"id\":\"12345\",\"x\":\"10000\",\"y\":\"5000\"}",
            "agent": "simulation[0].unity_linker[0]"
        }
        │
t=0.3s  [GAMA] Nhận thông tin máy bơm, tính toán ảnh hưởng lên mô hình ABM
        │       → Cập nhật tốc độ spawn cho các spawner lân cận
        │
t=0.5s  [GAMA → Unity]
        {
            "type": "json_output",
            "contents": {
                "pumpers": ["12345"],
                "spawnrates": [2000]
            }
        }
        │
t=0.5s  [Unity] SimulationManager nhận → receivedFreshWater = true
        │
t=0.6s  [Unity] FixedUpdate() xử lý:
        │       → Barrack.SpawnRate = (2000 / 2) / 1000 = 1.0 (giây)
        │
t=1.6s  [Unity] Barrack spawn FreshWater → Ally di chuyển tìm Enemy
```

---

## 8. HỆ THỐNG CHUYỂN ĐỔI TỌA ĐỘ

### 8.1. Bài toán chuyển đổi

GAMA và Unity sử dụng **hệ tọa độ khác nhau** — đây là thách thức kỹ thuật cốt lõi:

| Thuộc tính | GAMA | Unity |
|-----------|------|-------|
| **Hệ trục** | X–Y (2D mặt phẳng) | X–Y–Z (3D, Y hướng lên) |
| **Đơn vị** | Số nguyên × precision | Float (mét) |
| **Hướng Y** | Y tăng = đi xuống (kiểu GIS) | Z tăng = đi sâu (forward) |
| **Gốc tọa độ** | CRS offset | World origin (0,0,0) |

### 8.2. CoordinateConverter

```csharp
public class CoordinateConverter
{
    private int precision;          // Hệ số tỷ lệ (thường = 1000)
    private float GamaCRSCoefX;     // Hệ số nhân trục X
    private float GamaCRSCoefY;     // Hệ số nhân trục Y (GAMA Y → Unity Z)
    private float GamaCRSOffsetX;   // Offset trục X
    private float GamaCRSOffsetY;   // Offset trục Y
    
    // GAMA 2D → Unity 2D (trên mặt phẳng XZ)
    public Vector2 fromGAMACRS2D(int x, int y)
    {
        float ux = ((float)x / precision) * GamaCRSCoefX + GamaCRSOffsetX;
        float uz = ((float)y / precision) * GamaCRSCoefY + GamaCRSOffsetY;
        return new Vector2(ux, uz);
    }
    
    // GAMA 3D → Unity 3D
    public Vector3 fromGAMACRS(int x, int y, int z)
    {
        float ux = ((float)x / precision) * GamaCRSCoefX + GamaCRSOffsetX;
        float uy = ((float)z / precision);  // GAMA Z → Unity Y (chiều cao)
        float uz = ((float)y / precision) * GamaCRSCoefY + GamaCRSOffsetY;
        return new Vector3(ux, uy, uz);
    }
    
    // Unity → GAMA (chiều ngược lại)
    public int toGAMACRS_X(Vector3 unityPos)
    {
        return (int)((unityPos.x - GamaCRSOffsetX) / GamaCRSCoefX * precision);
    }
    
    public int toGAMACRS_Y(Vector3 unityPos)
    {
        return (int)((unityPos.z - GamaCRSOffsetY) / GamaCRSCoefY * precision);
    }
}
```

### 8.3. Công thức chuyển đổi

**GAMA → Unity:**

$$x_{Unity} = \frac{x_{GAMA}}{P} \times C_x + O_x$$

$$z_{Unity} = \frac{y_{GAMA}}{P} \times C_y + O_y$$

$$y_{Unity} = \frac{z_{GAMA}}{P}$$

**Unity → GAMA:**

$$x_{GAMA} = \left\lfloor \frac{x_{Unity} - O_x}{C_x} \times P \right\rfloor$$

$$y_{GAMA} = \left\lfloor \frac{z_{Unity} - O_y}{C_y} \times P \right\rfloor$$

Trong đó:
- $P$ : Precision (= 1000)
- $C_x, C_y$ : Hệ số tỷ lệ CRS (thường = 1.0)
- $O_x, O_y$ : Offset gốc tọa độ CRS

### 8.4. Bảng ánh xạ trục

```
        GAMA                              Unity
        
    Y ▲                              Y ▲ (up)
      │                                │
      │                                │
      │                                │
      │                                │
      └──────► X                       └──────► X
     /                                /
    Z (chiều cao)                    Z (forward)
    
    Ánh xạ:
    GAMA X  ──── × coefX + offsetX ────►  Unity X
    GAMA Y  ──── × coefY + offsetY ────►  Unity Z
    GAMA Z  ──── ÷ precision ──────────►  Unity Y
```

---

## 9. LUỒNG DỮ LIỆU MÔ PHỎNG

### 9.1. Giai đoạn khởi tạo (LOADING_DATA)

```
┌────────┐                                          ┌─────────┐
│ Unity  │                                          │  GAMA   │
└───┬────┘                                          └────┬────┘
    │                                                    │
    │── send_init_data ──────────────────────────────────►│
    │                                                    │
    │◄────────────────────── precision ──────────────────│
    │   {precision: 1000, position: [x,y,z],             │
    │    world: [minX,minY,maxX,maxY]}                   │
    │                                                    │
    │   → CoordinateConverter khởi tạo                   │
    │   → Player spawn tại position/precision            │
    │                                                    │
    │◄────────────────────── properties ────────────────│
    │   [{id:"tree", hasPrefab:true, prefab:"Tree1",    │
    │     hasCollider:true, isInteractable:false,        │
    │     size:1500, height:3000, ...}, ...]             │
    │                                                    │
    │   → Tải prefab từ Resources/                       │
    │   → Cache vào dictionary                           │
    │                                                    │
    │◄────────────────────── pointsLoc ────────────────│
    │   {position:[x1,y1,x2,y2,...],                     │
    │    names:["tree_01","house_01",...],                │
    │    propertyID:["tree","house",...],                 │
    │    pointsLoc:[{x,y,z},...],                        │
    │    pointsGeom:[{x,y},...]}                         │
    │                                                    │
    │   → Khởi tạo GameObjects:                          │
    │     ├── Prefab objects (hasPrefab=true)             │
    │     │   + BoxCollider + XR components               │
    │     └── Polygon meshes (hasPrefab=false)            │
    │         + MeshCollider via PolygonGenerator         │
    │                                                    │
    │── player_ready ────────────────────────────────────►│
    │                                                    │
    │   [Chuyển sang GameState.GAME]                     │
```

### 9.2. Giai đoạn gameplay (GAME) — Vòng lặp đồng bộ

```
┌────────────────────────────────────────────────────────────────────┐
│                  VÒNG LẶP ĐỒNG BỘ (mỗi 0.5 giây)                │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  Unity FixedUpdate()                                               │
│  ┌─────────────────┐     ┌─────────────────┐                     │
│  │ Gửi vị trí      │     │ Gửi vị trí      │                     │
│  │ Enemy Spawners   │────►│ Ally (FreshWater)│                     │
│  │ (CSV: id,x,y)   │     │ (CSV: id,x,y)   │                     │
│  └────────┬────────┘     └────────┬────────┘                     │
│           │                       │                               │
│           └───────────┬───────────┘                               │
│                       │                                           │
│                       ▼                                           │
│  ┌─────────────────────────────────┐                             │
│  │ Gửi vị trí + KPI người chơi    │                             │
│  │ • id, score, remaining_time     │                             │
│  │ • life_tree (HP trung bình cây) │                             │
│  │ • harvest_quantity              │                             │
│  │ • dtree (số cây sống)           │                             │
│  │ • fwater (số ally sống)         │                             │
│  └────────────────┬────────────────┘                             │
│                   │                                               │
│                   ▼                                               │
│  ════════════ WebSocket ════════════                              │
│                   │                                               │
│                   ▼                                               │
│  ┌─────────────────────────────────┐                             │
│  │        GAMA Server              │                             │
│  │ • Tính toán mô hình ABM        │                             │
│  │ • Cập nhật spawn rates          │                             │
│  │ • Tính mực nước/sụt lún        │                             │
│  │ • Cập nhật DEM (nếu có)        │                             │
│  │ • Tính animation triggers      │                             │
│  └────────────────┬────────────────┘                             │
│                   │                                               │
│                   ▼                                               │
│  ┌─────────────────────────────────┐                             │
│  │ Nhận & xử lý trên Main Thread  │                             │
│  │                                 │                             │
│  │ enemyspawners → cập nhật       │                             │
│  │   EnemySpawner.SpawnRate        │                             │
│  │                                 │                             │
│  │ pumpers → cập nhật             │                             │
│  │   Barrack.SpawnRate             │                             │
│  │                                 │                             │
│  │ subsidences → cập nhật         │                             │
│  │   mực nước + hiệu ứng sụt lún  │                             │
│  │                                 │                             │
│  │ rows/rows_update → cập nhật    │                             │
│  │   TerrainData heightmap         │                             │
│  │                                 │                             │
│  │ triggers → phát animation       │                             │
│  │   trên geometry objects         │                             │
│  └─────────────────────────────────┘                             │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

### 9.3. Quản lý thực thể (Entity Management)

`SimulationManager` duy trì 4 dictionary theo dõi mọi thực thể đã đăng ký:

```csharp
// Máy bơm nước: InstanceID → Component Barrack
Dictionary<string, Barrack> waterPumps;

// Spawner enemy: InstanceID → Component EnemySpawner
Dictionary<string, EnemySpawner> enemySpawners;

// Cây rào: InstanceID → GameObject
Dictionary<string, GameObject> treeBarriers;

// Hình học GAMA: Tên đối tượng → [GameObject, PropertiesGAMA]
Dictionary<string, List<object>> geometryMap;
```

**Luồng đăng ký/hủy đăng ký:**

```
Đăng ký (Register):
  Object.Start() 
    → SimulationManager.createXxx(gameObject)
    → dictionary[instanceID] = component
    → SendExecutableAsk("create_xxx", {id, x, y})

Hủy đăng ký (Unregister):
  Object.Die()
    → SimulationManager.deleteXxx(gameObject)
    → dictionary.Remove(instanceID)
    → SendExecutableAsk("delete_xxx", {id})
```

---

## 10. CẦU NỐI GAMA BRIDGE — TÍCH HỢP GAMEPLAY NÔNG NGHIỆP

### 10.1. GAMABridgeLevel1 — Cầu nối Level 1

**Class:** `GAMABridgeLevel1.cs` (387 dòng) — thành phần cầu nối chuyên biệt cho Level 1, kết nối logic gameplay nông nghiệp với hệ thống GAMA.

**Chức năng chính:**

| Chức năng | Phương thức | Mô tả |
|----------|------------|-------|
| Đồng bộ KPI | `updatePlayerPos()` | Gửi điểm số, thời gian, HP cây, số lượng thu hoạch |
| Đăng ký spawner | `createEnemySpawners()` | Đăng ký tất cả EnemySpawner với GAMA |
| Đăng ký cây | `createTrees()` | Đăng ký tất cả cây trên bản đồ |
| Tính HP cây | `GetLifeTree()` | Trung bình HP các TreeBarrier sống |
| Tính sản lượng | `GetHarvestQuantity()` | Đếm sản phẩm đã thu hoạch |

### 10.2. Dữ liệu KPI gửi đến GAMA

Mỗi 0.5 giây, `GAMABridgeLevel1` tổng hợp và gửi các chỉ số hiệu suất nông nghiệp:

```csharp
public void updatePlayerPos()
{
    Dictionary<string, string> args = new Dictionary<string, string> {
        {"id",              ConnectionManager.Instance.GetConnectionId()},
        {"score",           gameManager.GetScore().ToString()},
        {"remaining_time",  gameManager.GetRemainingTime().ToString()},
        {"life_tree",       GetLifeTree().ToString()},
        {"harvest_quantity", GetHarvestQuantity().ToString()},
        {"dtree",           GetAliveTreeCount().ToString()},
        {"fwater",          GetAliveAllyCount().ToString()}
    };
    
    ConnectionManager.Instance.SendExecutableAsk("update_player_pos", args);
}
```

**Bảng chỉ số KPI:**

| KPI | Mô tả | Cách tính | Ý nghĩa nông nghiệp |
|-----|-------|-----------|---------------------|
| `score` | Tổng điểm | `GameManager.GetScore()` | Tổng thu nhập nông nghiệp |
| `remaining_time` | Thời gian còn lại | `GameManager.GetRemainingTime()` | Thời gian mùa vụ |
| `life_tree` | HP trung bình cây | $\frac{\sum HP_i}{N_{alive}}$ | Sức khỏe hàng rào phòng thủ |
| `harvest_quantity` | Số lượng thu hoạch | Đếm sản phẩm đã gom | Năng suất thực tế |
| `dtree` | Số cây sống | Đếm TreeBarrier(HP>0) | Khả năng phòng thủ |
| `fwater` | Số ally sống | Đếm tag "Ally" | Hiệu lực máy bơm |

### 10.3. Tác động mô hình ABM lên gameplay nông nghiệp

GAMA sử dụng dữ liệu KPI để **điều chỉnh mô hình ABM**, tạo vòng phản hồi hai chiều:

```
┌──────────────────────────────────────────────────────────────────┐
│              VÒNG PHẢN HỒI GAMA ↔ UNITY                        │
│                                                                  │
│  Unity                            GAMA ABM                      │
│  ┌────────────┐                   ┌────────────┐                │
│  │ Người chơi │── KPI ──────────►│ Mô hình    │                │
│  │ đặt máy    │   (score, dtree, │ tác nhân   │                │
│  │ bơm + cây  │    fwater, time) │            │                │
│  │ rào        │                   │ Tính toán: │                │
│  └──────┬─────┘                   │ • Mật độ   │                │
│         │                         │   mặn      │                │
│         │                         │ • Tốc độ   │                │
│         │                         │   xâm nhập │                │
│  ┌──────┴──────┐  ◄── spawn ──── │ • Spawn    │                │
│  │ Enemy tốc   │      rates      │   rate mới │                │
│  │ độ thay đổi │                  │ • Mực nước │                │
│  │             │  ◄── subsidence │ • Sụt lún  │                │
│  │ FreshWater  │                  │            │                │
│  │ tốc độ     │  ◄── pumper     └────────────┘                │
│  │ thay đổi   │      rates                                     │
│  └─────────────┘                                                │
│                                                                  │
│  ───► Nếu score cao + nhiều cây sống:                           │
│       GAMA giảm mật度 spawn enemy (nông trại được bảo vệ tốt)  │
│                                                                  │
│  ───► Nếu score thấp + ít phòng thủ:                            │
│       GAMA tăng tốc độ spawn enemy (xâm nhập mặn mạnh hơn)    │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## 11. HỆ THỐNG TÁC NHÂN PHÒNG THỦ NÔNG NGHIỆP

### 11.1. Tương tác giữa các tác nhân

Hệ thống phòng thủ nông nghiệp mô phỏng thực tế quản lý thủy lợi tại ĐBSCL thông qua **3 cơ chế tương tác tác nhân**:

#### 11.1.1. Trung hòa (Neutralization): Ally vs Enemy

```
FreshWater (Ally)        Enemy (SaltyWater)
    tag: "Ally"             tag: "Enemy"
         │                       │
         └──── OnTriggerEnter ───┘
                    │
                    ▼
              Enemy.TakeDamage()
              Ally.TakeDamage()
                    │
                    ▼
         Cả hai giảm HP → tiêu diệt lẫn nhau
         (Mô phỏng: nước ngọt pha loãng nước mặn)
```

**Cơ sở khoa học:** Trong thực tế, việc bơm nước ngọt vào kênh / ruộng sẽ pha loãng nước mặn, giảm nồng độ muối. Hệ thống mô phỏng điều này qua va chạm 1:1 giữa Ally và Enemy.

#### 11.1.2. Bẫy (Trapping): TreeBarrier vs Enemy

```
TreeBarrier                      Enemy
    SphereCollider(trigger)        NavMeshAgent
         │                              │
         └── OnTriggerEnter / ──────────┘
             OverlapSphere
                  │
                  ▼
         controller.SetTrapped(true)
         (Dừng NavMeshAgent)
                  │
                  ▼
         Enemy bị kéo về gốc cây
         Cây bị ăn mòn (HP giảm dần)
                  │
         ┌────────┴────────┐
         │                 │
    Cây chết:          Enemy chết:
    Thả enemy          Cây giải phóng
    (tiếp tục đi)     (bắt con khác)
```

**Cơ sở khoa học:** Rừng ngập mặn và hàng cây ven sông đóng vai trò "bộ đệm sinh học", hấp thụ và giảm thiểu tác động xâm nhập mặn. Cây sẽ dần suy yếu khi tiếp xúc mặn kéo dài.

#### 11.1.3. Sinh sản (Spawning): WaterPump → Ally

```
Barrack (WaterPump)                  GAMA Server
    │                                     │
    │── createMovePumper() ──────────────►│
    │                                     │
    │◄── spawnRate cập nhật ─────────────│
    │                                     │
    │   Mỗi spawnRate giây:              │
    │   ├── CanSpawn()? (player gần?)    │
    │   ├── Instantiate(FreshWater)      │
    │   └── GameManager.reduceNumPump()  │
    │                                     │
    │   HP giảm khi bị Enemy tấn công    │
    │   └── Die() → { subsidencePrefab   │
    │               + delete_water_pump } │
```

**Cơ sở khoa học:** Trạm bơm thủy lợi tại ĐBSCL bơm nước ngọt từ kênh chính vào ruộng để rửa mặn. Hiệu suất bơm phụ thuộc vào vị trí đặt và tình trạng cơ sở hạ tầng.

### 11.2. Hệ thống sụt lún (Subsidence)

Khi máy bơm bị phá hủy, hệ thống tạo hiệu ứng sụt lún — mô phỏng hiện tượng sụt lún đất do khai thác nước ngầm quá mức:

```csharp
// Trong Barrack.Die()
public void Die()
{
    // 1. Tạo hố sụt tại vị trí máy bơm
    if (subsidencePrefab != null)
        Instantiate(subsidencePrefab, subsidenceSpawnPoint.position, Quaternion.identity);
    
    // 2. Thông báo GAMA
    SimulationManager sm = FindObjectOfType<SimulationManager>();
    if (sm != null)
    {
        Dictionary<string, string> args = new Dictionary<string, string> {
            {"id", gameObject.GetInstanceID().ToString()}
        };
        ConnectionManager.Instance.SendExecutableAsk("delete_water_pump", args);
    }
    
    // 3. Phá hủy ally lân cận (r=10m)
    Collider[] nearbyAllies = Physics.OverlapSphere(transform.position, 10f);
    foreach (var ally in nearbyAllies)
        if (ally.CompareTag("Ally")) Destroy(ally.gameObject);
    
    // 4. Xóa bản thân
    Destroy(gameObject, 2f);
}
```

---

## 12. HỆ THỐNG HÌNH HỌC VÀ ĐỊA HÌNH

### 12.1. GAMAGeometryLoader — Import hình học

`GAMAGeometryLoader` chịu trách nhiệm chuyển đổi dữ liệu hình học từ GAMA thành GameObjects trong Unity:

```
Dữ liệu GAMA                          Unity GameObjects
┌──────────────────┐                ┌─────────────────────┐
│ WorldJSONInfo    │                │                     │
│                  │                │ hasPrefab = true:   │
│ names[i]         │──────────────►│ ┌──────────────┐    │
│ propertyID[i]    │                │ │ Prefab       │    │
│ pointsLoc[i]     │                │ │ + Scale      │    │
│                  │                │ │ + Collider   │    │
│                  │                │ │ + XR Interac.│    │
│                  │                │ └──────────────┘    │
│                  │                │                     │
│ position[]       │──────────────►│ hasPrefab = false:  │
│ pointsGeom[]     │                │ ┌──────────────┐    │
│ offsetYGeom[]    │                │ │ PolygonMesh  │    │
│                  │                │ │ (PolyExtruder│    │
│                  │                │ │ + Material   │    │
│                  │                │ │ + MeshCollider│   │
│                  │                │ └──────────────┘    │
└──────────────────┘                └─────────────────────┘
```

### 12.2. PolygonGenerator — Tạo mesh 3D

```csharp
public GameObject GeneratePolygons(bool editMode, String name, 
    List<int> points, PropertiesGAMA prop, int precision)
{
    // 1. Giải mã cặp tọa độ GAMA → Vector2[] trong Unity
    List<Vector2> pts = new List<Vector2>();
    for (int i = 0; i < points.Count - 1; i += 2)
    {
        Vector2 p = converter.fromGAMACRS2D(points[i], points[i + 1]);
        pts.Add(p);
    }
    
    // 2. Áp dụng màu sắc & vật liệu
    Color32 col = new Color32(prop.red, prop.green, prop.blue, prop.alpha);
    Material mat = Resources.Load<Material>(prop.material);
    
    // 3. Đùn (extrude) đa giác 2D thành mesh 3D
    float height = (float)prop.height / precision;
    GameObject obj = GeneratePolygon(editMode, name, pts.ToArray(), 
                                    height, mat, col);
    
    // 4. Tạo MeshCollider từ mesh đã cache
    //    (surroundMesh + bottomMesh + topMesh)
    return obj;
}
```

### 12.3. Hệ thống cập nhật địa hình (DEM)

GAMA có thể cập nhật địa hình Unity theo 2 cách:

**Cập nhật toàn bộ (DEMData):**
```csharp
// Thay thế toàn bộ heightmap
float[,] heights = terrainData.GetHeights(0, 0, width, height);
for (int r = 0; r < dem.rows.Count; r++)
    for (int c = 0; c < dem.rows[r].Count; c++)
        heights[r, c] = (float)dem.rows[r][c] / (precision * valMax);
terrainData.SetHeights(0, 0, heights);
```

**Cập nhật cục bộ (DEMDataLoc):**
```csharp
// Chỉ cập nhật vùng tại (indexX, indexY)
float[,] patch = new float[patchH, patchW];
for (int r = 0; r < dem.rows.Count; r++)
    for (int c = 0; c < dem.rows[r].Count; c++)
        patch[r, c] = (float)dem.rows[r][c] / (precision * valMax);
terrainData.SetHeights(dem.indexX, dem.indexY, patch);
```

### 12.4. GAMAGeometryExport — Export hình học ngược

Hệ thống hỗ trợ **export hình học Unity → GAMA** qua Editor menu:

```csharp
public void ManageGeometries(GameObject objectToSend, string ip, string port,
                             float x, float y, float ox, float oy)
{
    // 1. Tạo CoordinateConverter với tham số cho CRS
    UnityGeometry ug = new UnityGeometry(objectToSend, 
                        new CoordinateConverter(10000, x, y, ox, oy));
    
    // 2. Serialize mesh Unity thành JSON
    string message = ug.ToJSON();
    
    // 3. Gửi đến GAMA qua action "receive_geometries"
    Dictionary<string, string> argsToSend = new Dictionary<string, string> {
        {"geoms", message}
    };
    ConnectionManager.Instance.SendExecutableAsk("receive_geometries", argsToSend);
}
```

---

## 13. PHÂN TÍCH HIỆU NĂNG VÀ TỐI ƯU HÓA

### 13.1. Chiến lược tối ưu mạng

| Chiến lược | Triển khai | Tác động |
|-----------|-----------|---------|
| **Bộ đếm thời gian lệch pha** | 3 timer độc lập cho Enemy/Ally/Player | Tránh gửi đồng thời, phân phối tải đều |
| **Nén CSV** | ID, X, Y nén thành 3 chuỗi | Giảm ~60% kích thước payload |
| **Tọa độ số nguyên** | Nhân precision × 1000 | Tránh mất chính xác khi serialize float |
| **Xử lý trì hoãn** | 12 boolean flags → main thread | Tránh race condition, thread-safe |
| **Gửi bất đồng bộ** | `socket.SendAsync()` | Không block main thread |
| **Polling có giới hạn** | `send_init_data` mỗi 2s | Tránh overwhelm server khi loading |

### 13.2. Bảng tải mạng ước tính

| Loại message | Kích thước | Tần suất | Băng thông |
|-------------|:----------:|:--------:|:----------:|
| `update_player_pos` | ~200 bytes | 2/s | ~400 B/s |
| `update_salty_water` (50 entities) | ~500 bytes | 2/s | ~1 KB/s |
| `update_fresh_water` (20 entities) | ~300 bytes | 2/s | ~600 B/s |
| `enemyspawners` (response) | ~200 bytes | ~1/s | ~200 B/s |
| `pumpers` (response) | ~150 bytes | ~1/s | ~150 B/s |
| `subsidences` (response) | ~100 bytes | ~1/s | ~100 B/s |
| **Tổng cộng (ước tính)** | | | **~2.5 KB/s** |

### 13.3. Chiến lược Thread Safety

```
WebSocket Thread                    Main Thread (FixedUpdate)
    │                                    │
    │── OnMessage ──►                    │
    │   Parse JSON    │                  │
    │   Set flag:     │                  │
    │   receivedXxx   │                  │
    │   = true        │                  │
    │                 │                  │
    │                 │  ◄── Check flags ─│
    │                 │      if (flag) {  │
    │                 │        flag=false │
    │                 │        Process() ─│── Instantiate / Update
    │                 │      }            │   GameObjects (safe)
    │                 │                   │
```

Chiến lược này đảm bảo:
- **WebSocket thread** chỉ parse JSON và set flags — **O(1) thời gian**
- **Main thread** thực hiện mọi Unity API calls (Instantiate, SetHeights, v.v.)
- Tránh `UnityException: can only be called from the main thread`

### 13.4. Quản lý bộ nhớ

| Cơ chế | Mô tả |
|--------|-------|
| **Dictionary tracking** | Mọi entity được theo dõi bằng InstanceID → cho phép O(1) lookup |
| **Lazy prefab loading** | Prefab chỉ được load khi nhận `properties` |
| **Destroy with delay** | `Destroy(obj, 2f)` cho phép animation kết thúc |
| **Pooling potential** | EnemySpawner / Barrack sử dụng `InvokeRepeating` — có thể nâng cấp lên Object Pool |

---

## 14. ĐÁNH GIÁ VÀ THẢO LUẬN

### 14.1. Ưu điểm của kiến trúc tích hợp

**Kiến trúc phân tầng rõ ràng:**
- 5 tầng phân tách trách nhiệm → dễ bảo trì, dễ mở rộng
- Singleton pattern cho ConnectionManager → truy cập toàn cục đơn giản
- Observer pattern (Event System) → giảm coupling giữa các thành phần

**Giao thức truyền thông linh hoạt:**
- Hỗ trợ 2 chế độ (Middleware / Direct) → thích ứng nhiều cấu hình triển khai
- JSON-RPC tiêu chuẩn → dễ debug, dễ mở rộng
- Circuit breaker → tự phục hồi khi mất kết nối

**Tích hợp ABM chân thực:**
- 4 loại tác nhân tương tác phức tạp → mô phỏng động lực xâm nhập mặn sát thực tế
- Vòng phản hồi GAMA ↔ Unity → GAMA điều chỉnh mô hình dựa trên hành vi người chơi
- KPI nông nghiệp → cầu nối giữa mô hình toán học và trải nghiệm VR

### 14.2. So sánh với các phương pháp tích hợp khác

| Tiêu chí | GAMA–Unity (SIMPLE VU2) | REST API | gRPC | Shared Memory |
|----------|:----------------------:|:--------:|:----:|:-------------:|
| Độ trễ | Thấp (~10ms) | Cao (~100ms+) | Thấp (~5ms) | Rất thấp (~1ms) |
| Hai chiều | ✓ (Full-duplex) | ✗ (Request-Response) | ✓ | ✓ |
| Phức tạp triển khai | Trung bình | Đơn giản | Phức tạp | Rất phức tạp |
| Cross-platform | ✓ | ✓ | ✓ | ✗ |
| Hỗ trợ VR real-time | ✓ | ✗ | ✓ | ✓ |
| Middleware proxy | ✓ | ✓ | ✗ | ✗ |
| Scalable (multiplayer) | ✓ | ✓ | ✓ | ✗ |

### 14.3. Hạn chế và hướng cải thiện

**Hạn chế kỹ thuật:**

| Hạn chế | Tác động | Đề xuất cải thiện |
|---------|---------|-------------------|
| **Thread safety thủ công** (12 boolean flags) | Dễ bỏ sót, khó mở rộng | Sử dụng `ConcurrentQueue<Action>` để thay thế |
| **Không có Object Pooling** cho Enemy/Ally | GC spike khi spawn/destroy nhiều | Triển khai Object Pool pattern |
| **Hardcoded agent path** `"simulation[0].unity_linker[0]"` | Không linh hoạt | Cấu hình qua ScriptableObject |
| **CSV parsing thủ công** | Dễ lỗi, khó debug | Sử dụng protobuf hoặc MessagePack |
| **Thiếu retry logic** cho message quan trọng | Mất dữ liệu khi mạng yếu | Thêm acknowledgment & retry queue |
| **Không mã hóa WebSocket** | Dữ liệu truyền plaintext | Nâng cấp lên WSS (TLS) |
| **FindObjectOfType** trong Start() | O(n) mỗi lần gọi | Dependency Injection hoặc Service Locator |

**Hạn chế mô hình ABM:**

| Hạn chế | Mô tả | Đề xuất |
|---------|-------|---------|
| **Spawn rate tĩnh theo công thức** | Không phản ánh mô hình thủy văn phức tạp | Tích hợp dữ liệu GIS thời gian thực từ GAMA |
| **Tương tác 1:1** (Ally vs Enemy) | Đơn giản hóa quá trình pha loãng | Mô hình gradient nồng độ |
| **TreeBarrier chỉ bẫy 1 enemy** | Không phản ánh hiệu quả rừng phòng hộ | Cho phép bẫy nhiều enemy với damage giảm dần |
| **Thiếu mô hình kinh tế** trong GAMA | GAMA chỉ điều khiển spawn rate | Tích hợp mô hình chi phí - lợi nhuận |

### 14.4. Ý nghĩa trong quản lý tài nguyên nông nghiệp

**Đối với mô phỏng nông nghiệp:**
- Tích hợp GAMA cho phép mô phỏng **hệ thống phức tạp đa tác nhân** — hàng trăm thực thể tương tác đồng thời
- Dữ liệu GIS thực tế từ GAMA → mô phỏng **chính xác theo địa lý** từng vùng ĐBSCL
- Vòng phản hồi hai chiều → **mô hình thích ứng** dựa trên hành vi người dùng

**Đối với đào tạo:**
- Hiển thị trực quan hiện tượng xâm nhập mặn qua tác nhân → **dễ hiểu hơn số liệu trừu tượng**
- Cơ chế phòng thủ (máy bơm + cây rào) → dạy nông dân về **cơ sở hạ tầng thủy lợi**
- KPI real-time → phản hồi tức thì cho **ra quyết định nông nghiệp**

**Đối với nghiên cứu:**
- Kiến trúc mở, module hóa → dễ **tái sử dụng** cho các dự án ABM khác
- Giao thức WebSocket chuẩn → dễ **tích hợp với các mô hình GAMA hiện có**
- Export/Import hình học → **đồng bộ thế giới ảo** giữa GAMA và Unity

---

## 15. KẾT LUẬN

Báo cáo đã trình bày chi tiết hệ thống tích hợp GAMA–Unity trong dự án SIMPLE VU2 — một mô hình Agent-Based phục vụ quản lý tài nguyên nông nghiệp vùng ĐBSCL. Các đóng góp chính gồm:

1. **Kiến trúc 5 tầng** (Connection → Serialization → Simulation → Bridge → Presentation) phân tách rõ ràng trách nhiệm, cho phép phát triển và bảo trì độc lập từng thành phần.

2. **Giao thức truyền thông JSON-RPC qua WebSocket** với 2 chế độ (Middleware/Direct), hỗ trợ 15 loại message gửi đi và 15 loại message nhận vào, tần suất đồng bộ 0.5 giây — đáp ứng yêu cầu thời gian thực cho ứng dụng VR.

3. **Máy trạng thái kết nối 4 trạng thái** (DISCONNECTED → PENDING → CONNECTED → AUTHENTICATED) với cơ chế tự phục hồi (circuit breaker) và keepalive (ping/pong).

4. **Hệ thống 4 loại tác nhân** (Enemy/Ally/WaterPump/TreeBarrier) tương tác phức tạp, mô phỏng chân thực động lực xâm nhập mặn và ứng phó thủy lợi tại ĐBSCL.

5. **Hệ thống chuyển đổi tọa độ CRS** hai chiều giữa GAMA (integer-scaled, GIS-oriented) và Unity (float, Y-up), hỗ trợ cả import và export hình học.

6. **Vòng phản hồi hai chiều GAMA ↔ Unity** — GAMA điều chỉnh mô hình ABM dựa trên KPI gameplay, tạo mô phỏng thích ứng và chân thực.

7. **Chiến lược tối ưu hiệu năng** bao gồm bộ đếm lệch pha, nén CSV, xử lý trì hoãn thread-safe, và gửi bất đồng bộ — tổng băng thông ước tính chỉ ~2.5 KB/s.

Hệ thống chứng minh rằng tích hợp GAMA–Unity qua WebSocket là **giải pháp khả thi và hiệu quả** cho mô hình hóa Agent-Based trong quản lý tài nguyên nông nghiệp, mở ra tiềm năng ứng dụng rộng rãi trong giáo dục, nghiên cứu và hỗ trợ ra quyết định nông nghiệp bền vững.

---

## 16. TÀI LIỆU THAM KHẢO

1. Taillandier, P., Gaudou, B., Grignard, A., Huynh, Q. N., Marilleau, N., Caillou, P., Philippon, D., & Drogoul, A. (2019). Building, composing and experimenting complex spatial models with the GAMA platform. *GeoInformatica*, 23(2), 299–322.

2. Grignard, A., Taillandier, P., Gaudou, B., Vo, D. A., Huynh, N. Q., & Drogoul, A. (2013). GAMA 1.6: Advancing the art of complex agent-based modeling and simulation. In *PRIMA 2013: Principles and Practice of Multi-Agent Systems* (pp. 117–131). Springer.

3. Macal, C. M., & North, M. J. (2010). Tutorial on agent-based modelling and simulation. *Journal of Simulation*, 4(3), 151–162.

4. Bonabeau, E. (2002). Agent-based modeling: Methods and techniques for simulating human systems. *Proceedings of the National Academy of Sciences*, 99(suppl 3), 7280–7287.

5. Smajgl, A., Toan, T. Q., Nhan, D. K., Ward, J., Trung, N. H., Tri, L. Q., Tri, V. P. D., & Vu, P. T. (2015). Responding to rising sea levels in the Mekong Delta. *Nature Climate Change*, 5(2), 167–174.

6. Renaud, F. G., Le, T. T. H., Lindener, C., Guber, V. S., & Sebesvari, Z. (2015). Resilience and shifts in agro-ecosystems facing increasing sea-level rise and salinity intrusion in Ben Tre Province, Mekong Delta. *Climatic Change*, 133(1), 69–84.

7. IPCC (2021). *Climate Change 2021: The Physical Science Basis*. Contribution of Working Group I to the Sixth Assessment Report. Cambridge University Press.

8. Viện Khoa học Thủy lợi miền Nam (2020). *Báo cáo tình hình xâm nhập mặn mùa khô 2019–2020 tại Đồng bằng sông Cửu Long*.

9. Fette, I. & Melnikov, A. (2011). The WebSocket Protocol. *RFC 6455*, IETF. https://tools.ietf.org/html/rfc6455

10. Unity Technologies (2022). *XR Interaction Toolkit Documentation*. https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.5/

11. Radianti, J., Majchrzak, T. A., Fromm, J., & Wohlgenannt, I. (2020). A systematic review of immersive virtual reality applications for higher education: Design elements, lessons learned, and a research agenda. *Computers & Education*, 147, 103778.

12. Kolb, D. A. (1984). *Experiential Learning: Experience as the Source of Learning and Development*. Prentice-Hall.

---

## PHỤ LỤC

### Phụ lục A: Danh sách đầy đủ file mã nguồn tích hợp GAMA

```
Assets/GAMA_Resources/Scripts/
├── Gama Provider/
│   ├── Connection/
│   │   ├── WebSocketConnector.cs         # Lớp cơ sở trừu tượng WebSocket
│   │   ├── ConnectionManager.cs          # Singleton quản lý kết nối
│   │   ├── ConnectionWithGama.cs         # Client standalone nhẹ
│   │   └── StaticInformation.cs          # Lưu ID người chơi
│   │
│   ├── Serialization/
│   │   ├── ConnectionParameter.cs        # Tham số kết nối khởi tạo
│   │   ├── PropertiesGAMA.cs             # Định nghĩa kiểu đối tượng
│   │   ├── WorldJSONInfo.cs              # Hình học thế giới
│   │   ├── DEMData.cs                    # DEM toàn bộ
│   │   ├── DEMDataLoc.cs                 # DEM cục bộ
│   │   ├── EnemySpawnerInfo.cs           # Tốc độ spawn enemy
│   │   ├── FreshWaterSpawn.cs            # Tốc độ spawn ally
│   │   ├── SubsidenceInfo.cs             # Mực nước + sụt lún
│   │   ├── AnimationInfo.cs              # Lệnh animation
│   │   ├── TeleportAreaInfo.cs           # Vùng dịch chuyển XR
│   │   ├── WallInfo.cs                   # Tường va chạm vô hình
│   │   └── EndOfGameInfo.cs              # Tín hiệu kết thúc
│   │
│   ├── Simulation/
│   │   ├── SimulationManager.cs          # Orchestrator trung tâm (1728 LOC)
│   │   ├── SimulationManagerSolo.cs      # Chế độ 1 người
│   │   ├── SimulationManagerMulti.cs     # Chế độ nhiều người
│   │   ├── SimulationManagerInteraction.cs # Hook ảo
│   │   └── CoordinateConverter.cs        # Chuyển đổi tọa độ
│   │
│   └── Utils/
│       └── PolygonGenerator.cs           # Tạo mesh 3D từ đa giác
│
├── Utils/
│   ├── GAMAGeometryLoader.cs             # Import hình học
│   └── GAMAGeometryExport.cs             # Export hình học
│
├── RUNTIME/CORE/
│   ├── Enemy.cs                          # Tác nhân nước mặn
│   ├── EnemyController.cs                # Điều khiển di chuyển enemy
│   ├── EnemySpawner.cs                   # Nguồn sinh enemy
│   └── Barrack.cs                        # Máy bơm nước (Water Pump)
│
└── ...

Assets/Scripts/VU2/
├── Managers/
│   └── GAMABridgeLevel1.cs               # Cầu nối Level 1
├── TreeBarrier.cs                        # Hàng rào cây xanh
└── ...

Assets/Editor/GAMAMenu/
├── GAMAMenu.cs                           # Unity Editor menu
├── GAMAGeometryLoaderUI.cs               # UI import hình học
└── GAMAGeometryExportUI.cs               # UI export hình học
```

### Phụ lục B: Bảng tham số cấu hình PlayerPrefs

| Key | Giá trị | Mặc định | Mô tả |
|-----|---------|----------|-------|
| `"MIDDLEWARE"` | `"Y"` / `"N"` | `"Y"` | Sử dụng middleware proxy |
| `"IP"` | IP address | `"192.168.88.148"` | Địa chỉ server |
| `"PORT"` | Port number | `"8080"` (MW) / `"1000"` (Direct) | Port kết nối |

### Phụ lục C: Bảng Interface hệ thống tác nhân

```csharp
// Interface cho thực thể có thể nhận sát thương
public interface IDamageable
{
    void TakeDamage(int damage);
    void Die();
    bool IsDead();
}

// Interface cho thực thể có thể gây sát thương
public interface IDamage
{
    void DealDamage(IDamageable target);
    bool HasValidTarget(GameObject target);
}

// Interface cho thực thể có thể sinh thực thể khác
public interface ISpawner
{
    string SpawnName { get; }
    float SpawnRate { get; set; }
    void Spawn();
}
```

### Phụ lục D: Thông số kỹ thuật nền tảng

| Thành phần | Phiên bản/Thông số |
|-----------|-------------------|
| Unity Engine | 2022.3.5f1 LTS |
| XR Interaction Toolkit | 2.5.2 |
| Universal Render Pipeline | 14.0.8 |
| GAMA Platform | 1.9+ (qua WebSocket) |
| WebSocket Library | websocket-sharp.dll |
| JSON Serialization | Newtonsoft.Json (Json.NET) |
| Nền tảng VR | Meta Quest (Oculus) |
| Giao thức kết nối | WebSocket (ws://) |
| Tần suất đồng bộ | 2 Hz (0.5s) |
| Precision factor | 1000 |
| Ngôn ngữ lập trình | C# (.NET Standard 2.1) |

---

*Báo cáo được tổng hợp từ phân tích mã nguồn hệ thống GAMA Provider, dữ liệu runtime, tài liệu kỹ thuật nội bộ dự án SIMPLE VU2 (GAMA_PROVIDER_GUIDE.md, GAMA_Protocol_Guide.md), và các báo cáo khoa học liên quan. Ngày lập: 14 tháng 4 năm 2026.*
