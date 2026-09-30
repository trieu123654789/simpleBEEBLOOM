using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// =============================================================================
// Ally - FreshWater entity that seeks and neutralizes SaltyWater (Enemy).
// Ally - Thực thể Nước Ngọt tìm kiếm và trung hòa Nước Mặn (Enemy).
//
// ROLE: Spawned by Barrack (Water Pump). Uses NavMeshAgent to pathfind toward
// the nearest Enemy. On contact (OnTriggerEnter), deals damage to Enemy and
// takes damage itself. Self-destructs after 5s without a target.
//
// VAI TRÒ: Được sinh ra bởi Barrack (Máy Bơm). Dùng NavMeshAgent để tìm đường
// đến Enemy gần nhất. Khi chạm (OnTriggerEnter), gây sát thương lên Enemy và
// bản thân cũng nhận sát thương. Tự hủy sau 5 giây không có mục tiêu.
//
// TAG: "Ally"    LAYER: 4 (Water)
// REQUIRES: NavMeshAgent component, BoxCollider (IsTrigger=true), Rigidbody
// CẦN: NavMeshAgent component, BoxCollider (IsTrigger=true), Rigidbody
//
// SPAWNED BY: Barrack.Spawn()
// TARGETS: Enemy (SaltyWater) on Layer 7
// SINH BỞI: Barrack.Spawn()
// MỤC TIÊU: Enemy (Nước Mặn) trên Layer 7
// =============================================================================
public class Ally : MonoBehaviour, IDamageable, IDamage
{
    // =========================================================================
    // SERIALIZED FIELDS — configured in Inspector / prefab.
    // CÁC TRƯỜNG SERIALIZE — cấu hình trong Inspector / prefab.
    // =========================================================================

    [Header("Basic Info")]
    [SerializeField] private string uniqueName;  // Unique ID / Mã định danh duy nhất
    [SerializeField] private int lvl;            // Level / Cấp độ
    
    [Header("Stats")]
    [SerializeField] private int health = 2;          // Hit points / Máu
    [SerializeField] private float moveSpeed = 2f;    // Movement speed / Tốc độ di chuyển
    [SerializeField] private float detectRange = 30f; // Enemy detection radius / Bán kính phát hiện Enemy
    [SerializeField] private int attackDamage = 1;    // Damage dealt on contact / Sát thương khi chạm

    [Header("Miscellaneous")]
    [SerializeField] private LayerMask targetLayerMask; // Layer mask for Enemy detection / Layer mask phát hiện Enemy
    [SerializeField] private Animator animator;          // Animator for disappear effect / Animator hiệu ứng biến mất

    // =========================================================================
    // RUNTIME STATE
    // TRẠNG THÁI RUNTIME
    // =========================================================================
    private int currentHealh;        // Current HP / Máu hiện tại
    private Transform target;        // Current chase target / Mục tiêu đang truy đuổi
    private NavMeshAgent navAgent;   // Cached NavMeshAgent / NavMeshAgent đã cache
    private float timeLife = 5.0f;   // Self-destruct timer when idle / Bộ đếm tự hủy khi rảnh
    private bool useNavMesh = true;  // Whether NavMesh pathfinding is available / NavMesh có khả dụng không

    private static readonly int ShadowColorID = Shader.PropertyToID("_Shadow_Color");
    private static readonly Color FreshColor = new Color(0.298f, 0.867f, 0.824f, 1f);  // #4CDDD2

    // =========================================================================
    // PUBLIC PROPERTIES
    // THUỘC TÍNH CÔNG KHAI
    // =========================================================================
    public int Health { 
        get { return currentHealh; } 
    }
    public float Range { 
        get { return detectRange; }
    }
    public int Damage { 
        get { return attackDamage; } 
    }

    // =========================================================================
    // LIFECYCLE
    // VÒNG ĐỜI
    // =========================================================================

