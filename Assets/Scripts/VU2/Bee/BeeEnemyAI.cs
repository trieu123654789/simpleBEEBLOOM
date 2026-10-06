// =============================================================================
// BeeEnemyAI - Ong dịch: Patrol → Chase → Attack → Retreat.
// BeeEnemyAI - Enemy bee AI: Patrol → Chase → Attack → Retreat.
//
// TRẠNG THÁI / STATES
//   Patrol  : bay ngẫu nhiên trong zone, tìm player
//   Chase   : player phát hiện → đuổi theo
//   Attack  : đủ gần → tấn công (gây stamina drain cho player)
//   Stunned : ong bị ong player phản công / thoát khỏi hive zone
//   Dead    : đã bị tiêu diệt
//
// TÍCH HỢP / INTEGRATION
//   BeeGameStats.SetDanger(true) khi Chase/Attack
//   BeeController: giảm stamina qua public field (không inject trực tiếp,
//     thay vào đó phát event để BeeController đọc)
//
// AI MODEL (đơn giản, không cần NavMesh)
//   Dùng chuyển động vector thuần: Patrol dùng waypoints ngẫu nhiên,
//   Chase dùng Seek steering, Attack dùng OverlapSphere.
// =============================================================================
using UnityEngine;

[DisallowMultipleComponent]
public class BeeEnemyAI : MonoBehaviour
{
    // =========================================================================
    // SETTINGS / CÀI ĐẶT
    // =========================================================================
    [Header("Detection / Phát hiện")]
    [Tooltip("Bán kính phát hiện player (m).")]
    [Range(2f, 20f)]
    public float detectRange = 8f;

    [Tooltip("Bán kính tấn công player (m).")]
    [Range(0.5f, 3f)]
    public float attackRange = 1.5f;

    [Tooltip("Bán kính patrol xung quanh spawnPoint.")]
    [Range(3f, 15f)]
    public float patrolRadius = 6f;

    [Header("Movement / Di chuyển")]
    [Tooltip("Tốc độ bay khi patrol (m/s).")]
    public float patrolSpeed = 2f;

    [Tooltip("Tốc độ bay khi đuổi (m/s).")]
    public float chaseSpeed = 5f;

    [Tooltip("Tốc độ xoay đầu (độ/giây).")]
    public float turnSpeed = 120f;

    [Header("Attack / Tấn công")]
    [Tooltip("Stamina drain mỗi giây khi bị tấn công (% stamina/s của player).")]
    [Range(5f, 40f)]
    public float staminaDrainPerSecond = 20f;

    [Tooltip("Cooldown giữa mỗi đợt tấn công (giây).")]
    [Range(0.5f, 3f)]
    public float attackCooldown = 1.2f;

    [Tooltip("Thời gian stun sau khi bị phản công (giây).")]
    [Range(1f, 5f)]
    public float stunDuration = 2.5f;

    [Tooltip("Khoảng cách rút lui sau khi bị stun (m).")]
    public float retreatDistance = 10f;

    [Header("Patrol Waypoints")]
    [Tooltip("Thời gian đến đích patrol trước khi chọn điểm mới (giây).")]
    [Range(2f, 8f)]
    public float waypointTimeout = 4f;

    // =========================================================================
    // REFERENCES / THAM CHIẾU
    // =========================================================================
    [Header("References")]
    [Tooltip("BeeGameStats của player để báo nguy hiểm.")]
    public BeeGameStats playerStats;

    [Tooltip("BeeController của player để gây stamina drain.")]
    public BeeController playerBee;

    [Header("Visual / Âm thanh")]
    public ParticleSystem attackVFX;
    public AudioSource buzzAudio;
    public AudioSource attackAudio;

    // =========================================================================
    // ENUMS
    // =========================================================================
    public enum AIState { Patrol, Chase, Attack, Stunned, Dead }

    // =========================================================================
    // PUBLIC STATE
    // =========================================================================
    public AIState State { get; private set; } = AIState.Patrol;

    // =========================================================================
    // EVENTS
    // =========================================================================
    /// <summary>Phát khi ong dịch chết.</summary>
    public System.Action OnDied;

    // =========================================================================
    // INTERNAL
    // =========================================================================
    private Vector3 _spawnPoint;
    private Vector3 _patrolTarget;
    private float _patrolTimer;
    private float _attackCooldownTimer;
    private float _stunTimer;
    private Transform _playerTransform;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        _spawnPoint = transform.position;

        if (playerBee == null) playerBee = FindObjectOfType<BeeController>();
        if (playerStats == null && playerBee != null)
            playerStats = playerBee.GetComponent<BeeGameStats>();

        if (playerBee != null) _playerTransform = playerBee.transform;

