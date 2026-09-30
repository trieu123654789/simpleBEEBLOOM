# Implementation Plan: Tool-Based Harvesting

## Tổng quan

**THÊM** thu hoạch bằng công cụ **BÊN CẠNH** hệ thống Bag hiện tại:

| Đối tượng | Bag (giữ nguyên) | Tool (thêm mới) |
|-----------|-----------------|-----------------| 
| 🥥 Dừa | ❌ Không dùng | Liềm chém → Rơi → **Bỏ vào Bag** → Điểm |
| 🐟 Cá | ✅ Chạm = điểm | Vợt + Xô → Điểm |
| 🌾 Lúa | ✅ Chạm = điểm | Liềm cắt → Điểm |

**Cách lấy công cụ:** Tool Station (Giá đựng công cụ)
- Player đến giá → Nhặt → Spawn bản sao vào tay
- Thả công cụ → Tự hủy (destroy)
- Giá luôn còn công cụ

---

## Phase 1: Tool Station

### [NEW] `David_ToolStation.cs`
Giá đựng công cụ - spawn bản sao khi nhặt:

```csharp
public class David_ToolStation : MonoBehaviour
{
    public GameObject toolPrefab;     // Prefab công cụ
    public Transform spawnPoint;      // Vị trí spawn
    public string interactTag = "Player";
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(interactTag))
            SpawnTool();
    }
    
    void SpawnTool()
    {
        Instantiate(toolPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}
```

**Unity Setup:**
1. Tạo GameObject "ToolStation_Sickle" (hoặc Net/Bucket)
2. Gắn `David_ToolStation` + Collider (IsTrigger)
3. Kéo prefab công cụ vào `toolPrefab`

---

## Phase 2: Tool Scripts

### [NEW] `David_HarvestTool.cs` - Base class

```csharp
public enum ToolType { None, FishingNet, Bucket, Sickle }

public class David_HarvestTool : MonoBehaviour
{
    public ToolType toolType;
    public bool isHeld = false;
    public float destroyDelay = 5f;  // Tự hủy sau khi thả
    
    public void OnDrop()
    {
        isHeld = false;
        Destroy(gameObject, destroyDelay);
    }
}
```

---

### [NEW] `David_Sickle.cs` - Liềm

```csharp
public class David_Sickle : David_HarvestTool
{
    void OnTriggerEnter(Collider other)
    {
        if (!isHeld) return;
        
        // Chém dừa
        var fruit = other.GetComponent<David_Fruit>();
        if (fruit != null && fruit.fruitType == FruitType.Coconut)
            fruit.DropFromTree();
        
        // Cắt lúa
        var rice = other.GetComponent<David_Rice>();
        if (rice != null)
            rice.Harvest();
    }
}
```

---

### [NEW] `David_FishingNet.cs` - Vợt

```csharp
public class David_FishingNet : David_HarvestTool
{
    public David_Bucket linkedBucket;
    
    void OnTriggerEnter(Collider other)
    {
        if (!isHeld || linkedBucket == null) return;
        
        var fish = other.GetComponent<David_Fish>();
        if (fish != null)
            fish.CatchWithNet(linkedBucket);
    }
}
```

---

### [NEW] `David_Bucket.cs` - Xô

```csharp
public class David_Bucket : David_HarvestTool
{
    public int fishCount = 0;
    public int maxCapacity = 5;
    
    public bool AddFish(int score)
    {
        if (fishCount >= maxCapacity) return false;
        fishCount++;
        Thuan_23127_GameManager.Instance?.AddScore(score);
        return true;
    }
}
```

---

## Phase 3: Target Objects

### [MODIFY] `David_Fruit.cs` - Thêm Sickle + Drop flow

```diff
+ public bool isOnTree = true;    // Đang trên cây?
+ public bool canCollect = false; // Chỉ nhặt được sau khi rơi

+ public void DropFromTree()
+ {
+     if (!isOnTree) return;
+     isOnTree = false;
+     canCollect = true;  // Giờ mới cho phép nhặt
+     var rb = GetComponent<Rigidbody>();
+     if (rb) rb.isKinematic = false;  // Rơi xuống
+ }

  // Sửa OnTriggerEnter:
  private void OnTriggerEnter(Collider other)
  {
+     // Dừa phải rơi xuống trước mới nhặt được
+     if (fruitType == FruitType.Coconut && !canCollect)
+         return;
      
      // Logic Bag như cũ...
  }
```

**Flow mới:**
1. Dừa trên cây (`isOnTree=true`, `canCollect=false`)
2. Liềm chạm → `DropFromTree()` → Rơi xuống
3. Giờ `canCollect=true` → Chạm Bag = cộng điểm

---

### [NEW] `David_Fish.cs` - Cá

```csharp
public class David_Fish : MonoBehaviour
{
    public FarmArea ownerArea;
    private bool _caught = false;
    
    public void CatchWithNet(David_Bucket bucket)
    {
        if (_caught) return;
        int score = GetScore();
        if (!bucket.AddFish(score)) return;
        _caught = true;
        Destroy(gameObject);
    }
    
    int GetScore()
    {
        bool isFresh = ownerArea?.waterType == WaterType.Fresh;
        bool isRainy = RulesoftheGame_VU2_1.Saltwater_Intrusion < 1f;
        if (isFresh) return isRainy ? 10 : 20;
        else         return isRainy ? 30 : 40;
    }
}
```

---

### [NEW] `David_Rice.cs` - Lúa

```csharp
public class David_Rice : MonoBehaviour
{
    public FarmArea ownerArea;
    private bool _harvested = false;
    
    public void Harvest()
    {
        if (_harvested) return;
        _harvested = true;
        Thuan_23127_GameManager.Instance?.AddScore(GetScore());
        Destroy(gameObject);
    }
    
    int GetScore()
    {
        bool isFresh = ownerArea?.waterType == WaterType.Fresh;
        bool isRainy = RulesoftheGame_VU2_1.Saltwater_Intrusion < 1f;
        if (isFresh) return isRainy ? 60 : -20;
        else         return isRainy ? 40 : 20;
    }
}
```

---

## Phase 4: Unity Setup

### Tool Station Setup
1. Tạo 3 Tool Stations: Sickle, FishingNet, Bucket
2. Mỗi station có prefab công cụ tương ứng
3. Đặt gần vùng thu hoạch

### Prefab Công cụ
| Prefab | Script | Collider |
|--------|--------|----------|
| Sickle | `David_Sickle` | Trigger |
| FishingNet | `David_FishingNet` | Trigger |
| Bucket | `David_Bucket` | Trigger |

### Target Objects
| Object | Script | Thêm |
|--------|--------|------|
| Dừa trên cây | `David_Fruit` | Rigidbody (isKinematic=true) |
| Cá | `David_Fish` | Collider (Trigger) |
| Lúa | `David_Rice` | Collider (Trigger) |

---

## File Summary

| File | Action |
|------|--------|
| `David_ToolStation.cs` | **[NEW]** |
| `David_HarvestTool.cs` | **[NEW]** |
| `David_Sickle.cs` | **[NEW]** |
| `David_FishingNet.cs` | **[NEW]** |
| `David_Bucket.cs` | **[NEW]** |
| `David_Fish.cs` | **[NEW]** |
| `David_Rice.cs` | **[NEW]** |
| `David_Fruit.cs` | **[MODIFY]** |
