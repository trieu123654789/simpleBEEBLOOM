using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// Barrack - Water Pump that spawns FreshWater (Ally) entities.
// Barrack - Máy Bơm Nước sinh ra các thực thể Nước Ngọt (Ally).
//
// ROLE: The player's main defense structure. Periodically spawns FreshWater
// prefabs that seek and neutralize SaltyWater enemies.
// VAI TRÒ: Công trình phòng thủ chính của người chơi. Định kỳ sinh ra prefab
// Nước Ngọt để tìm và trung hòa quân địch Nước Mặn.
//
// LIFECYCLE:
//   Start → Register with GAMA server (createMovePumper)
//   Update → Countdown spawnRate → Spawn FreshWater → Notify GameManager
//   TakeDamage → When HP=0, notify GAMA (delete_water_pump), play broken anim
//   Die → Spawn subsidence effect, destroy nearby Ally, destroy self
//
// VÒNG ĐỜI:
//   Start → Đăng ký với GAMA server (createMovePumper)
//   Update → Đếm ngược spawnRate → Spawn Nước Ngọt → Thông báo GameManager
//   TakeDamage → Khi máu=0, thông báo GAMA (delete_water_pump), chơi anim hỏng
//   Die → Sinh hiệu ứng sụt lún, hủy Ally lân cận, hủy bản thân
// =============================================================================
public class Barrack : MonoBehaviour, ISpawner, IDamageable
{
    // =========================================================================
    // SERIALIZED FIELDS — configured in Inspector / prefab.
    // CÁC TRƯỜNG SERIALIZE — cấu hình trong Inspector / prefab.
    // =========================================================================

    [Header("Basic Info")]
    [SerializeField] private string uniqueName;  // Unique ID / Mã định danh duy nhất
    [SerializeField] private int lvl;            // Level / Cấp độ

    [Header("Stats")]
    [SerializeField] private bool freeSpawn;     // If true, spawning costs no HP / Nếu true, spawn không tốn máu
    // EN: Starting water amount (HP). Default 2 — player must refill via grab.
    // VI: Lượng nước (HP) khởi điểm. Mặc định 2 — người chơi phải nạp thêm bằng grab.
    [SerializeField] private int health = 2;     // Starting / Current HP / Máu khởi điểm
    // EN: Maximum water capacity. Refill cannot exceed this value.
    // VI: Dung tích nước tối đa. Nạp thêm không vượt quá giá trị này.
    [SerializeField] private int maxHealth = 20;

    [SerializeField] private float spawnRate;        // Seconds between spawns / Giây giữa các lần spawn
    [SerializeField] private GameObject spawnPrefab; // FreshWater prefab to spawn / Prefab Nước Ngọt để spawn
    [SerializeField] private Transform spawnPoint;   // Position to spawn at / Vị trí spawn

    [Header("Miscellaneous")]
    [SerializeField] private float workRadius;                // Operating radius / Bán kính hoạt động
    [SerializeField] private LayerMask targetLayerMask;       // Layer of nearby pumps to detect / Layer máy bơm lân cận
    [SerializeField] private LayerMask spawnTriggerLayerMask; // Layer to check before spawning / Layer kiểm tra trước khi spawn
    [SerializeField] private Animator animator;                // Pump animator / Animator máy bơm

    [Header("Subsidence Settings")]
    [SerializeField] private GameObject subsidencePrefab;     // Sinkhole prefab on death / Prefab hố sụt khi chết
    [SerializeField] private Transform subsidenceSpawnPoint;  // Sinkhole spawn pos / Vị trí spawn hố sụt

    // =========================================================================
    // RUNTIME STATE
    // TRẠNG THÁI RUNTIME
    // =========================================================================
    private int currentHealh;   // Current HP / Máu hiện tại
    private float currentRate;  // Countdown to next spawn / Đếm ngược đến lần spawn kế
    
    GameManager GameManagerScript; // Cached reference / Tham chiếu cache

