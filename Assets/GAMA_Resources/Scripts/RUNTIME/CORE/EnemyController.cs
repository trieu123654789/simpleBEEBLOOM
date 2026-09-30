using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// =============================================================================
// EnemyController - Moves enemy along waypoints using NavMeshAgent OR fallback.
// EnemyController - Di chuyển enemy dọc waypoint bằng NavMeshAgent HOẶC fallback.
//
// If NavMesh is not baked in the scene, falls back to simple Transform.MoveTowards.
// Nếu NavMesh chưa baked trong scene, dùng Transform.MoveTowards thay thế.
// =============================================================================
public class EnemyController : MonoBehaviour
{
    private List<Transform> wayPoints;

    private int currentWayPointIndex = 0;
    private float agentStoppingDistance = .3f;

    private bool wayPointsSet = false;
    private bool hasStartedMoving = false;

    // TreeBarrier trap: khi bị cây giữ, enemy dừng di chuyển.
    // TreeBarrier trap: when trapped by a tree, enemy stops moving.
    private bool _isTrapped = false;

    // NavMesh fallback: use simple movement if NavMesh is not available.
    // Fallback NavMesh: dùng di chuyển đơn giản nếu NavMesh không khả dụng.
    private bool _useNavMesh = true;
    private float _fallbackSpeed = 2f;

    NavMeshAgent agent;

    private Enemy enemy;
    void Start()
    {
        enemy = GetComponent<Enemy>();
        agent = GetComponent<NavMeshAgent>();

        // Check if NavMesh is available. If not, disable agent and use fallback.
        // Kiểm tra NavMesh có khả dụng không. Nếu không, tắt agent và dùng fallback.
        if (agent != null)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                _useNavMesh = true;
                if (!agent.isOnNavMesh)
                {
                    agent.Warp(hit.position);
                }
            }
            else
            {
                // No NavMesh found — use fallback movement.
                _useNavMesh = false;
                agent.enabled = false;
                Debug.LogWarning($"[EnemyController] No NavMesh at {transform.position} — using fallback movement.");
            }
            _fallbackSpeed = agent.speed > 0 ? agent.speed : 2f;
        }
        else
        {
            _useNavMesh = false;
            _fallbackSpeed = 2f;
        }

        // Apply tidal speed modifier if available.
        // Áp dụng hệ số tốc độ triều nếu có.
        if (TidalClockManager.Instance != null)
        {
            float speedMult = TidalClockManager.Instance.CurrentEnemySpeedMultiplier;
            if (_useNavMesh && agent != null && agent.enabled)
            {
                agent.speed *= speedMult;
            }
            _fallbackSpeed *= speedMult;
        }
    }

    void Update()
    {
        if (!wayPointsSet) return;

        // Dừng di chuyển nếu enemy đã chết
        if (enemy != null && enemy.IsDead()) return;

        // Dừng di chuyển nếu bị cây giữ
        if (_isTrapped) return;

        if (_useNavMesh)
        {
            UpdateNavMeshMovement();
        }
        else
        {
            UpdateFallbackMovement();
        }
    }

    /// <summary>
    /// NavMesh-based movement (original behavior).
    /// Di chuyển bằng NavMesh (hành vi gốc).
    /// </summary>
    private void UpdateNavMeshMovement()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (!hasStartedMoving)
        {
            if (currentWayPointIndex < wayPoints.Count && wayPoints[currentWayPointIndex] != null)
            {
                agent.SetDestination(wayPoints[currentWayPointIndex].position);
                currentWayPointIndex++;
                hasStartedMoving = true;
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agentStoppingDistance)
        {
            if (currentWayPointIndex >= wayPoints.Count)
            {
                // Reached final waypoint without being killed → enemy breached inland.
                // Đến waypoint cuối mà chưa bị diệt → xâm nhập nội đồng.
                ReportBreachOnce();
                Destroy(this.gameObject, .1f);
            }
            else if (wayPoints[currentWayPointIndex] != null)
            {
                agent.SetDestination(wayPoints[currentWayPointIndex].position);
                currentWayPointIndex++;
            }
        }
    }

    /// <summary>
    /// Fallback movement when NavMesh is not available.
    /// Di chuyển fallback khi NavMesh không khả dụng.
    /// </summary>
    private void UpdateFallbackMovement()
    {
        if (!hasStartedMoving)
        {
            hasStartedMoving = true;
            currentWayPointIndex = 0;
        }

        if (currentWayPointIndex >= wayPoints.Count)
        {
            // Reached final waypoint without being killed → enemy breached inland.
            // Đến waypoint cuối mà chưa bị diệt → xâm nhập nội đồng.
            ReportBreachOnce();
            Destroy(this.gameObject, .1f);
            return;
        }

        Transform target = wayPoints[currentWayPointIndex];
        if (target == null)
        {
            currentWayPointIndex++;
            return;
        }

        Vector3 targetPos = target.position;
        transform.position = Vector3.MoveTowards(
            transform.position, targetPos, _fallbackSpeed * Time.deltaTime);

        Vector3 dir = (targetPos - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 5f);
        }

        if (Vector3.Distance(transform.position, targetPos) <= agentStoppingDistance)
        {
            currentWayPointIndex++;
        }
    }

    public void SetDestination(List<Transform> wayPoints)
    {
        this.wayPoints = wayPoints;
        wayPointsSet = true;
        hasStartedMoving = false;
        currentWayPointIndex = 0;
    }

    // Cho phép báo cáo breach đúng 1 lần cho mỗi enemy (bảo vệ chống đếm trùng).
    // Ensures breach is reported exactly once per enemy.
    private bool _breachReported = false;
    private void ReportBreachOnce()
    {
        if (_breachReported) return;
        _breachReported = true;
        // Chỉ tính nếu enemy vẫn sống (không bị Ally/công trình diệt).
        // Only count if enemy is still alive (was not neutralized).
        if (enemy != null && enemy.IsDead()) return;
        if (StatisticsManager.Instance != null)
            StatisticsManager.Instance.IncreaseEnemyBreachedCount();
    }

    /// <summary>
    /// Bật/tắt trạng thái bị cây giữ. Khi trapped=true, enemy dừng di chuyển.
    /// Toggle trapped state. When trapped=true, enemy stops moving.
    /// </summary>
    public void SetTrapped(bool trapped)
    {
        _isTrapped = trapped;
        if (_useNavMesh && agent != null && agent.isOnNavMesh)
        {
            if (trapped)
                agent.isStopped = true;
            else
                agent.isStopped = false;
        }
    }

    public bool IsTrapped => _isTrapped;
}