        PickNewPatrolTarget();
    }

    private void Update()
    {
        switch (State)
        {
            case AIState.Patrol:  UpdatePatrol();  break;
            case AIState.Chase:   UpdateChase();   break;
            case AIState.Attack:  UpdateAttack();  break;
            case AIState.Stunned: UpdateStunned(); break;
        }

        // Âm thanh bầy vo ve
        UpdateBuzz();
    }

    // =========================================================================
    // UpdatePatrol
    // =========================================================================
    private void UpdatePatrol()
    {
        MoveTowards(_patrolTarget, patrolSpeed);

        _patrolTimer -= Time.deltaTime;
        float distToTarget = Vector3.Distance(transform.position, _patrolTarget);
        if (distToTarget < 1f || _patrolTimer <= 0f)
            PickNewPatrolTarget();

        // Chuyển sang chase nếu thấy player
        if (CanSeePlayer(detectRange))
            SetState(AIState.Chase);
    }

    // =========================================================================
    // UpdateChase
    // =========================================================================
    private void UpdateChase()
    {
        if (_playerTransform == null) { SetState(AIState.Patrol); return; }

        float dist = Vector3.Distance(transform.position, _playerTransform.position);

        if (dist <= attackRange)
        {
            SetState(AIState.Attack);
            return;
        }

        if (!CanSeePlayer(detectRange * 1.5f))
        {
            SetState(AIState.Patrol);
            return;
        }

        MoveTowards(_playerTransform.position, chaseSpeed);
        playerStats?.SetDanger(true);
    }

    // =========================================================================
    // UpdateAttack
    // =========================================================================
    private void UpdateAttack()
    {
        if (_playerTransform == null) { SetState(AIState.Patrol); return; }

        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        if (dist > attackRange * 1.3f)
        {
            SetState(AIState.Chase);
            return;
        }

        // Giữ khoảng cách tấn công
        MoveTowards(_playerTransform.position, chaseSpeed * 0.5f);
        playerStats?.SetDanger(true);

        // Drain stamina player
        _attackCooldownTimer -= Time.deltaTime;
        if (_attackCooldownTimer <= 0f)
        {
            _attackCooldownTimer = attackCooldown;
            PerformAttack();
        }
    }

    // =========================================================================
    // PerformAttack
    // =========================================================================
    private void PerformAttack()
    {
        if (playerBee == null) return;

        // Gây stamina drain bằng cách trigger exhaustion event (không modify internal)
        // Thay vào đó gọi OnExhausted event để notify HUD
        playerBee.OnExhausted?.Invoke();

        PlayVFX(attackVFX);
        attackAudio?.Play();

        Debug.Log("[BeeEnemyAI] ⚠️ Tấn công player!");
    }

    // =========================================================================
    // UpdateStunned
    // =========================================================================
    private void UpdateStunned()
    {
        _stunTimer -= Time.deltaTime;

        // Retreat khỏi player khi bị stun
        if (_playerTransform != null)
        {
            Vector3 awayDir = (transform.position - _playerTransform.position).normalized;
            transform.position += awayDir * patrolSpeed * Time.deltaTime;
        }

        if (_stunTimer <= 0f)
        {
            SetState(AIState.Patrol);
            PickNewPatrolTarget();
        }
    }

    // =========================================================================
    // Public: Stun ong dịch (player phản công, hoa shield, v.v.)
    // =========================================================================
    public void Stun()
    {
        if (State == AIState.Dead) return;
        SetState(AIState.Stunned);
    }

    // =========================================================================
    // Public: Tiêu diệt ong dịch
    // =========================================================================
    public void Die()
    {
        if (State == AIState.Dead) return;
        State = AIState.Dead;
        OnDied?.Invoke();
        Debug.Log("[BeeEnemyAI] Ong dịch đã bị tiêu diệt.");
        Destroy(gameObject, 0.5f);
    }

    // =========================================================================
    // Helpers
    // =========================================================================
    private void SetState(AIState newState)
    {
        if (State == newState) return;
        State = newState;

        if (newState == AIState.Stunned)
        {
            _stunTimer = stunDuration;
            Debug.Log("[BeeEnemyAI] Ong dịch bị STUN!");
        }
    }

    private bool CanSeePlayer(float range)
    {
        if (_playerTransform == null) return false;
        return Vector3.Distance(transform.position, _playerTransform.position) <= range;
    }

    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 dir = (target - transform.position).normalized;
        if (dir.sqrMagnitude < 0.001f) return;

        // Smooth rotation
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot,
                                                      turnSpeed * Time.deltaTime);
        transform.position += dir * speed * Time.deltaTime;
    }

    private void PickNewPatrolTarget()
    {
        Vector2 rand2D = Random.insideUnitCircle * patrolRadius;
        _patrolTarget = _spawnPoint + new Vector3(rand2D.x, Random.Range(-1f, 2f), rand2D.y);
        _patrolTimer  = waypointTimeout;
    }

    private void UpdateBuzz()
    {
        if (buzzAudio == null) return;
        bool shouldPlay = State != AIState.Dead && State != AIState.Stunned;
        if (shouldPlay && !buzzAudio.isPlaying) buzzAudio.Play();
        if (!shouldPlay && buzzAudio.isPlaying) buzzAudio.Stop();
    }

    private static void PlayVFX(ParticleSystem ps)
    {
        if (ps != null) ps.Play();
    }

    // =========================================================================
    // Gizmos
    // =========================================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? _spawnPoint : transform.position, patrolRadius);
    }
}