    /// <summary>
    /// Initialize HP, warp to nearest NavMesh position, start target scanning.
    /// Khởi tạo máu, warp đến vị trí NavMesh gần nhất, bắt đầu quét mục tiêu.
    /// </summary>
    void Start()
    {
        currentHealh = health;
        navAgent = GetComponent<NavMeshAgent>();

        // Warp Ally to nearest NavMesh position (pump may be above NavMesh).
        // Warp Ally xuống vị trí NavMesh gần nhất (máy bơm có thể cao hơn NavMesh).
        if (navAgent != null)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 20f, NavMesh.AllAreas))
            {
                navAgent.enabled = false;
                transform.position = hit.position;
                navAgent.enabled = true;
                navAgent.Warp(hit.position);
                navAgent.speed = moveSpeed;
                useNavMesh = true;
            }
            else
            {
                // No NavMesh nearby — fall back to direct transform movement.
                // Không có NavMesh gần — dùng di chuyển transform trực tiếp.
                Debug.LogWarning($"[Ally] No NavMesh within 20m of {transform.position}, using fallback movement");
                navAgent.enabled = false;
                useNavMesh = false;
            }
        }
        else
        {
            useNavMesh = false;
        }

        // Set FreshWater shadow color.
        // Đặt màu bóng Nước Ngọt.
        foreach (var rend in GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty(ShadowColorID))
                    mat.SetColor(ShadowColorID, FreshColor);
            }
        }

        // Scan for enemies every 0.5 seconds.
        // Quét tìm enemy mỗi 0.5 giây.
        InvokeRepeating("FindTarget", 0f, .5f);
    }

    /// <summary>
    /// Each frame: chase target or countdown self-destruct if idle.
    /// Mỗi frame: truy đuổi mục tiêu hoặc đếm ngược tự hủy nếu rảnh.
    /// </summary>
    void Update()
    {
        if (IsDead()) return;

        if (target)
        {
            // Reset idle timer while chasing.
            // Reset bộ đếm rảnh khi đang truy đuổi.
            timeLife = 5.0f;
            MoveToTarget();
        }
        else
        {
            // No target → stop moving, countdown to self-destruct.
            // Không có target → dừng di chuyển, đếm ngược tự hủy.
            if (useNavMesh && navAgent != null && navAgent.enabled)
                navAgent.ResetPath();

            timeLife -= Time.deltaTime;
            if (timeLife <= 0)
            {
                Die();
            }
        }  
    }

    // =========================================================================
    // MOVEMENT
    // DI CHUYỂN
    // =========================================================================

    /// <summary>
    /// Move toward current target using NavMesh pathfinding or fallback.
    /// Di chuyển đến mục tiêu bằng NavMesh pathfinding hoặc fallback.
    /// </summary>
    private void MoveToTarget()
    {
        if (useNavMesh && navAgent != null && navAgent.enabled)
        {
            // Sample target position onto NavMesh (target may not be on NavMesh).
            // Sample vị trí target xuống NavMesh (target có thể không nằm trên NavMesh).
            Vector3 dest = target.position;
            if (NavMesh.SamplePosition(target.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                dest = hit.position;

            navAgent.SetDestination(dest);
        }
        else
        {
            // Fallback: move straight toward target (no pathfinding).
            // Fallback: di chuyển thẳng đến target (không pathfinding).
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    // =========================================================================
    // COLLISION — NEUTRALIZATION
    // VA CHẠM — TRUNG HÒA
    // =========================================================================

    /// <summary>
    /// When touching a valid Enemy: deal damage to it, take 1 damage ourselves.
    /// This is the core "neutralization" mechanic — FreshWater dissolves SaltyWater.
    /// Khi chạm Enemy hợp lệ: gây sát thương lên nó, bản thân nhận 1 sát thương.
    /// Đây là cơ chế "trung hòa" cốt lõi — Nước Ngọt hòa tan Nước Mặn.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (HasValidTarget(other.gameObject) && !IsDead())
        {
            Enemy enemy = other.GetComponent<Enemy>();
            enemy.TakeDamage(attackDamage);
            TakeDamage(1);
        }
    }

    // =========================================================================
    // TARGET FINDING
    // TÌM MỤC TIÊU
    // =========================================================================

    /// <summary>
    /// Find the closest living Enemy. Two-pass approach:
    /// 1. Physics.OverlapSphere with targetLayerMask (fast, precise).
    /// 2. Fallback: FindGameObjectsWithTag("Enemy") if nothing found.
    ///
    /// Tìm Enemy sống gần nhất. Hai bước:
    /// 1. Physics.OverlapSphere với targetLayerMask (nhanh, chính xác).
    /// 2. Fallback: FindGameObjectsWithTag("Enemy") nếu không tìm thấy.
    /// </summary>
    void FindTarget()
    {
        // Pass 1: OverlapSphere on targetLayerMask.
        // Bước 1: OverlapSphere trên targetLayerMask.
        Collider[] nearbyTargets = Physics.OverlapSphere(transform.position, detectRange, targetLayerMask);

        float closestDistance = Mathf.Infinity;
        GameObject closestTarget = null;

        foreach (Collider col in nearbyTargets)
        {
            if (col == null) continue;
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null && !enemy.IsDead())
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestTarget = col.gameObject;
                }
            }
        }

        // Pass 2: Fallback — search by Tag "Enemy" if OverlapSphere found nothing.
        // Bước 2: Fallback — tìm bằng Tag "Enemy" nếu OverlapSphere không tìm thấy.
        if (closestTarget == null)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
                if (enemy == null || !enemy.activeSelf) continue;
                Enemy enemyScript = enemy.GetComponent<Enemy>();
                if (enemyScript != null && !enemyScript.IsDead())
                {
                    float dist = Vector3.Distance(transform.position, enemy.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestTarget = enemy;
                    }
                }
            }
        }

        target = closestTarget != null ? closestTarget.transform : null;
    }

    // =========================================================================
    // DAMAGE & DEATH
    // SÁT THƯƠNG & CHẾT
    // =========================================================================

    /// <summary>
    /// Receive damage. Die if HP reaches 0.
    /// Nhận sát thương. Chết nếu máu về 0.
    /// </summary>
    public void TakeDamage(int damage)
    {
        currentHealh -= damage;
        if (currentHealh <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Play disappear animation and destroy after 2 seconds.
    /// Phát animation biến mất và hủy sau 2 giây.
    /// </summary>
    public void Die()
    {
        if (animator) animator.Play("Disappear");
        Destroy(gameObject, 2f);
    }

    /// <summary>
    /// Check if dead (HP <= 0). / Kiểm tra đã chết chưa (máu <= 0).
    /// </summary>
    public bool IsDead()
    {
        return currentHealh <= 0;
    }

    /// <summary>
    /// Check if a GameObject is on a valid target layer (matches targetLayerMask).
    /// Kiểm tra GameObject có nằm trên layer hợp lệ (khớp targetLayerMask) không.
    /// </summary>
    public bool HasValidTarget(GameObject target)
    {
        return (targetLayerMask == (targetLayerMask | (1 << target.layer)));
    }

    /// <summary>
    /// Deal damage to an IDamageable target.
    /// Gây sát thương lên mục tiêu IDamageable.
    /// </summary>
    public void DealDamage(IDamageable target)
    {
        target.TakeDamage(attackDamage);
    }
}
