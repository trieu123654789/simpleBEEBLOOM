using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// Thuan_23127_ChickenEggSpawner - Spawns eggs at chicken position over time.
// Thuan_23127_ChickenEggSpawner - Spawn trứng tại vị trí gà theo thời gian.
// 
// This script is attached to chickens to simulate egg laying:
// - Spawns egg prefab at random intervals
// - Respects maximum egg count limit
// - Clears all eggs when season changes
// 
// Script này được gắn vào gà để mô phỏng đẻ trứng:
// - Spawn prefab trứng với khoảng thời gian ngẫu nhiên
// - Tuân theo giới hạn số trứng tối đa
// - Xóa tất cả trứng khi đổi mùa
// =============================================================================
public class Thuan_23127_ChickenEggSpawner : MonoBehaviour
{
    // =========================================================================
    // EGG SPAWN CONFIGURATION
    // CẤU HÌNH SPAWN TRỨNG
    // =========================================================================
    [Header("Egg Spawn Config / Cấu hình đẻ trứng")]
    [Tooltip("Egg prefab to spawn")]
    // Prefab for the egg to instantiate.
    // Prefab trứng để instantiate.
    public GameObject eggPrefab;
    
    [Tooltip("Minimum time between egg spawns (seconds)")]
    // Minimum spawn interval in seconds.
    // Khoảng thời gian spawn tối thiểu tính bằng giây.
    public float minSpawnInterval = 10f;
    
    [Tooltip("Maximum time between egg spawns (seconds)")]
    // Maximum spawn interval in seconds.
    // Khoảng thời gian spawn tối đa tính bằng giây.
    public float maxSpawnInterval = 10f;
    
    [Tooltip("Spawn position offset from chicken (Y = height)")]
    // Offset from chicken position where egg appears.
    // Độ lệch từ vị trí gà nơi trứng xuất hiện.
    public Vector3 spawnOffset = new Vector3(0f, 0.1f, 0f);
    
    // =========================================================================
    // LIMITS
    // GIỚI HẠN
    // =========================================================================
    [Header("Limits / Giới hạn")]
    [Tooltip("Maximum eggs that can exist (0 = unlimited)")]
    // Prevents too many eggs from cluttering the scene.
    // Ngăn quá nhiều trứng làm lộn xộn scene.
    public int maxEggs = 50;
    
    [Tooltip("Chicken transform for spawn position (uses script transform if empty)")]
    // Reference to the moving chicken object.
    // Tham chiếu đến object gà đang di chuyển.
    public Transform chickenTransform;
    
    // =========================================================================
    // INTERNAL STATE
    // TRẠNG THÁI NỘI BỘ
    // =========================================================================
    
    // Countdown timer to next spawn.
    // Bộ đếm ngược đến lần spawn tiếp theo.
    private float timer;
    
    // Current number of eggs in scene.
    // Số trứng hiện tại trong scene.
    private int currentEggCount = 0;
    
    // List of spawned eggs for management (cleared on season change).
    // Danh sách trứng đã spawn để quản lý (xóa khi đổi mùa).
    private List<GameObject> spawnedEggs = new List<GameObject>();

    // True if this spawner is on a duck → egg laying DISABLED for ducks.
    // True nếu spawner này trên vịt → đẻ trứng BỊ TẮT cho vịt.
    private bool _isDuck = false;

    // =========================================================================
    // Start - Initialize spawn timer.
    // Start - Khởi tạo timer spawn.
    // =========================================================================
    private void Start()
    {
        // Detect if this spawner is on a duck → disable egg laying.
        // Phát hiện nếu spawner này trên vịt → tắt đẻ trứng.
        _isDuck = GetComponent<Thuan_23127_DuckAiAction>() != null
              || GetComponentInParent<Thuan_23127_DuckAiAction>() != null
              || GetComponentInChildren<Thuan_23127_DuckAiAction>() != null;
        
        if (_isDuck)
        {
            Debug.Log($"[ChickenEggSpawner] VỊT phát hiện trên {gameObject.name} → TẮT đẻ trứng.");
            return;
        }
        
        SetRandomTimer();
    }
    
    // =========================================================================
    // OnEnable - Subscribe to season change events.
    // OnEnable - Đăng ký lắng nghe sự kiện đổi mùa.
    // =========================================================================
    private void OnEnable()
    {
        GameRulesProvider.OnPhaseChanged += OnSeasonChanged;
    }

    // =========================================================================
    // OnDisable - Unsubscribe from events.
    // OnDisable - Hủy đăng ký sự kiện.
    // =========================================================================
    private void OnDisable()
    {
        GameRulesProvider.OnPhaseChanged -= OnSeasonChanged;
    }

