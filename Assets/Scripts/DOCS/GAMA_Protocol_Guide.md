# Hướng Dẫn Giao Thức GAMA - Unity

## Tổng Quan

Tài liệu này mô tả cách gửi dữ liệu từ Unity lên GAMA server. Sử dụng pattern này khi cần đăng ký/hủy đăng ký các object game với server GAMA.

---

## Kiến Trúc Tổng Quan

```
┌─────────────────────────────────────────────────────────────────┐
│                         Unity Game                              │
├─────────────────────────────────────────────────────────────────┤
│  TreeBarrier.cs / Barrack.cs / EnemySpawner.cs                  │
│       │                                                          │
│       │ Gọi createXXX() / deleteXXX()                           │
│       ▼                                                          │
│  SimulationManager.cs                                            │
│       │                                                          │
│       │ SendExecutableAsk("action_name", args)                  │
│       ▼                                                          │
│  ConnectionManager.cs                                            │
│       │                                                          │
│       │ WebSocket Message                                        │
│       ▼                                                          │
├─────────────────────────────────────────────────────────────────┤
│                      GAMA Server                                 │
│                                                                  │
│  Action: action_name(idP, id_object, x, y, ...)                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Các Bước Thực Hiện

### Bước 1: Thêm Dictionary Quản Lý trong SimulationManager.cs

```csharp
// Khai báo dictionary để theo dõi các object đã đăng ký
private Dictionary<string, GameObject> treeBarriers;  // Ví dụ cho TreeBarrier

// Khởi tạo trong Awake()
void Awake()
{
    // ... existing code ...
    treeBarriers = new Dictionary<string, GameObject>();
}
```

### Bước 2: Tạo Phương Thức Đăng Ký (Create)

```csharp
/// <summary>
/// EN: Register a new [ObjectType] with GAMA server.
/// VI: Đăng ký [ObjectType] mới với GAMA server.
/// </summary>
public void createTreeBarrier(GameObject treeBarrier)
{
    // Guard clauses - kiểm tra điều kiện
    if (ConnectionManager.Instance == null)
    {
        Debug.LogWarning("[SimulationManager] createTreeBarrier skipped — no GAMA connection.");
        return;
    }
    if (parameters == null)
    {
        Debug.LogWarning("[SimulationManager] createTreeBarrier skipped — parameters not initialized.");
        return;
    }

    // Kiểm tra trùng lặp
    string key = treeBarrier.GetInstanceID() + "";
    if (treeBarriers.ContainsKey(key))
    {
        Debug.LogWarning($"[SimulationManager] Duplicate TreeBarrier InstanceID skipped: {key}");
        return;
    }
    
    // Đăng ký vào dictionary
    treeBarriers.Add(key, treeBarrier);

    // Chuẩn bị arguments
    Dictionary<string, string> args = new Dictionary<string, string> {
        {"idP", ConnectionManager.Instance.GetConnectionId()},      // ID người chơi
        {"idtb", treeBarrier.GetInstanceID() + ""},                  // ID object
        {"x", "" + treeBarrier.transform.position.x * parameters.precision},  // Tọa độ X
        {"y", "" + treeBarrier.transform.position.z * parameters.precision}   // Tọa độ Y (Unity Z → GAMA Y)
    };

    // Gửi lên GAMA
    ConnectionManager.Instance.SendExecutableAsk("create_tree_barrier", args);
    Debug.Log($"[GAMA Send] create_tree_barrier | idP={args["idP"]}, idtb={args["idtb"]}, x={args["x"]}, y={args["y"]}");
}
```

### Bước 3: Tạo Phương Thức Hủy Đăng Ký (Delete)

```csharp
/// <summary>
/// EN: Notify GAMA server when a [ObjectType] dies/is destroyed.
/// VI: Thông báo GAMA server khi [ObjectType] chết/bị hủy.
/// </summary>
public void deleteTreeBarrier(GameObject treeBarrier)
{
    if (ConnectionManager.Instance == null)
    {
        Debug.LogWarning("[SimulationManager] deleteTreeBarrier skipped — no GAMA connection.");
        return;
    }

    // Xóa khỏi dictionary
    string key = treeBarrier.GetInstanceID() + "";
    if (treeBarriers.ContainsKey(key))
    {
        treeBarriers.Remove(key);
    }

    // Chuẩn bị arguments
    Dictionary<string, string> args = new Dictionary<string, string> {
        {"idP", ConnectionManager.Instance.GetConnectionId()},
        {"idtb", treeBarrier.GetInstanceID() + ""}
    };

    // Gửi lên GAMA
    ConnectionManager.Instance.SendExecutableAsk("delete_tree_barrier", args);
    Debug.Log($"[GAMA Send] delete_tree_barrier | idP={args["idP"]}, idtb={args["idtb"]}");
}
```

### Bước 4: Gọi Phương Thức từ Component

Trong file component của object (ví dụ: `TreeBarrier.cs`):

```csharp
public class TreeBarrier : MonoBehaviour
{
    private SimulationManager sm;

