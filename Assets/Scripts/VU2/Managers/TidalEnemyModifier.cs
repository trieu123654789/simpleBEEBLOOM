using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// =============================================================================
// TidalEnemyModifier - Modifies enemy spawn rate and movement speed based on tide.
// TidalEnemyModifier - Điều chỉnh tốc độ sinh và di chuyển con mặn theo triều.
//
// BEHAVIOR:
// - Triều cường (Spring Tide): enemies spawn faster, move faster, more enemies
// - Triều kém (Neap Tide): enemies spawn slower, move slower, fewer enemies
// - Enemies spawned during Neap Tide have reversed waypoints (retreat outward)
//
// HÀNH VI:
// - Triều cường: con mặn sinh nhanh hơn, di chuyển nhanh hơn, nhiều hơn
// - Triều kém: con mặn sinh chậm hơn, di chuyển chậm hơn, ít hơn
// - Con mặn sinh trong triều kém có waypoint đảo ngược (rút ra ngoài)
// =============================================================================
public class TidalEnemyModifier : MonoBehaviour
{
    // =========================================================================
    // CONFIGURATION
    // CẤU HÌNH
    // =========================================================================
    [Header("Enemy References / Tham chiếu con mặn")]
    
    [Tooltip("Tag used to find active enemies in scene.\n"
           + "Tag dùng để tìm con mặn đang hoạt động trong scene.")]
    public string enemyTag = "Enemy";

    [Header("Speed Settings / Cài đặt tốc độ")]
    
    [Tooltip("Base move speed of enemies (from Enemy prefab).\n"
           + "Tốc độ di chuyển cơ sở (từ prefab Enemy).")]
    public float baseEnemySpeed = 2f;

    [Tooltip("Smoothing speed for speed transitions.\n"
           + "Tốc độ chuyển đổi mượt cho thay đổi tốc độ.")]
    public float speedTransitionRate = 2f;

    [Header("Retreat Settings / Cài đặt rút lui")]
    
    [Tooltip("During Neap Tide, should existing enemies reverse direction?\n"
           + "Trong triều kém, con mặn hiện có có đảo hướng không?")]
    public bool reverseEnemiesOnNeapTide = true;

    [Tooltip("Speed multiplier for retreating enemies.\n"
           + "Hệ số tốc độ cho con mặn đang rút lui.")]
    public float retreatSpeedMultiplier = 0.8f;

    // =========================================================================
    // INTERNAL
    // NỘI BỘ
    // =========================================================================
    private TidalClockManager _manager;
    private float _currentSpeedMultiplier = 1f;
    private TidalState _lastState = (TidalState)(-1);

    // =========================================================================
    // LIFECYCLE
    // VÒNG ĐỜI
    // =========================================================================
    private void Start()
    {
        _manager = TidalClockManager.Instance;
        if (_manager == null)
        {
            Debug.LogWarning("[TidalEnemyModifier] TidalClockManager not found!");
            enabled = false;
            return;
        }

        TidalClockManager.OnTidalStateChanged += OnTideStateChanged;
        TidalClockManager.OnTidalIntensityUpdated += OnTidalIntensityUpdated;
    }

    private void OnDestroy()
    {
        TidalClockManager.OnTidalStateChanged -= OnTideStateChanged;
        TidalClockManager.OnTidalIntensityUpdated -= OnTidalIntensityUpdated;
    }

    // =========================================================================
    // EVENT HANDLERS
    // XỬ LÝ SỰ KIỆN
    // =========================================================================
    
    /// <summary>
    /// Called when tide state changes between Spring and Neap.
    /// Được gọi khi trạng thái triều chuyển giữa Cường và Kém.
    /// </summary>
    private void OnTideStateChanged(TidalState state)
    {
        if (state == _lastState) return;
        _lastState = state;

        if (state == TidalState.NeapTide && reverseEnemiesOnNeapTide)
        {
            // Reverse existing enemy movement (retreat outward).
            // Đảo hướng con mặn hiện có (rút ra ngoài theo con nước).
            ReverseAllActiveEnemies();
        }

        Debug.Log($"[TidalEnemyModifier] Tide → {state} | " +
                  $"SpawnMult={_manager.CurrentSpawnMultiplier:F1}x | " +
                  $"SpeedMult={_manager.CurrentEnemySpeedMultiplier:F1}x");
    }

