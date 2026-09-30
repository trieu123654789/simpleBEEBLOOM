# Cơ chế Trồng Cây Chắn Nước Mặn (TreeBarrier)

## Tổng quan

Cây trồng chắn mặn (TreeBarrier) là một công trình phòng thủ trong hệ thống Build. Mỗi cây trồng được sẽ **giữ 1 con nước mặn** (Enemy) không cho di chuyển vào nội đồng, bảo vệ cây trồng và công trình phía trong.

### Ý nghĩa giáo dục

Mô phỏng thực tế: **trồng rừng phòng hộ ven biển** giúp ngăn chặn xâm nhập mặn vào đất liền, nhưng cây sẽ bị ảnh hưởng nếu tiếp xúc nước mặn lâu dài.

---

## Luồng hoạt động

```
Người chơi nhấn Button_Tree (index 3) trong BuildUI
        │
        ▼
Build mode → Ray chiếu đến SurfaceConnector → Ghost cây hiện lên
        │
        ▼
Nhấn trigger xác nhận → Cây thật (PFB_TreeBarrier) được đặt
        │
        ▼
Enemy đi vào vùng SphereCollider (trigger) của cây
        │
        ▼
TreeBarrier.TrapEnemy() → EnemyController.SetTrapped(true)
        │                   → NavMeshAgent.isStopped = true
        │                   → Enemy đứng yên tại chỗ
        ▼
Cây bị ăn mòn dần (corrosionDamagePerSecond = 0.5 HP/s)
        │
        ├── Cây chết trước (HP ≤ 0)
        │       → ReleaseEnemy() → Enemy tiếp tục di chuyển theo waypoint
        │       → Animation Tree_Die → Destroy sau 2s
        │
        └── Enemy chết trước (bị Ally trung hòa)
                → Cây sẵn sàng bắt con mặn tiếp theo
```

---

## Quy tắc gameplay

| Quy tắc | Chi tiết |
|---|---|
| **Tỷ lệ** | 1 cây giữ tối đa 1 con mặn |
| **Ăn mòn** | Khi đang giữ enemy, cây mất 0.5 HP/s (cấu hình được) |
| **Cây chết** | Thả enemy, enemy tiếp tục đi → cần trồng cây mới |
| **Enemy chết** | Cây được giải phóng, tự động bắt con mặn kế tiếp đi qua |
| **Số lượng tối đa** | Mặc định 5 cây (cấu hình trong ConstructionSO) |
| **Cooldown** | 3 giây giữa mỗi lần trồng (cấu hình trong ConstructionSO) |

---

## Các file liên quan

| File | Vai trò |
|---|---|
| `Assets/Scripts/VU2/TreeBarrier.cs` | Script chính — bắt enemy, ăn mòn, thả enemy |
| `Assets/GAMA_Resources/Scripts/RUNTIME/CORE/EnemyController.cs` | Đã thêm `SetTrapped(bool)` / `IsTrapped` — dừng/tiếp tục di chuyển enemy |
| `Assets/GAMA_Resources/Scripts/RUNTIME/MANAGER/BuildSystemManager.cs` | Đã thêm `case 3` cho thống kê TreeBarrier |
| `Assets/GAMA_Resources/Scripts/RUNTIME/MANAGER/StatisticsManager.cs` | Đã thêm `currentTreeBarrierCount` + getter |

---

## Hướng dẫn Setup trong Unity

### Bước 1: Tạo ConstructionSO cho Tree

1. Trong Project window: **Right-click** → **Create** → **ScriptableObjects** → **ConstructionSO**
2. Đặt tên: `Tree`
3. Cấu hình trong Inspector:

| Field | Giá trị |
|---|---|
| Model Build Prefab | Kéo `PFB_TreeBarrier_Ghost` vào *(tạo ở bước 3)* |
| Final Prefab | Kéo `PFB_TreeBarrier` vào *(tạo ở bước 2)* |
| Cost | `0` |
| Description | `Trồng cây chắn mặn. Mỗi cây giữ 1 con nước mặn không di chuyển vào nội đồng.` |
| Max Quantity | `5` |
| Cooldown Time | `3` |

### Bước 2: Tạo Final Prefab (PFB_TreeBarrier)

Tạo prefab cây trồng với cấu trúc:

```
PFB_TreeBarrier                     ← Empty root (Tag: Construction)
│
├── Components trên root:
│   ├── TreeBarrier (Script)        ← Thêm component
│   │   ├── Max Health: 5
│   │   ├── Corrosion Damage Per Second: 0.5
│   │   ├── Animator: ← kéo Animator từ child TreeModel vào
│   │   └── Trap Radius: 3
│   ├── ConstructionRemover (Script) ← Thêm component
│   ├── SphereCollider              ← isTrigger = ✓, Radius = 3
│   └── BoxCollider                 ← isTrigger = ✗ (va chạm vật lý)
│
├── TreeModel                       ← Model 3D cây (copy từ AM_Tree có sẵn)
│   └── Animator                    ← Animation clips cần có:
│       ├── Tree_Good               (cây khỏe mạnh — khi bắt enemy)
│       ├── Tree_Bad                (cây yếu — khi HP ≤ 50%)
│       └── Tree_Die                (cây chết)
│
└── Connector (child Empty)         ← Thêm component Connector
    └── Collider (Sphere/Box)       ← Layer: BuildConnector
```

### Bước 3: Tạo Ghost Prefab (PFB_TreeBarrier_Ghost)

