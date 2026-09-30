# GAMA Provider — Hướng dẫn chi tiết / Detailed Guide

> **EN:** Complete reference for the 22 C# files that form the Unity ↔ GAMA bridge.  
> **VI:** Tài liệu tham khảo đầy đủ cho 22 file C# tạo nên cầu nối Unity ↔ GAMA.

---

## Mục lục / Table of Contents

1. [Kiến trúc tổng quan / Architecture Overview](#1-kiến-trúc-tổng-quan--architecture-overview)
2. [Luồng dữ liệu / Data Flow](#2-luồng-dữ-liệu--data-flow)
3. [Máy trạng thái / State Machine](#3-máy-trạng-thái--state-machine)
4. [Connection Layer (4 files)](#4-connection-layer-4-files)
5. [Serialization Layer (15 files)](#5-serialization-layer-15-files)
6. [Simulation Layer (5 files)](#6-simulation-layer-5-files)
7. [Utils & Scenes (3 files)](#7-utils--scenes-3-files)
8. [Bảng message Unity → GAMA](#8-bảng-message-unity--gama)
9. [Bảng message GAMA → Unity](#9-bảng-message-gama--unity)
10. [Vấn đề cần lưu ý / Known Issues](#10-vấn-đề-cần-lưu-ý--known-issues)

---

## 1. Kiến trúc tổng quan / Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      Unity Runtime                          │
│                                                             │
│  ┌──────────────┐    ┌───────────────────┐    ┌──────────┐  │
│  │ MenuController│───▶│ ConnectionManager │◀──▶│  GAMA /  │  │
│  │ (Startup UI)  │    │ (WebSocket Client)│    │Middleware│  │
│  └──────────────┘    └────────┬──────────┘    └──────────┘  │
│                               │                              │
│                    events ▼ ▲ SendExecutableAsk              │
│                               │                              │
│                    ┌──────────┴──────────┐                   │
│                    │ SimulationManager   │                   │
│                    │ (Main Orchestrator) │                   │
│                    └──┬────┬────┬───┬───┘                   │
│                       │    │    │   │                        │
│              ┌────────┘    │    │   └─────────┐              │
│              ▼             ▼    ▼             ▼              │
│  ┌──────────────┐ ┌────────┐ ┌──────┐ ┌──────────────┐     │
│  │Coordinate    │ │Polygon │ │GameUI│ │LevelManager  │     │
│  │Converter     │ │Generator│ │      │ │(Spawns/Timer)│     │
│  └──────────────┘ └────────┘ └──────┘ └──────────────┘     │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │         Serialization DTOs (15 classes)               │   │
│  │  ConnectionParameter, WorldJSONInfo, DEMData, ...     │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

**EN:** The system uses a WebSocket connection (via `WebSocketSharp`) to communicate with a GAMA simulation server, optionally through a middleware proxy. `SimulationManager` is the central orchestrator that routes inbound messages to handlers and sends outbound data periodically.

**VI:** Hệ thống dùng kết nối WebSocket (qua `WebSocketSharp`) để giao tiếp với server mô phỏng GAMA, tùy chọn thông qua middleware proxy. `SimulationManager` là bộ điều phối trung tâm định tuyến message đến các handler và gửi dữ liệu ra ngoài theo chu kỳ.

---

## 2. Luồng dữ liệu / Data Flow

### 2.1 Khởi tạo / Initialization Sequence

```
MenuController          ConnectionManager       SimulationManager         GAMA
     │                        │                        │                    │
     │──── StartBtn() ───────▶│                        │                    │
     │                        │── TryConnection ──────▶│                    │
     │                        │◀──── WebSocket Open ───│                    │
     │                        │── "connection" ────────────────────────────▶│
     │                        │◀──── "json_state" (connected) ─────────────│
     │                        │   UpdateState(CONNECTED)                    │
     │                        │◀──── "json_state" (authenticated) ─────────│
     │                        │   UpdateState(AUTHENTICATED)                │
     │                        │──── event ────────────▶│                    │
     │                        │                        │ LOADING_DATA       │
     │                        │                        │── "send_init_data"▶│
     │                        │                        │◀── "precision" ────│
     │                        │                        │◀── "properties" ───│
     │                        │                        │◀── "pointsLoc" ────│
     │                        │                        │   GAME state       │
     │                        │                        │── "player_ready"──▶│
     │                        │                        │◀── "startGame" ────│
```

### 2.2 Vòng lặp gameplay / Gameplay Loop

```
Every ~0.5s (TimeSendPosition):
  ┌─ Timer 1 (offset 0.0s):   sendEnemies()      → "update_salty_water"
  ├─ Timer 2 (offset 0.25s):  sendFreshWater()    → "update_fresh_water"
  └─ Timer 3 (offset 0.17s):  updatePlayerPos()   → "update_player_pos"

GAMA responds with:
  ← "enemyspawners"  → updateInfoSpawnRateEnemy()
  ← "pumpers"        → updateInfoSpawnRatePumper()
  ← "subsidences"    → updateSubsidence()
  ← "rows" / "indexX" → manageUpdateTerrain() / manageSetValueTerrain()
```

---

## 3. Máy trạng thái / State Machine

### 3.1 GameState (SimulationManager)

```
  MENU ──connect──▶ WAITING ──auth──▶ LOADING_DATA ──data ready──▶ GAME ──▶ END
    │                                                                │
    └◀──────────────────── CRASH ◀───────────────────────────────────┘
```

| State | EN Description | VI Mô tả |
|-------|---------------|-----------|
| `MENU` | No connection. Entry point. | Chưa kết nối. Điểm khởi đầu. |
| `WAITING` | Socket open, waiting for middleware auth. | Socket đã mở, chờ xác thực middleware. |
| `LOADING_DATA` | Authenticated. Polling `send_init_data` every 0.5s. | Đã xác thực. Gọi `send_init_data` mỗi 0.5s. |
| `GAME` | All initial data received. Periodic sync active. | Đã nhận đủ dữ liệu. Đồng bộ định kỳ hoạt động. |
| `END` | Game finished normally. | Game kết thúc bình thường. |
| `CRASH` | Unrecoverable error. | Lỗi không khôi phục. |

### 3.2 ConnectionState (ConnectionManager)

```
  DISCONNECTED ──connect──▶ PENDING ──open──▶ CONNECTED ──auth──▶ AUTHENTICATED
       ▲                                                              │
       └──────────────── close / error ◀──────────────────────────────┘
```

| State | EN | VI |
|-------|----|----|
| `DISCONNECTED` | No socket. Auto-triggers `TryConnectionToServer()`. | Không có socket. Tự động gọi `TryConnectionToServer()`. |
| `PENDING` | `Socket.Connect()` called, waiting for handshake. | Đã gọi `Socket.Connect()`, chờ bắt tay. |
| `CONNECTED` | WebSocket open, waiting for middleware to confirm game slot. | WebSocket mở, chờ middleware xác nhận slot game. |
| `AUTHENTICATED` | Ready to exchange simulation data. Sends `new_connection`. | Sẵn sàng trao đổi dữ liệu. Gửi `new_connection`. |

---

## 4. Connection Layer (4 files)

### 4.1 `WebSocketConnector.cs`

> **Path:** `Connection/WebSocketConnector.cs`

**EN:** Abstract base class for all WebSocket communication. Manages socket creation, IP/port resolution from PlayerPrefs or hardcoded defaults, and delegates message handling to subclasses.

**VI:** Lớp trừu tượng cơ sở cho mọi giao tiếp WebSocket. Quản lý tạo socket, phân giải IP/port từ PlayerPrefs hoặc giá trị mặc định cố định, và ủy thác xử lý message cho lớp con.

| Field | Type | EN | VI |
|-------|------|----|----|
| `host` | `string` | Server IP address | Địa chỉ IP server |
| `port` | `string` | Server port | Cổng server |
| `UseMiddleware` | `bool` | Route through middleware proxy | Định tuyến qua middleware proxy |
| `UseHeartbeat` | `bool` | Enable ping/pong keepalive (middleware only) | Bật ping/pong keepalive (chỉ middleware) |
| `DesktopMode` | `bool` | Use localhost for PC testing | Dùng localhost cho test PC |
| `fixedProperties` | `bool` | Ignore PlayerPrefs, use DefaultIP/DefaultPort | Bỏ qua PlayerPrefs, dùng DefaultIP/DefaultPort |
| `numErrorsBeforeDeconnection` | `int` | Max consecutive send errors before force-disconnect | Số lỗi gửi liên tiếp tối đa trước khi buộc ngắt |

**Abstract methods (lớp con phải override):**

| Method | EN | VI |
|--------|----|----|
| `HandleConnectionOpen()` | Called when WebSocket opens | Gọi khi WebSocket mở |
| `HandleReceivedMessage()` | Called when a message arrives | Gọi khi nhận message |
| `HandleConnectionClosed()` | Called when connection closes | Gọi khi đóng kết nối |

**Key method:**

```csharp
// EN: Async send with liveness check. Silently drops if socket is dead.
// VI: Gửi bất đồng bộ với kiểm tra sống. Bỏ qua nếu socket chết.
SendMessageToServer(string message, Action<bool> successCallback)
```

---

### 4.2 `ConnectionManager.cs`

> **Path:** `Connection/ConnectionManager.cs`  
> **Extends:** `WebSocketConnector`

**EN:** Singleton high-level connection manager. Handles the full lifecycle: connect → authenticate → route inbound messages → send outbound RPC calls to GAMA.

**VI:** Singleton quản lý kết nối cấp cao. Xử lý toàn bộ vòng đời: kết nối → xác thực → định tuyến message đến → gửi RPC ra GAMA.

| Event | Params | EN | VI |
|-------|--------|----|----|
| `OnConnectionStateChanged` | `ConnectionState` | Fired on state transition | Phát khi chuyển trạng thái |
| `OnServerMessageReceived` | `(string key, string json)` | Fired when simulation output arrives | Phát khi nhận output mô phỏng |
| `OnConnectionStateReceived` | `JObject` | Fired on `json_state` message | Phát khi nhận message `json_state` |
| `OnConnectionAttempted` | `bool` | Fired after connect attempt | Phát sau lần thử kết nối |

**Inbound message protocol (two modes):**

| Mode | Message Type | Handling |
|------|-------------|----------|
| Middleware | `"ping"` | Reply `"pong"` (keepalive) |
| Middleware | `"json_state"` | Update auth state → fire `OnConnectionStateChanged` |
| Middleware | `"json_output"` | Extract `contents` → find first JSON key → fire `OnServerMessageReceived` |
| Direct | `"SimulationOutput"` | Split by `"|||"` → fire `OnServerMessageReceived` for each |

**Key outbound methods:**

```csharp
// EN: Send an RPC to GAMA agent. This is the PRIMARY outbound method.
//     JSON format: { type: "ask", action: "<name>", args: {…}, agent: "simulation[0].unity_linker[0]" }
// VI: Gửi RPC đến agent GAMA. Đây là phương thức gửi CHÍNH.
SendExecutableAsk(string action, Dictionary<string,string> arguments)

// EN: Send raw GAML expression (less commonly used).
// VI: Gửi biểu thức GAML thô (ít dùng hơn).
SendExecutableExpression(string expression)
```

---

### 4.3 `StaticInformation.cs`

> **Path:** `Connection/StaticInformation.cs`

**EN:** Static utility that generates a unique player ID from the machine's local IP (last octet). E.g. IP `192.168.1.42` → ID `"Player_42"`. Cached after first call.

**VI:** Tiện ích static tạo ID người chơi duy nhất từ IP cục bộ (octet cuối). VD: IP `192.168.1.42` → ID `"Player_42"`. Cache sau lần gọi đầu.

| Member | Type | EN | VI |
|--------|------|----|----|
| `endOfGame` | `string` | Game result text from GAMA | Văn bản kết quả game từ GAMA |
| `getId()` | `string` | Returns cached player ID | Trả về player ID đã cache |

---

### 4.4 `ConnectionWithGama.cs`

> **Path:** `Connection/ConnectionWithGama.cs`

**EN:** Lightweight standalone WebSocket client for direct GAMA communication (no middleware, no lifecycle management). Provides `SendExecutableAsk()` with the same JSON protocol. **Largely superseded by `ConnectionManager`** — kept for simple test scenarios.

**VI:** Client WebSocket nhẹ độc lập cho giao tiếp GAMA trực tiếp (không middleware, không quản lý vòng đời). Cung cấp `SendExecutableAsk()` cùng giao thức JSON. **Phần lớn đã bị thay thế bởi `ConnectionManager`** — giữ lại cho test đơn giản.

---

## 5. Serialization Layer (15 files)

> **EN:** All DTOs use `[System.Serializable]` + `JsonUtility.FromJson<T>()` for deserialization. No custom converters needed.  
> **VI:** Tất cả DTO dùng `[System.Serializable]` + `JsonUtility.FromJson<T>()` để deserialize. Không cần custom converter.

### 5.1 `ConnectionParameter.cs` — Tham số khởi tạo / Init Parameters

> **Inbound key:** `"precision"`

| Field | Type | EN | VI |
|-------|------|----|----|
| `precision` | `int` | Coordinate multiplier (all GAMA coords = real × precision) | Hệ số nhân tọa độ |
| `position` | `List<int>` | Player spawn position `[x, y]` | Vị trí spawn `[x, y]` |
| `world` | `List<int>` | World bounds `[width, height]` | Giới hạn thế giới `[rộng, cao]` |
| `hotspots` | `List<string>` | Named POIs (optional) | Điểm quan tâm (tùy chọn) |
| `minPlayerUpdateDuration` | `int` | Min interval between position updates (precision-scaled ms) | Chu kỳ tối thiểu gửi vị trí (ms × precision) |

**EN:** Received once at start. `precision` is critical — all subsequent integer coordinates are divided by this value.  
**VI:** Nhận một lần khi khởi động. `precision` rất quan trọng — mọi tọa độ nguyên sau đó đều chia cho giá trị này.

---

### 5.2 `PropertiesGAMA.cs` — Thuộc tính đối tượng / Object Properties

> **Inbound key:** `"properties"` (wrapped in `AllProperties.properties[]`)

| Field | Type | EN | VI |
|-------|------|----|----|
| `id` | `string` | Unique type ID | ID kiểu duy nhất |
| `hasCollider` | `bool` | Add Collider component | Thêm Collider |
| `tag` | `string` | Unity tag to assign | Tag Unity gán cho |
| `isInteractable` | `bool` | Enable XR interaction | Bật tương tác XR |
| `isGrabable` | `bool` | XRGrabInteractable vs XRSimpleInteractable | Grab vs Simple |
| `constraints` | `List<bool>` | Rigidbody freeze `[posX,Y,Z, rotX,Y,Z]` | Freeze Rigidbody |
| `hasPrefab` | `bool` | Instantiate from Resources prefab | Tạo từ prefab Resources |
| `prefab` | `string` | Resources path (e.g. `"Prefabs/Tree"`) | Đường dẫn Resources |
| `size` | `int` | Scale factor (÷ precision) | Hệ số scale |
| `yOffset` | `int` | Y offset after placement (÷ precision) | Offset Y sau khi đặt |
| `rotationCoeff` | `int` | Rotation multiplier (÷ precision) | Hệ số xoay |
| `visible` | `bool` | Show/hide mesh renderers | Hiện/ẩn renderer |
| `height` | `int` | Extrusion height for 3D polygons (÷ precision) | Chiều cao đùn polygon 3D |
| `is3D` | `bool` | 3D extruded (true) or flat 2D (false) | Đùn 3D hay phẳng 2D |
| `material` | `string` | Material resource path | Đường dẫn material |
| `red/green/blue/alpha` | `int` | RGBA color components | Thành phần màu RGBA |
| `toFollow` | `bool` | Track position → send to GAMA | Theo dõi vị trí → gửi GAMA |

---

### 5.3 `WorldJSONInfo.cs` — Dữ liệu thế giới ban đầu / Initial World Data

> **Inbound key:** `"pointsLoc"` (parsed only once)

| Field | Type | EN | VI |
|-------|------|----|----|
| `position` | `List<int>` | Flat coordinate list `[x1,y1, x2,y2, …]` | Danh sách tọa độ phẳng |
| `names` | `List<string>` | Object names | Tên đối tượng |
| `keepNames` | `List<string>` | Names preserved across updates | Tên giữ qua các lần cập nhật |
| `propertyID` | `List<string>` | Maps to `PropertiesGAMA.id` | Ánh xạ tới `PropertiesGAMA.id` |
| `pointsLoc` | `List<GAMAPoint>` | Agent locations in GAMA CRS | Vị trí agent trong CRS GAMA |
| `pointsGeom` | `List<GAMAPoint>` | Polygon vertices for geometry | Đỉnh polygon cho hình học |
| `offsetYGeom` | `List<int>` | Y offsets per polygon (÷ precision) | Offset Y mỗi polygon |
| `ranking` | `List<int>` | Player scores (multiplayer) | Điểm người chơi (multiplayer) |
| `players` | `List<string>` | Player IDs (multiplayer) | ID người chơi (multiplayer) |
| `isInit` | `bool` | True = initial snapshot | True = bản chụp ban đầu |

**Helper:** `GAMAPoint` — `{ List<int> c }` — point in GAMA CRS `[x,y]` or `[x,y,z]`.

---

### 5.4 `DEMData.cs` — DEM đầy đủ / Full DEM Heightmap

> **Inbound key:** `"rows"`

| Field | Type | EN | VI |
|-------|------|----|----|
| `rows` | `List<Row>` | Height grid (each Row has `List<int> h`) | Lưới độ cao |
| `id` | `string` | Target Terrain name | Tên Terrain đích |
| `valMax` | `int` | Max height for normalization | Độ cao max để chuẩn hóa |
| `sizeX` | `int` | Terrain width (world units) | Chiều rộng Terrain |
| `sizeY` | `int` | Terrain depth (world units) | Chiều sâu Terrain |

**EN:** Replaces the **entire** heightmap. Terrain is repositioned to `(0, 0, -sizeY)`.  
**VI:** Thay thế **toàn bộ** heightmap. Terrain được đặt lại vị trí `(0, 0, -sizeY)`.

---

### 5.5 `DEMDataLoc.cs` — DEM từng phần / Partial DEM Patch

> **Inbound key:** `"indexX"`

| Field | Type | EN | VI |
|-------|------|----|----|
| `rows` | `List<Row>` | Height patch data | Dữ liệu miếng vá độ cao |
| `id` | `string` | Target Terrain name | Tên Terrain đích |
| `indexX` | `int` | Patch start X in heightmap grid | X bắt đầu trong lưới heightmap |
| `indexY` | `int` | Patch start Y in heightmap grid | Y bắt đầu trong lưới heightmap |
| `valMax` | `int` | May trigger terrain rescale if exceeds current max | Có thể co giãn terrain nếu vượt max |

---

### 5.6 `WallInfo.cs` — Tường vô hình / Invisible Walls

> **Inbound key:** `"wallId"`  
> **⚠ NOTE:** `manageWalls()` is currently **commented out** in SimulationManager.

| Field | Type | EN | VI |
|-------|------|----|----|
| `wallId` | `string` | Wall identifier | ID tường |
| `offsetYGeom` | `List<int>` | Y offsets (÷ precision) | Offset Y |
| `height` | `int` | Extrusion height (÷ precision) | Chiều cao đùn |
| `pointsGeom` | `List<GAMAPoint>` | Polygon vertices | Đỉnh polygon |

---

### 5.7 `TeleoportAreaInfo.cs` — Vùng teleport / Teleportation Areas

> **Inbound key:** `"teleportId"`

| Field | Type | EN | VI |
|-------|------|----|----|
| `teleportId` | `string` | Teleport area identifier | ID vùng teleport |
| `offsetYGeom` | `List<int>` | Y offsets (÷ precision) | Offset Y |
| `height` | `int` | Extrusion height (÷ precision) | Chiều cao đùn |
| `pointsGeom` | `List<GAMAPoint>` | Polygon vertices | Đỉnh polygon |

**EN:** Creates `MeshCollider` objects attached to an XR `TeleportationArea` for VR locomotion.  
**VI:** Tạo `MeshCollider` gắn vào XR `TeleportationArea` cho di chuyển VR.

---

### 5.8 `AnimationInfo.cs` — Lệnh animation / Animation Commands

> **Inbound key:** `"triggers"`

| Field | Type | EN | VI |
|-------|------|----|----|
| `names` | `List<string>` | Target geometry names (keys in `geometryMap`) | Tên geometry đích |
| `triggers` | `List<string>` | Animator trigger names to fire | Tên trigger Animator |
| `parameters` | `List<ParameterVal>` | Animator params to set before triggers | Tham số Animator set trước trigger |

**`ParameterVal`:**

| Field | Type | EN | VI |
|-------|------|----|----|
| `key` | `string` | Animator parameter name | Tên tham số Animator |
| `type` | `string` | `"int"` / `"float"` / `"bool"` | Kiểu dữ liệu |
| `intVal` / `floatVal` / `boolVal` | varied | The value to set | Giá trị cần set |

---

### 5.9 `EnableMoveInfo.cs` — Bật/tắt di chuyển / Movement Toggle

> **Inbound key:** `"enableMove"`

| Field | Type | EN | VI |
|-------|------|----|----|
| `enableMove` | `bool` | `true` = player can move; `false` = frozen | `true` = di chuyển; `false` = đóng băng |

---

### 5.10 `StartGameParameters.cs` — Thời gian game / Game Timing

> **Inbound key:** `"startGame"`

| Field | Type | EN | VI |
|-------|------|----|----|
| `time_prep` | `int` | Preparation phase duration | Thời lượng giai đoạn chuẩn bị |
| `time_def` | `int` | Defense phase duration | Thời lượng giai đoạn phòng thủ |

---

### 5.11 `SubsidenceInfo.cs` — Sụt lún / Subsidence Data

> **Inbound key:** `"subsidences"`

| Field | Type | EN | VI |
|-------|------|----|----|
| `subsidences` | `List<string>` | Zone identifiers | ID vùng sụt lún |
| `waterLocal` | `int` | Local water level (÷ precision) | Mức nước cục bộ |
| `waterGlobal` | `int` | Global water level (÷ precision) | Mức nước toàn cục |
| `subsi_score` | `float` | Subsidence severity score | Điểm mức độ sụt lún |

---

### 5.12 `EnemySpawnerInfo.cs` — Tốc độ spawn enemy / Enemy Spawn Rates

> **Inbound key:** `"enemyspawners"`

| Field | Type | EN | VI |
|-------|------|----|----|
| `enemyspawners` | `List<string>` | Spawner InstanceID strings | Chuỗi InstanceID spawner |
| `spawnrates` | `List<int>` | New spawn rates (÷ precision) | Tốc độ spawn mới |

**EN:** Parallel arrays — `enemyspawners[i]` maps to `spawnrates[i]`.  
**VI:** Mảng song song — `enemyspawners[i]` ánh xạ tới `spawnrates[i]`.

---

### 5.13 `FreshWaterSpawn.cs` — Tốc độ spawn nước / Water Pump Rates

> **Inbound key:** `"pumpers"`

| Field | Type | EN | VI |
|-------|------|----|----|
| `pumpers` | `List<string>` | Pumper InstanceID strings | Chuỗi InstanceID pumper |
| `spawnrates` | `List<int>` | New spawn rates (÷ precision, then halved in code) | Tốc độ (÷ precision, rồi chia 2) |

---

### 5.14 `EndOfGameInfo.cs` — Kết thúc game / End of Game

> **Inbound key:** `"endOfGame"`

| Field | Type | EN | VI |
|-------|------|----|----|
| `endOfGame` | `string` | Human-readable result text | Văn bản kết quả game |

**EN:** Stored in `StaticInformation.endOfGame` → display on `EndofGameController` screen → load scene `"End of Game Menu"`.  
**VI:** Lưu vào `StaticInformation.endOfGame` → hiển thị trên `EndofGameController` → load scene `"End of Game Menu"`.

---

### 5.15 `UnityGeometry.cs` — Xuất hình học / Geometry Export

**EN:** Serializable representation of Unity mesh geometry **exported TO GAMA** (reverse direction). Recursively traverses child objects, extracts all triangle vertices, converts to GAMA CRS.

**VI:** Biểu diễn serializable hình học mesh Unity **xuất SANG GAMA** (chiều ngược). Duyệt đệ quy các con, trích xuất đỉnh tam giác, chuyển sang CRS GAMA.

| Class | Field | EN | VI |
|-------|-------|----|----|
| `UnityGeometry` | `points` | All triangle vertices in GAMA CRS | Đỉnh tam giác trong CRS GAMA |
| `UnityGeometry` | `heights` | Bounding-box height per sub-mesh | Chiều cao bounding-box |
| `UnityGeometry` | `names` | Object name per triangle | Tên object mỗi tam giác |
| `UnityPoint` | `c` | Coordinate `[x, y]` or `[x, y, z]` | Tọa độ `[x, y]` hoặc `[x, y, z]` |

---

## 6. Simulation Layer (5 files)

### 6.1 `SimulationManager.cs` — Bộ điều phối chính / Main Orchestrator

> **Path:** `Simulation/SimulationManager.cs`

**EN:** The central MonoBehaviour (~1000 lines). Subscribes to `ConnectionManager` events, routes all inbound messages, and manages three periodic outbound timers plus one-shot registrations.

**VI:** MonoBehaviour trung tâm (~1000 dòng). Đăng ký event `ConnectionManager`, định tuyến mọi message đến, và quản lý ba timer gửi định kỳ cùng các lần đăng ký một lần.

#### Lifecycle methods / Phương thức vòng đời

| Method | Loop | EN | VI |
|--------|------|----|-----|
| `Awake()` | once | Singleton, find locomotion, init dictionaries | Singleton, tìm locomotion, khởi tạo dictionary |
| `Start()` | once | Init geometry map, stagger timers | Khởi tạo geometry map, lệch pha timer |
| `OnEnable()` | once | Subscribe to ConnectionManager events | Đăng ký event ConnectionManager |
| `OnDisable()` | once | Unsubscribe from events | Hủy đăng ký event |
| `FixedUpdate()` | physics | Process deferred flags (ground, geometry, terrain, teleport, wall, animation, spawns, subsidence) | Xử lý cờ trì hoãn |
| `Update()` | frame | Input polling, reconnect timer, 3 periodic send timers, ready/start checks | Input, reconnect, 3 timer gửi, kiểm tra ready/start |

#### Outbound methods / Phương thức gửi ra

| Method | Action Name | Frequency | Payload |
|--------|------------|-----------|---------|
| `updatePlayerPos()` | `update_player_pos` | Every `TimeSendPosition` | `idP, x, y, o, remaining_time, dtree, fwater, score` |
| `sendEnemies()` | `update_salty_water` | Every `TimeSendPosition` (offset) | `idP, swsStr, xsStr, ysStr` (CSV of Enemy objects) |
| `sendFreshWater()` | `update_fresh_water` | Every `TimeSendPosition` (offset) | `idP, fwsStr, xsStr, ysStr` (CSV of Ally objects) |
| `createEnemySpawner()` | `create_enemy_spawners` | Once | `idP, idESStr, xsStr, ysStr` |
| `sendTrees()` | `create_trees` | Once | `idP, idTsStr, xsStr, ysStr` |
| `createMovePumper()` | `move_create_pumper` | Per pumper | `idP, idwp, x, y` |
| `SendEndMessageToGAMA()` | `player_finish_game` | Once | `idP` |
| `sendReadyToGAMA()` | `player_ready` | Once | `idP` |
| `ChangeState()` | `change_state` | On demand | `idP, new_state` |
| `RestartGame()` | `restart` | On demand | `id` |
| `TryReconnect()` | `ping_GAMA` | On demand | `id` |

#### Inbound handler / Bộ xử lý message đến

| JSON Key | DTO Class | Handler Method | EN | VI |
|----------|-----------|---------------|----|----|
| `"precision"` | `ConnectionParameter` | (inline) | Init converter + ground | Khởi tạo converter + ground |
| `"properties"` | `AllProperties` | (inline) | Build propertyMap | Xây propertyMap |
| `"pointsLoc"` | `WorldJSONInfo` | (inline) | Store world data (once) | Lưu dữ liệu thế giới (1 lần) |
| `"rows"` | `DEMData` | `manageUpdateTerrain()` | Full terrain replacement | Thay toàn bộ terrain |
| `"indexX"` | `DEMDataLoc` | `manageSetValueTerrain()` | Partial terrain patch | Vá terrain từng phần |
| `"wallId"` | `WallInfo` | `manageWalls()` | ⚠ Disabled | ⚠ Đã vô hiệu |
| `"teleportId"` | `TeleoportAreaInfo` | `manageTeleportationArea()` | Build XR teleport areas | Xây vùng teleport XR |
| `"enableMove"` | `EnableMoveInfo` | `playerMovement()` | Toggle locomotion | Bật/tắt di chuyển |
| `"triggers"` | `AnimationInfo` | `updateAnimation()` | Set Animator params + triggers | Set Animator + trigger |
| `"subsidences"` | `SubsidenceInfo` | `updateSubsidence()` | Update water levels | Cập nhật mức nước |
| `"enemyspawners"` | `EnemySpawnerInfo` | `updateInfoSpawnRateEnemy()` | Adjust spawn rates | Điều chỉnh tốc độ spawn |
| `"pumpers"` | `FreshWaterSpawn` | `updateInfoSpawnRatePumper()` | Adjust pump rates | Điều chỉnh tốc độ bơm |
| `"endOfGame"` | `EndOfGameInfo` | (inline) | Load end screen | Load màn hình kết thúc |
| `"readyToStart"` | — | (flag) | Enable start button | Bật nút start |
| `"startGame"` | `StartGameParameters` | `startGameWithTime()` | Set wave timers | Đặt timer wave |

#### Virtual hooks / Hook ảo cho lớp con

| Method | EN | VI |
|--------|----|-----|
| `OtherUpdate()` | Per-frame custom logic | Logic tùy chỉnh mỗi frame |
| `TriggerMainButton()` | Main button action | Hành động nút chính |
| `HoverEnterInteraction()` | XR ray enters object | Tia XR vào đối tượng |
| `HoverExitInteraction()` | XR ray leaves object | Tia XR rời đối tượng |
| `SelectInteraction()` | XR select/grab | XR chọn/grab |
| `AdditionalInitAfterGeomLoading()` | Post-geometry init | Khởi tạo sau tải geometry |
| `ManageOtherMessages()` | Unknown message keys | Khóa message không xác định |

---

### 6.2 `CoordinateConverter.cs` — Chuyển đổi tọa độ / Coordinate Conversion

> **Path:** `Simulation/CoordinateConverter.cs`

**EN:** Bidirectional converter between GAMA CRS (integer, precision-scaled, Y-down) and Unity world space (float, meters, Z-forward). **Critical note:** the Y coefficient is negated in the constructor to flip GAMA's downward Y into Unity's forward Z.

**VI:** Bộ chuyển đổi hai chiều giữa CRS GAMA (số nguyên, scale precision, Y hướng xuống) và không gian Unity (float, mét, Z hướng trước). **Lưu ý quan trọng:** hệ số Y bị đảo dấu trong constructor để lật Y hướng xuống của GAMA thành Z hướng trước của Unity.

| Method | Direction | EN | VI |
|--------|----------|----|----|
| `fromGAMACRS2D(x, y)` | GAMA → Unity `Vector2` | 2D conversion | Chuyển 2D |
| `fromGAMACRS(x, y, z)` | GAMA → Unity `Vector3` | 3D: GAMA(x,y,z) → Unity(x, z_gama, y_gama) | 3D với ánh xạ trục |
| `toGAMACRS(pos)` | Unity → GAMA `[x, y]` | 2D inverse | Nghịch đảo 2D |
| `toGAMACRS3D(pos)` | Unity → GAMA `[x, y, z]` | 3D inverse | Nghịch đảo 3D |

**Conversion formula / Công thức:**

```
Unity.x = (GamaCRSCoefX × gamaX) / precision + OffsetX
Unity.z = (GamaCRSCoefY × gamaY) / precision + OffsetY    // CoefY is negated
Unity.y = (GamaCRSCoefZ × gamaZ) / precision + OffsetZ
```

---

### 6.3 `SimulationManagerSolo.cs` — Chế độ một người / Single Player

> **Path:** `Simulation/SimulationManagerSolo.cs`  
> **Extends:** `SimulationManager`

| Override | EN | VI |
|----------|----|-----|
| `TriggerMainButton()` | Toggle day/night (all lights intensity 0 ↔ 1) | Chuyển đổi ngày/đêm |
| `AdditionalInitAfterGeomLoading()` | Highlight hotspot objects in red | Tô đỏ đối tượng hotspot |
| `HoverEnterInteraction()` | Blue highlight on selectable/car/moto | Tô xanh khi hover |
| `HoverExitInteraction()` | Restore: red if selected, gray/white otherwise | Khôi phục màu |

---

### 6.4 `SimulationManagerMulti.cs` — Chế độ nhiều người / Multiplayer Stub

> **Path:** `Simulation/SimulationManagerMulti.cs`  
> **Extends:** `SimulationManager`

**EN:** Empty stub. Intended for multiplayer-specific overrides (player list sync, shared resources).  
**VI:** Stub trống. Dự kiến cho override riêng multiplayer (đồng bộ danh sách, tài nguyên chung).

---

### 6.5 `SimulationManagerInteraction.cs` — Mẫu tương tác / Interaction Template

> **Path:** `Simulation/SimulationManagerInteraction.cs`  
> **Extends:** `SimulationManager`

**EN:** Template with empty overrides for all virtual hooks. Starting point for new game modes.  
**VI:** Mẫu với override rỗng cho tất cả hook ảo. Điểm khởi đầu cho chế độ game mới.

---

## 7. Utils & Scenes (3 files)

### 7.1 `PolygonGenerator.cs` — Tạo mesh polygon / Polygon Mesh Generator

> **Path:** `Utils/PolygonGenerator.cs`

**EN:** Singleton utility that converts GAMA integer polygon vertices into extruded 3D Unity meshes via the `PolyExtruder` component. Used for teleportation areas and walls.

**VI:** Tiện ích singleton chuyển đổi đỉnh polygon số nguyên GAMA thành mesh 3D đùn Unity qua component `PolyExtruder`. Dùng cho vùng teleport và tường.

| Method | EN | VI |
|--------|----|-----|
| `GeneratePolygons(…)` | Decode GAMA points → Unity 2D → determine color/material → extrude | Giải mã điểm GAMA → 2D Unity → xác định màu/material → đùn |
| `surroundMesh` / `bottomMesh` / `topMesh` | Cached face meshes for MeshCollider attachment | Mesh mặt cache cho MeshCollider |

---

### 7.2 `DebugOverlay.cs` (`DebugManager`)

> **Path:** `Utils/DebugOverlay.cs`

**EN:** On-screen debug log. Captures `Debug.Log` messages via `Application.logMessageReceivedThreaded` and displays the last N lines on a TextMeshPro element. Essential for VR debugging where the console is not visible.

**VI:** Log debug trên màn hình. Bắt message `Debug.Log` qua `Application.logMessageReceivedThreaded` và hiển thị N dòng cuối trên TextMeshPro. Thiết yếu cho debug VR khi không thấy console.

---

### 7.3 `MenuController.cs` — Menu khởi động / Startup Menu

> **Path:** `StartUpScene/MenuController.cs`

**EN:** Startup screen controller. Shows player ID, middleware toggle, IP configuration. Saves IP/PORT/MIDDLEWARE to `PlayerPrefs` and loads `"Main Scene"`.

**VI:** Controller màn hình khởi động. Hiển thị player ID, toggle middleware, cấu hình IP. Lưu IP/PORT/MIDDLEWARE vào `PlayerPrefs` và load `"Main Scene"`.

### 7.4 `EndofGameController.cs` — Màn hình kết quả / Result Screen

> **Path:** `EndOfGameScene/EndofGameController.cs`

**EN:** End screen. Displays player ID and `StaticInformation.endOfGame` text. Reset button returns to `"Startup Menu"`.

**VI:** Màn hình kết thúc. Hiển thị player ID và văn bản `StaticInformation.endOfGame`. Nút reset quay về `"Startup Menu"`.

---

## 8. Bảng message Unity → GAMA

| # | Action | When / Khi nào | Args |
|---|--------|----------------|------|
| 1 | `new_connection` | `AUTHENTICATED` state | `id` |
| 2 | `create_init_player` | Direct mode connect | `id` |
| 3 | `send_init_data` | `LOADING_DATA` (polling 0.5s) | `id` |
| 4 | `player_position_updated` | After geometry processing | `id` |
| 5 | `player_ready_to_receive_geometries` | Entering `GAME` state | `id` |
| 6 | `player_ready` | One-shot before gameplay | `idP` |
| 7 | `player_finish_game` | Game ends | `idP` |
| 8 | **`update_player_pos`** | Timer (~0.5s) | `idP, x, y, o, remaining_time, dtree, fwater, score` |
| 9 | **`update_salty_water`** | Timer (~0.5s offset) | `idP, swsStr, xsStr, ysStr` — CSV Enemy positions |
| 10 | **`update_fresh_water`** | Timer (~0.5s offset) | `idP, fwsStr, xsStr, ysStr` — CSV Ally positions |
| 11 | `create_enemy_spawners` | Once (registration) | `idP, idESStr, xsStr, ysStr` |
| 12 | `create_trees` | Once (registration) | `idP, idTsStr, xsStr, ysStr` |
| 13 | `move_create_pumper` | Per pumper creation | `idP, idwp, x, y` |
| 14 | `move_geoms_followed` | When toFollow objects exist | `ids, points, sep` |
| 15 | `move_player_external` | ⚠ Dead code (not called) | `id, x, y, z, angle` |
| 16 | `change_state` | On demand | `idP, new_state` |
| 17 | `ping_GAMA` | Reconnect attempt | `id` |
| 18 | `restart` | Restart simulation | `id` |

---

## 9. Bảng message GAMA → Unity

| # | JSON Key | DTO Class | Handler | EN | VI |
|---|----------|-----------|---------|----|----|
| 1 | `precision` | `ConnectionParameter` | Init converter | World config | Cấu hình thế giới |
| 2 | `properties` | `AllProperties` | Build propertyMap | Object type defs | Định nghĩa kiểu đối tượng |
| 3 | `pointsLoc` | `WorldJSONInfo` | Store (once) | Initial geometry | Hình học ban đầu |
| 4 | `rows` | `DEMData` | `manageUpdateTerrain()` | Full DEM | DEM đầy đủ |
| 5 | `indexX` | `DEMDataLoc` | `manageSetValueTerrain()` | Partial DEM | DEM từng phần |
| 6 | `wallId` | `WallInfo` | `manageWalls()` ⚠ | Walls (disabled) | Tường (vô hiệu) |
| 7 | `teleportId` | `TeleoportAreaInfo` | `manageTeleportationArea()` | Teleport areas | Vùng teleport |
| 8 | `enableMove` | `EnableMoveInfo` | `playerMovement()` | Movement toggle | Bật/tắt di chuyển |
| 9 | `triggers` | `AnimationInfo` | `updateAnimation()` | Animations | Animation |
| 10 | `subsidences` | `SubsidenceInfo` | `updateSubsidence()` | Subsidence data | Dữ liệu sụt lún |
| 11 | `enemyspawners` | `EnemySpawnerInfo` | `updateInfoSpawnRateEnemy()` | Spawn rates | Tốc độ spawn enemy |
| 12 | `pumpers` | `FreshWaterSpawn` | `updateInfoSpawnRatePumper()` | Pump rates | Tốc độ bơm |
| 13 | `endOfGame` | `EndOfGameInfo` | Load end scene | Game result | Kết quả game |
| 14 | `readyToStart` | — | Set flag | Ready signal | Tín hiệu sẵn sàng |
| 15 | `startGame` | `StartGameParameters` | `startGameWithTime()` | Timing params | Tham số thời gian |

---

## 10. Vấn đề cần lưu ý / Known Issues

| # | EN | VI |
|---|----|----|
| 1 | **Inconsistent payload format** — All periodic sends use manual CSV string concatenation instead of JSON. Fragile to parse on GAMA side. | **Payload không nhất quán** — Tất cả gửi định kỳ dùng nối chuỗi CSV thủ công thay vì JSON. Dễ lỗi parse phía GAMA. |
| 2 | **Dead code** — `UpdatePlayerPosition()` and `move_player_external` are never called in the Update loop. Appears superseded by `updatePlayerPos()`. | **Dead code** — `UpdatePlayerPosition()` và `move_player_external` không bao giờ gọi. Có vẻ bị thay thế bởi `updatePlayerPos()`. |
| 3 | **Naming mismatch** — `sendEnemies()` sends `"update_salty_water"` but the actual data is Enemy positions, not salt water. | **Tên không khớp ngữ nghĩa** — `sendEnemies()` gửi `"update_salty_water"` nhưng dữ liệu thực là vị trí Enemy. |
| 4 | **Tag-based lookup every frame** — `FindGameObjectsWithTag("Enemy")` and `FindGameObjectsWithTag("Ally")` are called every 0.5s. Should cache references. | **Tìm theo tag mỗi frame** — `FindGameObjectsWithTag` gọi mỗi 0.5s. Nên cache danh sách. |
| 5 | **Three timers, same period** — Enemy/FreshWater/Player all use `TimeSendPosition`, just phase-shifted. Could be a single batched message. | **Ba timer cùng chu kỳ** — Enemy/FreshWater/Player cùng dùng `TimeSendPosition`, chỉ lệch pha. Có thể gộp thành 1 batch. |
| 6 | **Thread safety** — `HandleReceivedMessage` runs on WebSocket thread but sets fields consumed in `FixedUpdate` (main thread). The deferred-flag pattern works but is implicit and error-prone. | **An toàn thread** — `HandleReceivedMessage` chạy trên thread WebSocket nhưng set field dùng trong `FixedUpdate` (main thread). Pattern cờ trì hoãn hoạt động nhưng ngầm định và dễ lỗi. |
| 7 | **`manageWalls()` disabled** — Entire method body is commented out. `WallInfo` still received and parsed but never used. | **`manageWalls()` vô hiệu** — Toàn bộ thân hàm bị comment. `WallInfo` vẫn nhận và parse nhưng không dùng. |
| 8 | **Bug in `updateAnimation()`** — Line `if (o == null && o.Count == 0)` should be `||` not `&&`. With `&&`, accessing `.Count` on null throws `NullReferenceException`. | **Bug trong `updateAnimation()`** — Dòng `if (o == null && o.Count == 0)` phải là `||` không phải `&&`. Với `&&`, gọi `.Count` trên null gây `NullReferenceException`. |
| 9 | **`ConnectionParameter` constructor bug** — `GamaCRSCoefY` is passed twice: `new CoordinateConverter(…, GamaCRSCoefY, GamaCRSCoefY, …)`. Second should be `GamaCRSCoefZ` or `1.0f`. | **Bug constructor `ConnectionParameter`** — `GamaCRSCoefY` truyền hai lần: `new CoordinateConverter(…, GamaCRSCoefY, GamaCRSCoefY, …)`. Tham số thứ hai phải là `GamaCRSCoefZ` hoặc `1.0f`. |
| 10 | **`id` vs `idP` inconsistency** — Some actions use `"id"` as the player key, others use `"idP"`. GAMA side must handle both. | **`id` vs `idP` không nhất quán** — Một số action dùng `"id"` làm khóa player, số khác dùng `"idP"`. Phía GAMA phải xử lý cả hai. |
