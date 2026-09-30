using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// Enemy - SaltyWater entity that attacks trees/structures.
// Enemy - Thực thể Nước Mặn tấn công cây cối/công trình.
//
// ROLE: Represents saltwater intrusion. Spawned by EnemySpawner, moves along
// waypoints via EnemyController, and periodically attacks nearby targets.
// VAI TRÒ: Đại diện xâm nhập mặn. Được EnemySpawner sinh ra, di chuyển dọc
// waypoint qua EnemyController, và tấn công định kỳ các mục tiêu lân cận.
//
// TAG: "Enemy"    LAYER: 7
// NEUTRALIZED BY: Ally (FreshWater) via OnTriggerEnter collision.
// BỊ TRUNG HÒA BỞI: Ally (Nước Ngọt) qua va chạm OnTriggerEnter.
// =============================================================================
public class Enemy : MonoBehaviour, IDamageable, IDamage
{
    // =========================================================================
    // SERIALIZED FIELDS — configured in Inspector / prefab.
    // CÁC TRƯỜNG SERIALIZE — cấu hình trong Inspector / prefab.
    // =========================================================================

    [Header("Basic Info")]
    [SerializeField] private string uniqueName;   // Unique ID / Mã định danh duy nhất
    [SerializeField] private int lvl;             // Level / Cấp độ

    [Header("Stats")]
    [SerializeField] private int health = 2;            // Hit points / Máu
    [SerializeField] private float moveSpeed = 2f;      // NavMeshAgent speed / Tốc độ di chuyển
    [SerializeField] private float attackInterval = 5f; // Seconds between attacks / Giây giữa các đợt tấn công
    [SerializeField] private float attackRange = 2f;    // Attack radius / Bán kính tấn công
    [SerializeField] private int attackDamage = 3;      // Damage per hit / Sát thương mỗi đòn

    [Header("Miscellaneous")]
    [SerializeField] Animator actionAnimator;    // Animator for attack/slide / Animator hành động tấn công/trượt
    [SerializeField] Animator emotionAnimator;   // Animator for emotion states / Animator trạng thái cảm xúc
    [SerializeField] LayerMask targetLayerMask;  // Which layers to attack / Layer nào bị tấn công

    // =========================================================================
    // SHADER PROPERTY IDs
    // =========================================================================
    private static readonly int ShadowColorID = Shader.PropertyToID("_Shadow_Color");
    private static readonly Color SaltyColor  = new Color(0.106f, 0.184f, 0.878f, 1f);  // #1B2FE0
    private static readonly Color NeutralColor = new Color(0.298f, 0.867f, 0.824f, 1f);  // #4CDDD2

    // =========================================================================
    // RUNTIME STATE
    // TRẠNG THÁI RUNTIME
    // =========================================================================
    private int currentHealh;       // Current HP / Máu hiện tại
    private float currentInterval;  // Countdown to next attack / Đếm ngược đến đợt tấn công kế
    private Renderer[] bodyRenderers; // Cached renderers for _Shadow_Color / Renderer đã cache cho _Shadow_Color

    // =========================================================================
    // PUBLIC PROPERTIES
    // THUỘC TÍNH CÔNG KHAI
    // =========================================================================
    public int Health
    {
        get { return currentHealh; }
    }
    public float Range
    {
        get { return attackRange; }
    }
    public int Damage
    {
        get { return attackDamage; }
    }

    // =========================================================================
    // LIFECYCLE
    // VÒNG ĐỜI
    // =========================================================================

    /// <summary>
    /// Initialize HP and sync NavMeshAgent speed.
    /// Khởi tạo máu và đồng bộ tốc độ NavMeshAgent.
    /// </summary>
    void Start()
    {
        currentHealh = health;
        currentInterval = attackInterval;
        var navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent) navAgent.speed = moveSpeed;