    // =========================================================================
    // Update - Countdown timer and spawn eggs.
    // Update - Đếm ngược timer và spawn trứng.
    // =========================================================================
    private void Update()
    {
        // DUCK: egg laying disabled.
        // VỊT: đẻ trứng đã tắt.
        if (_isDuck) return;
        
        if (eggPrefab == null)
        {
            return;
        }
        
        timer -= Time.deltaTime;
        
        if (timer <= 0f)
        {
            SpawnEgg();
            SetRandomTimer();
        }
    }
    
    // =========================================================================
    // OnSeasonChanged - Clears all eggs when season changes.
    // OnSeasonChanged - Xóa tất cả trứng khi đổi mùa.
    // 
    // This simulates a new breeding cycle each season.
    // Điều này mô phỏng một chu kỳ sinh sản mới mỗi mùa.
    // =========================================================================
    private void OnSeasonChanged(SeasonPhase newPhase)
    {
        Debug.Log($"[ChickenEggSpawner] Season changed to {newPhase}. Resetting eggs!");
        
        // Destroy all existing eggs.
        // Hủy tất cả trứng hiện có.
        foreach (var egg in spawnedEggs)
        {
            if (egg != null)
            {
                Destroy(egg);
            }
        }
        spawnedEggs.Clear();
        currentEggCount = 0;
        
        // Reset timer for new batch.
        // Reset timer cho lứa mới.
        SetRandomTimer();
    }

    // =========================================================================
    // SpawnEgg - Creates a new egg at chicken position.
    // SpawnEgg - Tạo một trứng mới tại vị trí gà.
    // =========================================================================
    private void SpawnEgg()
    {
        // Check max egg limit.
        // Kiểm tra giới hạn trứng tối đa.
        if (maxEggs > 0 && currentEggCount >= maxEggs)
        {
            return;
        }
        
        // Calculate spawn position.
        // Tính vị trí spawn.
        Vector3 basePosition = (chickenTransform != null) ? chickenTransform.position : transform.position;
        Vector3 spawnPosition = basePosition + spawnOffset;
        
        // Instantiate egg.
        // Instantiate trứng.
        GameObject egg = Instantiate(eggPrefab, spawnPosition, Quaternion.identity);
        
        if (egg != null)
        {
            currentEggCount++;
            spawnedEggs.Add(egg);
            
            // Add notifier component to track destruction.
            // Thêm component notifier để theo dõi việc hủy.
            egg.AddComponent<EggDestroyNotifier>().Initialize(this);
        }
    }

    // =========================================================================
    // OnEggDestroyed - Called when an egg is destroyed (collected or expired).
    // OnEggDestroyed - Được gọi khi một trứng bị hủy (thu hoạch hoặc hết hạn).
    // =========================================================================
    public void OnEggDestroyed(GameObject egg)
    {
        currentEggCount--;
        if (currentEggCount < 0) currentEggCount = 0;
        
        if (spawnedEggs.Contains(egg))
        {
            spawnedEggs.Remove(egg);
        }
    }
    
    // =========================================================================
    // ResetSpawner - Resets spawner to initial state.
    // ResetSpawner - Reset spawner về trạng thái ban đầu.
    // =========================================================================
    public void ResetSpawner()
    {
        SetRandomTimer();
        currentEggCount = 0;
        spawnedEggs.Clear();
    }

    // =========================================================================
    // SetRandomTimer - Sets timer to random value within configured range.
    // SetRandomTimer - Đặt timer thành giá trị ngẫu nhiên trong phạm vi đã cấu hình.
    // =========================================================================
    private void SetRandomTimer()
    {
        timer = Random.Range(minSpawnInterval, maxSpawnInterval);
    }
}

// =============================================================================
// EggDestroyNotifier - Helper component to notify spawner when egg is destroyed.
// EggDestroyNotifier - Component helper để thông báo spawner khi trứng bị hủy.
// 
// Added automatically to each spawned egg.
// Được thêm tự động vào mỗi trứng đã spawn.
// =============================================================================
public class EggDestroyNotifier : MonoBehaviour
{
    // Reference to the spawner that created this egg.
    // Tham chiếu đến spawner đã tạo trứng này.
    private Thuan_23127_ChickenEggSpawner spawner;
    
    // =========================================================================
    // Initialize - Sets the parent spawner reference.
    // Initialize - Đặt tham chiếu spawner cha.
    // =========================================================================
    public void Initialize(Thuan_23127_ChickenEggSpawner owner)
    {
        spawner = owner;
    }
    
    // =========================================================================
    // OnDestroy - Notifies spawner to decrement egg count.
    // OnDestroy - Thông báo spawner để giảm số đếm trứng.
    // =========================================================================
    private void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.OnEggDestroyed(gameObject);
        }
    }
}