    /// <summary>
    /// Called every frame with smooth intensity value.
    /// Được gọi mỗi frame với giá trị cường độ mượt.
    /// </summary>
    private void OnTidalIntensityUpdated(float intensity)
    {
        if (_manager == null) return;

        // Smooth speed transition.
        // Chuyển đổi tốc độ mượt.
        float targetMultiplier = _manager.CurrentEnemySpeedMultiplier;
        _currentSpeedMultiplier = Mathf.MoveTowards(
            _currentSpeedMultiplier, targetMultiplier,
            speedTransitionRate * Time.deltaTime
        );

        // Apply speed to all active enemies.
        // Áp dụng tốc độ cho tất cả con mặn đang hoạt động.
        ApplySpeedToAllEnemies(_currentSpeedMultiplier);
    }

    // =========================================================================
    // ENEMY SPEED CONTROL
    // ĐIỀU KHIỂN TỐC ĐỘ
    // =========================================================================
    
    /// <summary>
    /// Set NavMeshAgent speed on all active enemies.
    /// Đặt tốc độ NavMeshAgent cho tất cả con mặn đang hoạt động.
    /// </summary>
    private void ApplySpeedToAllEnemies(float multiplier)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        float newSpeed = baseEnemySpeed * multiplier;

        foreach (var enemyObj in enemies)
        {
            NavMeshAgent agent = enemyObj.GetComponent<NavMeshAgent>();
            if (agent && agent.isOnNavMesh)
            {
                agent.speed = newSpeed;
            }
        }
    }

    /// <summary>
    /// Reverse the direction of all active enemies (for Neap Tide retreat).
    /// Đảo hướng tất cả con mặn (cho rút lui triều kém).
    /// 
    /// Implementation: Set each enemy's destination to the spawner's position,
    /// effectively making them walk back out.
    /// 
    /// Thực hiện: Đặt điểm đến của mỗi con mặn về vị trí spawner,
    /// khiến chúng đi ngược ra ngoài.
    /// </summary>
    private void ReverseAllActiveEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        
        foreach (var enemyObj in enemies)
        {
            NavMeshAgent agent = enemyObj.GetComponent<NavMeshAgent>();
            EnemyController controller = enemyObj.GetComponent<EnemyController>();
            
            if (agent && agent.isOnNavMesh)
            {
                // Find nearest spawner as retreat destination.
                // Tìm spawner gần nhất làm điểm đến rút lui.
                EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>();
                if (spawners.Length > 0)
                {
                    // Find closest spawner.
                    // Tìm spawner gần nhất.
                    float minDist = float.MaxValue;
                    Vector3 retreatPos = enemyObj.transform.position;

                    foreach (var spawner in spawners)
                    {
                        float dist = Vector3.Distance(enemyObj.transform.position, spawner.transform.position);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            retreatPos = spawner.transform.position;
                        }
                    }

                    agent.speed = baseEnemySpeed * retreatSpeedMultiplier;
                    agent.SetDestination(retreatPos);
                }
            }
        }

        Debug.Log($"[TidalEnemyModifier] Reversed {enemies.Length} enemies for Neap Tide retreat.");
    }

    // =========================================================================
    // PUBLIC API FOR SPAWNER INTEGRATION
    // API CÔNG KHAI CHO TÍCH HỢP SPAWNER
    // =========================================================================
    
    /// <summary>
    /// Get adjusted spawn rate to pass to EnemySpawner.
    /// The spawner should multiply its base rate by this value.
    /// 
    /// Lấy tốc độ sinh đã điều chỉnh để truyền cho EnemySpawner.
    /// Spawner nên nhân tốc độ cơ sở với giá trị này.
    /// </summary>
    public float GetAdjustedSpawnRate(float baseRate)
    {
        if (_manager == null) return baseRate;
        return baseRate * _manager.CurrentSpawnMultiplier;
    }

    /// <summary>
    /// Get adjusted spawn count for a wave.
    /// Lấy số lượng sinh đã điều chỉnh cho 1 đợt.
    /// </summary>
    public int GetAdjustedSpawnCount(int baseCount)
    {
        if (_manager == null) return baseCount;
        return Mathf.Max(1, Mathf.RoundToInt(baseCount * _manager.CurrentSpawnMultiplier));
    }
}