        // Set SaltyWater shadow color on spawn.
        // Đặt màu bóng Nước Mặn khi sinh ra.
        bodyRenderers = GetComponentsInChildren<Renderer>();
        SetShadowColor(SaltyColor);
    }

    /// <summary>
    /// Each frame: update UI position, countdown attack timer.
    /// Mỗi frame: cập nhật vị trí UI, đếm ngược bộ đếm tấn công.
    /// </summary>
    void Update()
    {
        if (IsDead()) return;

        // Update HUD marker for this enemy.
        // Cập nhật marker HUD cho enemy này.
        if (GameUI.Instance != null && gameObject != null)
        {
            GameUI.Instance.UpdateConstructionPosition(gameObject);
        }

        // Attack timer countdown.
        // Đếm ngược bộ đếm tấn công.
        currentInterval -= Time.deltaTime;
        if (currentInterval <= 0)
        {
            Attack();
            currentInterval = attackInterval;
        }
    }

    // =========================================================================
    // DAMAGE & DEATH
    // SÁT THƯƠNG & CHẾT
    // =========================================================================

    /// <summary>
    /// Receive damage. If HP <= 0, die and change tag/layer to "Water".
    /// Nhận sát thương. Nếu máu <= 0, chết và đổi tag/layer thành "Water".
    /// </summary>
    public void TakeDamage(int damage)
    {
        // Play hurt emotion animation.
        // Phát animation cảm xúc bị tấn công.
        if (emotionAnimator) emotionAnimator.Play("Attacked");
        currentHealh -= damage;

        // Gradually transition shadow color based on remaining HP.
        // Chuyển màu bóng dần dần theo HP còn lại.
        float hpRatio = Mathf.Clamp01((float)currentHealh / health);
        Color targetColor = Color.Lerp(NeutralColor, SaltyColor, hpRatio);
        StartCoroutine(LerpShadowColor(targetColor, 0.5f));

        if (currentHealh <= 0)
        {
            // Remove HUD marker.
            // Xóa marker HUD.
            if (GameUI.Instance != null && gameObject != null)
            {
                GameUI.Instance.DeletePlayer(gameObject);
            }
            Die();
            // Play neutralized emotion.
            // Phát animation bị trung hòa.
            if (emotionAnimator) emotionAnimator.Play("Neutralized");
        }
    }

    /// <summary>
    /// Mark as dead: change tag to "Water", change layer, increment kill count.
    /// NOTE: Does NOT Destroy — EnemyController handles that at end of path.
    /// Đánh dấu chết: đổi tag sang "Water", đổi layer, tăng số lượng tiêu diệt.
    /// GHI CHÚ: KHÔNG Destroy — EnemyController xử lý khi đến cuối đường.
    /// </summary>
    public void Die()
    {
        gameObject.tag = "Water";
        gameObject.layer = LayerMask.NameToLayer("Water");
        if (StatisticsManager.Instance != null)
            StatisticsManager.Instance.IncreaseEnemyKillCount();

        // Revert to neutral color when neutralized.
        // Chuyển về màu trung hòa khi bị trung hòa.
        SetShadowColor(NeutralColor);

        Destroy(gameObject, 3f);
    }

    /// <summary>
    /// Check if dead (HP <= 0). / Kiểm tra đã chết chưa (máu <= 0).
    /// </summary>
    public bool IsDead()
    {
        return (currentHealh <= 0);
    }

    // =========================================================================
    // ATTACK LOGIC
    // LOGIC TẤN CÔNG
    // =========================================================================

    /// <summary>
    /// Play slide animation when passing through an area.
    /// Phát animation trượt khi đi qua một khu vực.
    /// </summary>
    public void PassThrough()
    {
        if (actionAnimator) actionAnimator.Play("Slide");
    }

    /// <summary>
    /// Find all IDamageable targets within attackRange and deal damage.
    /// Tìm tất cả mục tiêu IDamageable trong attackRange và gây sát thương.
    /// </summary>
    private void Attack()
    {
        // Detect targets in range on the specified layer.
        // Phát hiện mục tiêu trong tầm trên layer chỉ định.
        Collider[] nearbyTargets = Physics.OverlapSphere(transform.position, attackRange, targetLayerMask);

        foreach (var target in nearbyTargets)
        {
            var health = target.GetComponent<IDamageable>();
            if (health != null) DealDamage(health);
        }
        if (actionAnimator) actionAnimator.Play("Attack");
    }

    public bool HasValidTarget(GameObject target)
    {
        Debug.Log(target.name);
        return true;
    }

    // Tick counter for damage throttling (only deal damage every 2 ticks).
    // Bộ đếm tick để giới hạn sát thương (chỉ gây sát thương mỗi 2 tick).
    int tick = 0;

    /// <summary>
    /// Deal damage to target, throttled: only every 2nd call actually damages.
    /// Gây sát thương lên mục tiêu, giới hạn: chỉ mỗi lần gọi thứ 2 mới gây damage.
    /// </summary>
    public void DealDamage(IDamageable target)
    {
        tick++;
        if (tick >= 2)
        {
            tick = 0;
            target.TakeDamage(attackDamage);
        }
    }

    // =========================================================================
    // EDITOR GIZMOS
    // GIZMOS TRONG EDITOR
    // =========================================================================
    // =========================================================================
    // SHADOW COLOR
    // MÀU BÓNG
    // =========================================================================

    private Color currentShadowColor;

    private void SetShadowColor(Color color)
    {
        currentShadowColor = color;
        if (bodyRenderers == null) return;
        foreach (var rend in bodyRenderers)
        {
            if (rend == null) continue;
            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty(ShadowColorID))
                    mat.SetColor(ShadowColorID, color);
            }
        }
    }

    private IEnumerator LerpShadowColor(Color target, float duration)
    {
        Color start = currentShadowColor;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetShadowColor(Color.Lerp(start, target, elapsed / duration));
            yield return null;
        }
        SetShadowColor(target);
    }

    // =========================================================================
    // EDITOR GIZMOS
    // GIZMOS TRONG EDITOR
    // =========================================================================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        // Draw attack range sphere.
        // Vẽ hình cầu tầm tấn công.
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