    // =========================================================================
    // PUBLIC PROPERTIES
    // THUỘC TÍNH CÔNG KHAI
    // =========================================================================
    public int Health
    {
        get { return currentHealh; }
    }
    // EN: Water capacity ceiling exposed for UI / refill scripts.
    // VI: Trần dung tích nước, mở ra cho UI / script nạp nước.
    public int MaxHealth
    {
        get { return maxHealth; }
    }
    public string SpawnName
    {
        get { return spawnPrefab.name; }
    }
    public float SpawnRate
    {
        get { return spawnRate; }
        set { spawnRate = value; }  // GAMA server can adjust spawn rate / GAMA server có thể điều chỉnh tốc độ spawn
    }

    // =========================================================================
    // LIFECYCLE
    // VÒNG ĐỜI
    // =========================================================================

    /// <summary>
    /// Initialize HP, find GameManager, reduce HP if near other pumps,
    /// register this pump with GAMA server.
    /// Khởi tạo máu, tìm GameManager, giảm máu nếu gần máy bơm khác,
    /// đăng ký máy bơm này với GAMA server.
    /// </summary>
    void Start()
    {
        // Clamp starting health to [0, maxHealth] so prefab cannot start above capacity.
        // Giới hạn máu khởi điểm trong [0, maxHealth] để prefab không vượt dung tích.
        if (maxHealth < 1) maxHealth = 1;
        if (health > maxHealth) health = maxHealth;
        if (health < 0) health = 0;
        currentHealh = health;

        // Find GameManager (may be absent in test scenes).
        // Tìm GameManager (có thể vắng trong scene test).
        GameObject gmObj = GameObject.FindGameObjectWithTag("GameManager");
        if (gmObj != null)
            GameManagerScript = gmObj.GetComponent<GameManager>();

        // Reduce HP if placed near another pump (penalty for clustering).
        // Giảm máu nếu đặt gần máy bơm khác (phạt khi dồn cụm).
        Collider[] nearbyTargets = Physics.OverlapSphere(transform.position, 5f, targetLayerMask);
        foreach (var target in nearbyTargets)
        {
            var health = target.GetComponent<ISpawner>();
            if (health != null)
            {
                if (currentHealh > 20) currentHealh -= 20;
            }
        }
        currentRate = spawnRate;

        // Register this pump with GAMA simulation server.
        // Đăng ký máy bơm này với GAMA simulation server.
        SimulationManager sm = FindObjectOfType<SimulationManager>();
        if (sm != null)
            sm.createMovePumper(gameObject);
        else
            Debug.LogWarning($"[Barrack] SimulationManager not found — skipping GAMA registration for {gameObject.name}"); 
    }

    /// <summary>
    /// Each frame: update UI, countdown spawn timer, spawn if ready.
    /// Mỗi frame: cập nhật UI, đếm ngược bộ đếm spawn, spawn nếu sẵn sàng.
    /// </summary>
    void Update()
    {
        if (!IsDead())
        {
            // Update HUD marker position.
            // Cập nhật vị trí marker HUD.
            if (GameUI.Instance != null && gameObject != null)
            {
                GameUI.Instance.UpdateConstructionPosition(gameObject);
            }

            // Spawn timer countdown.
            // Đếm ngược bộ đếm spawn.
            currentRate -= Time.deltaTime;
            if (currentRate <= 0)
            {
                // Only spawn if player is within workRadius (CanSpawn check).
                // Chỉ spawn nếu người chơi trong workRadius (kiểm tra CanSpawn).
                if (CanSpawn())
                {
                    Spawn();
                    currentRate = spawnRate;
                    // Notify GameManager to increment water count.
                    // Thông báo GameManager tăng số lượng nước.
                    if (GameManagerScript != null)
                        GameManagerScript.IncrementWaterCount();
                }
            }
        }
    }

    // =========================================================================
    // DAMAGE & DEATH
    // SÁT THƯƠNG & CHẾT
    // =========================================================================

