using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// TreeBarrier - Cây trồng chặn nước mặn xâm nhập nội đồng.
// TreeBarrier - Planted tree that traps one saltwater enemy from moving inland.
//
// CÁCH HOẠT ĐỘNG:
// - Khi enemy (tag "Enemy") đi vào vùng trigger → cây "bắt" enemy đó.
// - Enemy bị kéo về gốc cây, đứng tại chỗ.
// - Mỗi cây chỉ giữ được 1 con mặn (1:1).
// - Cây từ từ mất máu khi đang giữ enemy (bị nước mặn ăn mòn).
// - Màu cây thay đổi theo HP: xanh (full) → vàng (nửa) → nâu (sắp chết).
// - Khi cây chết → enemy được thả ra, tiếp tục di chuyển.
// - Khi enemy chết trước → cây được giải phóng, sẵn sàng bắt con khác.
//
// SETUP:
// 1. Gắn script này vào prefab cây trồng
// 2. Thêm SphereCollider (isTrigger=true) làm vùng bắt
// 3. Đặt tag cây là "Construction" hoặc layer phù hợp
// 4. Tích hợp vào BuildSystemManager như 1 ConstructionSO mới (index 3)
// =============================================================================
public class TreeBarrier : MonoBehaviour, IDamageable
{
    // =========================================================================
    // CONFIGURATION
    // =========================================================================

    [Header("Stats / Chỉ số")]
    [Tooltip("Máu cây khi mới trồng")]
    [SerializeField] private int maxHealth = 5;

    [Tooltip("Sát thương mỗi giây khi đang giữ enemy (bị nước mặn ăn mòn)")]
    [SerializeField] private float corrosionDamagePerSecond = 0.5f;

    [Header("Visual")]
    [SerializeField] private Animator animator;

    [Header("Đổi màu theo HP")]
    [Tooltip("Màu cây khi đầy máu")]
    [SerializeField] private Color healthyColor = Color.white;
    [Tooltip("Màu cây khi hết máu")]
    [SerializeField] private Color deadColor = new Color(0.4f, 0.25f, 0.1f, 1f);
    [Tooltip("Scale nhỏ nhất khi sắp chết (% so với gốc)")]
    [Range(0.5f, 1f)]
    [SerializeField] private float minScale = 0.8f;

    [Header("Trap Settings / Bẫy")]
    [Tooltip("Bán kính phát hiện enemy (SphereCollider trigger)")]
    [SerializeField] private float trapRadius = 3f;
    [Tooltip("Tốc độ kéo enemy về gốc cây (units/s)")]
    [SerializeField] private float enemyPullSpeed = 3f;