1. **Duplicate** `PFB_TreeBarrier` → đổi tên `PFB_TreeBarrier_Ghost`
2. **Gỡ bỏ**: `TreeBarrier`, `ConstructionRemover`, `SphereCollider` (trigger)
3. **Thêm**: `GhostConstruction` (Script)
   - Valid Material: ← kéo material xanh (giống ghost Pump/Gate)
   - Invalid Material: ← kéo material đỏ
   - Collide Layer Mask: chọn layer cần check va chạm
   - Connector Layer Mask: chọn layer BuildConnector
4. Giữ nguyên child **Connector** và **TreeModel**

### Bước 4: Thêm vào BuildSystemManager

1. Trong Hierarchy, tìm object có component **BuildSystemManager** (Script)
   - Đường dẫn trong scene: `ManagersMulti → Game Manager`
2. Tìm field **Constructions** (List) và thêm Tree SO:

```
Constructions (List<ConstructionSO>):
  [0] Gate       ← đã có
  [1] Pump       ← đã có
  [2] Lake       ← đã có
  [3]            ← Nhấn "+" → Kéo ScriptableObject "Tree" vào đây
```

3. Kiểm tra các field khác đã được cấu hình:

| Field | Giá trị | Ghi chú |
|---|---|---|
| Build IU | Kéo object có `BuildUI` script | Đã có sẵn |
| Build Ray Interactor | XR Ray tay phải | Đã có sẵn |
| Build Mode Indicator | Icon "!" trên ray | Đã có sẵn |
| Connector Layer Mask | `BuildConnector` | Đã có sẵn |
| Connector Detection Radius | `1.5` | Đã có sẵn |
| Subsidence Manager | Kéo object có `SubsidenceManager` | Đã có sẵn |

> **Lưu ý:** Khi xây TreeBarrier, `Build()` sẽ tự động:
> - Gán `buildSystemManager` vào `ConstructionRemover` trên prefab cây
> - Reset cooldown + giảm số lượng theo ConstructionSO "Tree"
> - Disable SurfaceConnector đã dùng (qua `Connector.UpdateConnector(false)`)
> - Gọi `SubsidenceManager.IncreaseSubsidenceLevel()` + `DecreaseWaterLevel()`
> - Cập nhật thống kê qua `StatisticsManager.IncreateTreeBarrierCount()` (case 3)

### Bước 5: Tạo Button_Tree trong UI

1. Trong Hierarchy, mở: **BuildUI → Menu → Button**
2. **Duplicate** `Button_Pump` (hoặc bất kỳ button nào đã có)
3. Đổi tên → `Button_Tree`
4. Đổi icon/text → hình cây hoặc chữ "Tree" / "Cây"
5. Trong component **Button → OnClick()**:
   - Object: kéo object có **BuildUI** script
   - Function: `BuildUI` → `ChoseConstruction`
   - **Parameter: `3`** *(index của Tree trong list Constructions)*
6. Thêm **OnPointerEnter** → `BuildUI.OnHover(3)` (hiện tooltip)
7. Thêm **OnPointerExit** → `BuildUI.ExitHover()` (ẩn tooltip)

### Bước 6: Cập nhật BuildUI Lists

Chọn object chứa **BuildUI (Script)** trong Inspector:

```
Current Quantities (List):        ← Nhấn "+" thêm 1 slot
  [0] TextMeshPro — Gate qty
  [1] TextMeshPro — Pump qty
  [2] TextMeshPro — Lake qty
  [3]                             ← Kéo TextMeshPro số lượng từ Button_Tree vào

Image Cooldown List (List):       ← Nhấn "+" thêm 1 slot
  [0] Image — Gate cooldown
  [1] Image — Pump cooldown
  [2] Image — Lake cooldown
  [3]                             ← Kéo Image cooldown từ Button_Tree vào
```

---

## Cấu hình TreeBarrier trong Inspector

| Parameter | Mặc định | Mô tả |
|---|---|---|
| Max Health | `5` | HP tối đa khi mới trồng |
| Corrosion Damage Per Second | `0.5` | Sát thương/giây khi đang giữ enemy (10s để chết với HP=5) |
| Animator | *(kéo vào)* | Animator của model cây |
| Trap Radius | `3` | Bán kính bắt enemy (đơn vị Unity) |

### Tính toán thời gian sống

```
Thời gian cây sống khi giữ enemy = maxHealth / corrosionDamagePerSecond

Ví dụ mặc định: 5 / 0.5 = 10 giây
```

Điều chỉnh 2 giá trị này để cân bằng gameplay:
- **Tăng maxHealth** hoặc **giảm corrosion** → cây sống lâu hơn → dễ hơn
- **Giảm maxHealth** hoặc **tăng corrosion** → cây chết nhanh → khó hơn

---

## Animation clips cần có

| Clip Name | Khi nào phát | Mô tả |
|---|---|---|
| `Tree_Good` | Bắt được enemy | Cây rung nhẹ, lá xanh tươi |
| `Tree_Bad` | HP ≤ 50% | Lá héo, đổi màu vàng |
| `Tree_Die` | HP ≤ 0 | Cây gãy đổ, biến mất |

> **Tip**: Có thể dùng lại animation từ `Tree_Animation.cs` (VU1) đã có sẵn các clip này.

---

## Tương tác với hệ thống khác

| Hệ thống | Tương tác |
|---|---|
| **EnemyController** | `SetTrapped(true/false)` dừng/tiếp tục di chuyển |
| **Enemy** | Check `IsDead()` để biết khi nào thả enemy |
| **Ally (FreshWater)** | Ally vẫn có thể trung hòa enemy đang bị giữ → cây được giải phóng |
| **BuildSystemManager** | Index `3` trong list Constructions, thống kê qua `StatisticsManager` |
| **GameUI** | Hiển thị marker HUD cho cây trên minimap |
| **TidalEnemyModifier** | Enemy bị giữ không bị ảnh hưởng bởi triều (đã dừng di chuyển) |