    /// <summary>
    /// Receive damage. When HP reaches 0, notify GAMA and play broken animation.
    /// Nhận sát thương. Khi máu về 0, thông báo GAMA và phát animation hỏng.
    /// </summary>
    public void TakeDamage(int damage)
    {
        currentHealh -= damage;
        if (currentHealh <= 0)
        {
            // Notify GAMA server that this pump is destroyed.
            // Thông báo GAMA server rằng máy bơm đã bị phá hủy.
            Dictionary<string, string> args = new Dictionary<string, string> {
                {"idP", ConnectionManager.Instance.GetConnectionId()},
                {"idwp", gameObject.GetInstanceID()+"" }};

            ConnectionManager.Instance.SendExecutableAsk("delete_water_pump", args);
            animator.Play("ANIM_WaterPump_Broken");
        }
    }

    /// <summary>
    /// Destroy pump: spawn subsidence effect, destroy all nearby Ally, destroy self.
    /// Hủy máy bơm: sinh hiệu ứng sụt lún, hủy tất cả Ally lân cận, hủy bản thân.
    /// </summary>
    public void Die()
    {
        // Spawn subsidence (sinkhole) visual effect.
        // Sinh hiệu ứng sụt lún.
        if (subsidencePrefab)
        {
            Instantiate(subsidencePrefab, subsidenceSpawnPoint.position, subsidenceSpawnPoint.rotation);
        }
        // Destroy all targets within workRadius.
        // Hủy tất cả mục tiêu trong workRadius.
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, workRadius, transform.forward, Mathf.Infinity, targetLayerMask);
        foreach (RaycastHit hit in hits)
        {
            Destroy(hit.transform.gameObject);
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// Check if dead (HP <= 0). / Kiểm tra đã chết chưa (máu <= 0).
    /// </summary>
    public bool IsDead()
    {
        return currentHealh <= 0;
    }

    // =========================================================================
    // REFILL — Player grabs the pump (like shrimp grab) to add water back.
    // NẠP NƯỚC — Người chơi grab máy bơm (giống grab tôm) để bơm thêm nước.
    // =========================================================================

    /// <summary>
    /// EN: Add <paramref name="amount"/> water (HP) capped at <see cref="MaxHealth"/>.
    ///     Returns the amount actually added (0 if already full or pump destroyed).
    /// VI: Cộng <paramref name="amount"/> nước (HP), không vượt quá <see cref="MaxHealth"/>.
    ///     Trả về lượng thực sự đã cộng (0 nếu đã đầy hoặc máy bơm đã bị phá huỷ).
    /// </summary>
    public int Refill(int amount)
    {
        if (amount <= 0) return 0;
        if (IsDead()) return 0; // Không nạp lại được khi máy bơm đã bị phá huỷ.
        if (currentHealh >= maxHealth) return 0;

        int before = currentHealh;
        currentHealh = Mathf.Min(maxHealth, currentHealh + amount);
        return currentHealh - before;
    }

    // =========================================================================
    // SPAWNING
    // SINH RA NƯỚC NGỌT
    // =========================================================================

    /// <summary>
    /// Instantiate a FreshWater (Ally) at spawnPoint. Costs 1 HP if not freeSpawn.
    /// Tạo một Nước Ngọt (Ally) tại spawnPoint. Tốn 1 máu nếu không freeSpawn.
    /// </summary>
    public void Spawn()
    {
        Instantiate(spawnPrefab, spawnPoint.position, spawnPoint.rotation);
        if (animator) animator.Play("Spawn");
        // Non-free spawning costs 1 HP per spawn (pump wears out).
        // Spawn không miễn phí tốn 1 máu mỗi lần (máy bơm hao mòn).
        if (!freeSpawn) TakeDamage(1);
    }

    /// <summary>
    /// Check if there are trigger objects within workRadius (e.g., player nearby).
    /// Kiểm tra có object trigger trong workRadius không (vd: người chơi ở gần).
    /// </summary>
    public bool CanSpawn()
    {
        Collider[] nearbySpawnTriggers = Physics.OverlapSphere(transform.position, workRadius, spawnTriggerLayerMask);
        return nearbySpawnTriggers.Length > 0;
    }

    // =========================================================================
    // EDITOR GIZMOS
    // GIZMOS TRONG EDITOR
    // =========================================================================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        // Draw work radius sphere.
        // Vẽ hình cầu bán kính hoạt động.
        Gizmos.DrawWireSphere(transform.position, workRadius);
    }
}