    [Header("Growth Stages / Giai đoạn lớn lên")]
    [Tooltip("Thời gian (giây) để cây lớn hoàn toàn: 0..0.5*total = Sapling (1/3), 0.5..1*total = Small (1/2), >total = Big (1.0).")]
    [SerializeField] private float fullGrowthTime = 20f;
    [Tooltip("Scale cây non (Sapling) so với scale gốc.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float saplingScale = 0.333f;
    [Tooltip("Scale cây nhỏ (Small) so với scale gốc.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float smallScale = 0.5f;
    [Tooltip("Số enemy tối đa cây có thể giữ ở giai đoạn Sapling / Small / Big.")]
    [SerializeField] private int saplingCapacity = 0;
    [SerializeField] private int smallCapacity = 1;
    [SerializeField] private int bigCapacity = 2;

    public enum GrowthStage { Sapling, Small, Big }

    // =========================================================================
    // RUNTIME STATE
    // =========================================================================
    private int currentHealth;
    private float corrosionAccumulator;
    private readonly List<TrappedEntry> trappedEnemies = new List<TrappedEntry>();
    private float plantedTime;
    private GrowthStage currentStage = GrowthStage.Sapling;

    private struct TrappedEntry
    {
        public GameObject obj;
        public EnemyController controller;
    }

    // Visual state
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;
    private Vector3 _initialScale;
    private static readonly string[] _colorProps = { "_BaseColor", "_Color", "_Tint", "_TintColor" };

    // =========================================================================
    // PUBLIC PROPERTIES
    // =========================================================================
    public int Health => currentHealth;
    public bool IsTrapping => trappedEnemies.Count > 0;
    public GrowthStage Stage => currentStage;
    public int TrapCapacity => GetCapacityForStage(currentStage);

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    void Start()
    {
        currentHealth = maxHealth;
        plantedTime = Time.time;
        currentStage = GrowthStage.Sapling;

        // Tự tạo / cấu hình trigger collider
        SphereCollider trigger = GetComponent<SphereCollider>();
        if (trigger == null)
            trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = trapRadius;
        trigger.center = Vector3.zero;

        // Cần Rigidbody (kinematic) để OnTriggerEnter hoạt động.
        // Unity yêu cầu ít nhất 1 trong 2 object phải có Rigidbody.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Visual setup
        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();
        _initialScale = transform.localScale;

        // Màu ban đầu (full HP) + scale Sapling (1/3)
        UpdateVisuals();

        // =========================================================================
        // GAMA REGISTRATION — Đăng ký với GAMA server tương tự WaterPump/Barrack.
        // =========================================================================
        SimulationManager sm = FindObjectOfType<SimulationManager>();
        if (sm != null)
            sm.createTreeBarrier(gameObject);
        else
            Debug.LogWarning($"[TreeBarrier] SimulationManager not found — skipping GAMA registration for {gameObject.name}");
    }

    void Update()
    {
        if (IsDead()) return;

        // Cập nhật giai đoạn lớn lên theo thời gian
        UpdateGrowthStage();

        // Cập nhật HUD
        if (GameUI.Instance != null)
            GameUI.Instance.UpdateConstructionPosition(gameObject);

        // Dọn những entry không còn hợp lệ
        PruneTrappedList();

        // Nếu còn sức chứa, quét enemy mới (fallback OverlapSphere)
        if (trappedEnemies.Count < TrapCapacity)
        {
            ScanForEnemyInRange();
        }

        // Giữ enemy: kéo về gốc cây + ăn mòn cây
        if (trappedEnemies.Count > 0)
        {
            for (int i = 0; i < trappedEnemies.Count; i++)
            {
                var entry = trappedEnemies[i];
                if (entry.obj == null) continue;
                Vector3 pullTarget = transform.position;
                pullTarget.y = entry.obj.transform.position.y;
                entry.obj.transform.position = Vector3.MoveTowards(
                    entry.obj.transform.position, pullTarget, enemyPullSpeed * Time.deltaTime);
            }

            // Ăn mòn cây (không nhân số enemy để độ khó ổn định)
            corrosionAccumulator += corrosionDamagePerSecond * Time.deltaTime;
            if (corrosionAccumulator >= 1f)
            {
                int dmg = Mathf.FloorToInt(corrosionAccumulator);
                corrosionAccumulator -= dmg;
                TakeDamage(dmg);
            }
        }
    }

    /// <summary>Xác định stage hiện tại theo thời gian đã trồng. Cập nhật visual khi đổi stage.</summary>
    private void UpdateGrowthStage()
    {
        float elapsed = Time.time - plantedTime;
        GrowthStage newStage;
        if (elapsed >= fullGrowthTime) newStage = GrowthStage.Big;
        else if (elapsed >= fullGrowthTime * 0.5f) newStage = GrowthStage.Small;
        else newStage = GrowthStage.Sapling;

        if (newStage != currentStage)
        {
            currentStage = newStage;
            UpdateVisuals();
            Debug.Log($"[TreeBarrier] {name} grew to stage {currentStage} (capacity={TrapCapacity})");
        }
    }

    private void PruneTrappedList()
    {
        for (int i = trappedEnemies.Count - 1; i >= 0; i--)
        {
            var entry = trappedEnemies[i];
            bool drop = false;
            if (entry.obj == null || !entry.obj.activeInHierarchy)
            {
                drop = true;
            }
            else
            {
                var enemy = entry.obj.GetComponent<Enemy>();
                if (enemy != null && enemy.IsDead()) drop = true;
            }
            if (drop)
            {
                if (entry.controller != null) entry.controller.SetTrapped(false);
                trappedEnemies.RemoveAt(i);
            }
        }
    }

    // =========================================================================
    // TRAP LOGIC — Bắt enemy
    // =========================================================================

    /// <summary>
    /// Fallback scan: dùng OverlapSphere mỗi frame để phát hiện enemy trong vùng.
    /// Cần thiết vì Transform.MoveTowards (fallback movement) không trigger physics.
    /// </summary>
    private void ScanForEnemyInRange()
    {
        if (trappedEnemies.Count >= TrapCapacity) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, trapRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            var enemy = hit.GetComponent<Enemy>();
            if (enemy == null || enemy.IsDead()) continue;

            var controller = hit.GetComponent<EnemyController>();
            if (controller == null || controller.IsTrapped) continue;

            if (IsAlreadyTrapped(hit.gameObject)) continue;

            TrapEnemy(hit.gameObject, controller);
            if (trappedEnemies.Count >= TrapCapacity) return;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsDead()) return;
        if (trappedEnemies.Count >= TrapCapacity) return; // Đã đầy

        if (!other.CompareTag("Enemy")) return;

        var enemy = other.GetComponent<Enemy>();
        if (enemy == null || enemy.IsDead()) return;

        var controller = other.GetComponent<EnemyController>();
        if (controller == null) return;

        if (IsAlreadyTrapped(other.gameObject)) return;

        TrapEnemy(other.gameObject, controller);
    }

    private bool IsAlreadyTrapped(GameObject obj)
    {
        for (int i = 0; i < trappedEnemies.Count; i++)
            if (trappedEnemies[i].obj == obj) return true;
        return false;
    }

    private void TrapEnemy(GameObject enemyObj, EnemyController controller)
    {
        trappedEnemies.Add(new TrappedEntry { obj = enemyObj, controller = controller });
        controller.SetTrapped(true);
        Debug.Log($"[TreeBarrier] {name} trapped {enemyObj.name} ({trappedEnemies.Count}/{TrapCapacity})");
    }

    /// <summary>
    /// Thả tất cả enemy khi cây chết.
    /// </summary>
    private void ReleaseAllEnemies()
    {
        for (int i = 0; i < trappedEnemies.Count; i++)
        {
            if (trappedEnemies[i].controller != null)
                trappedEnemies[i].controller.SetTrapped(false);
        }
        trappedEnemies.Clear();
        Debug.Log($"[TreeBarrier] {name} released all enemies");
    }

    private int GetCapacityForStage(GrowthStage stage)
    {
        switch (stage)
        {
            case GrowthStage.Sapling: return saplingCapacity;
            case GrowthStage.Small: return smallCapacity;
            case GrowthStage.Big: return bigCapacity;
            default: return 0;
        }
    }

    private float GetScaleForStage(GrowthStage stage)
    {
        switch (stage)
        {
            case GrowthStage.Sapling: return saplingScale;
            case GrowthStage.Small: return smallScale;
            case GrowthStage.Big: return 1f;
            default: return 1f;
        }
    }

    // =========================================================================
    // IDAMAGEABLE
    // =========================================================================

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        UpdateVisuals();

        if (currentHealth <= 0)
            Die();
    }

    public void Die()
    {
        currentHealth = 0;

        // Thả enemy trước khi chết
        ReleaseAllEnemies();

        // =========================================================================
        // GAMA NOTIFICATION — Thông báo GAMA server cây đã chết.
        // =========================================================================
        SimulationManager sm = FindObjectOfType<SimulationManager>();
        if (sm != null)
            sm.deleteTreeBarrier(gameObject);

        if (animator != null)
            animator.Play("Tree_Die");

        // Hoàn trả số lượng cây cho BuildSystem (index 3)
        var remover = GetComponent<ConstructionRemover>();
        if (remover != null && remover.buildSystemManager != null)
            remover.buildSystemManager.Constructions[3].IncreaseQuantity();

        // Xóa HUD
        if (GameUI.Instance != null)
            GameUI.Instance.DeletePlayer(gameObject);

        Debug.Log($"[TreeBarrier] {name} died");
        Destroy(gameObject, 2f);
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    // =========================================================================
    // VISUAL — Đổi màu + scale theo HP
    // =========================================================================

    private void UpdateVisuals()
    {
        float hpRatio = (maxHealth > 0) ? (float)currentHealth / maxHealth : 0f;

        // Lerp màu: healthyColor (full HP) → deadColor (0 HP)
        Color currentColor = Color.Lerp(deadColor, healthyColor, hpRatio);

        foreach (var r in _renderers)
        {
            if (r == null || r.sharedMaterial == null) continue;
            string prop = GetColorProp(r);
            if (prop == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(prop, currentColor);
            r.SetPropertyBlock(_mpb);
        }

        // Scale = stage scale * (một chút shrink theo HP để báo hiệu sắp chết)
        float stageFactor = GetScaleForStage(currentStage);
        float hpFactor = Mathf.Lerp(minScale, 1f, hpRatio);
        transform.localScale = _initialScale * stageFactor * hpFactor;

        // Animation theo ngưỡng HP
        if (animator != null)
        {
            if (hpRatio > 0.5f)
                animator.Play("Tree_Good");
            else if (hpRatio > 0f)
                animator.Play("Tree_Bad");
        }
    }

    private string GetColorProp(Renderer r)
    {
        foreach (var prop in _colorProps)
            if (r.sharedMaterial.HasProperty(prop)) return prop;
        return null;
    }

    // =========================================================================
    // GIZMOS
    // =========================================================================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, trapRadius);
    }
}