    void Start()
    {
        // Lấy reference tới SimulationManager
        sm = SimulationManager.Instance;
        
        // Đăng ký với GAMA khi khởi tạo
        if (sm != null)
        {
            sm.createTreeBarrier(gameObject);
        }
    }

    public void Die()
    {
        // Thông báo GAMA trước khi hủy
        if (sm != null)
        {
            sm.deleteTreeBarrier(gameObject);
        }
        
        Destroy(gameObject);
    }
}
```

---

## Quy Ước Đặt Tên

| Loại | Unity Method | GAMA Action |
|------|--------------|-------------|
| Tạo mới | `createXXX()` | `create_xxx` hoặc `move_create_xxx` |
| Xóa | `deleteXXX()` | `delete_xxx` |
| Cập nhật | `updateXXX()` | `update_xxx` |

---

## Conversion Tọa Độ

**QUAN TRỌNG**: Unity và GAMA sử dụng hệ tọa độ khác nhau!

```
Unity:  X (left-right), Y (up-down), Z (forward-backward)
GAMA:   X (left-right), Y (forward-backward)

Chuyển đổi:
- GAMA X = Unity X * precision
- GAMA Y = Unity Z * precision  (KHÔNG phải Unity Y!)
```

Luôn sử dụng `parameters.precision` để scale tọa độ.

---

## Các Ví Dụ Có Sẵn

Tham khảo các pattern đã implement:

| Object | File | Methods |
|--------|------|---------|
| Water Pump (Barrack) | SimulationManager.cs | `createMovePumper()` |
| Enemy Spawner | SimulationManager.cs | `createEnemySpawner()` |
| Tree Barrier | SimulationManager.cs | `createTreeBarrier()`, `deleteTreeBarrier()` |
| Trees | SimulationManager.cs | `sendTrees()` |
| Enemies | SimulationManager.cs | `sendEnemies()` |
| Fresh Water (Allies) | SimulationManager.cs | `sendFreshWater()` |
| Player Position | SimulationManager.cs | `updatePlayerPos()` |

---

## Lưu Ý Quan Trọng

1. **Kiểm tra null**: Luôn kiểm tra `ConnectionManager.Instance` và `parameters` trước khi gửi.

2. **Debug log**: Thêm `Debug.Log` với prefix `[GAMA Send]` để dễ trace.

3. **Dictionary key**: Sử dụng `GetInstanceID() + ""` làm key duy nhất.

4. **Timing**: 
   - Đăng ký trong `Start()` (sau khi object khởi tạo xong)
   - Hủy đăng ký TRƯỚC khi `Destroy(gameObject)`

5. **GAMA side**: Cần implement action tương ứng trong GAMA model:
   ```gaml
   action create_tree_barrier(string idP, string idtb, float x, float y) {
       // Xử lý trong GAMA
   }
   
   action delete_tree_barrier(string idP, string idtb) {
       // Xử lý trong GAMA
   }
   ```

---

## Troubleshooting

| Vấn đề | Nguyên nhân | Giải pháp |
|--------|-------------|-----------|
| Không gửi được | `ConnectionManager.Instance == null` | Kiểm tra kết nối GAMA |
| Tọa độ sai | Không nhân `precision` | Thêm `* parameters.precision` |
| Duplicate warning | Object đăng ký 2 lần | Kiểm tra `ContainsKey()` trước khi Add |
| Object không xóa trên GAMA | Quên gọi `deleteXXX()` | Thêm call trong `Die()` / `OnDestroy()` |

---

## Tác Giả & Cập Nhật

- **Ngày tạo**: 10/04/2026
- **Dự án**: simple.CTU.VU2
- **Files liên quan**: 
  - `Assets/GAMA_Resources/Scripts/Gama Provider/Simulation/SimulationManager.cs`
  - `Assets/Scripts/VU2/TreeBarrier.cs`
  - `Assets/GAMA_Resources/Scripts/Gama Provider/ConnectionManager.cs`
