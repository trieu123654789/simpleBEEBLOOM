using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// SaltyWaterPatrol - Di chuyển vòng lặp qua các điểm định sẵn.
// SaltyWaterPatrol - Loops through waypoints, speed driven by tidal system.
//
// Gắn script này vào PFB_SaltyWater_inTheSea đặt sẵn trong scene.
// Kéo các Empty GameObject (waypoint) vào danh sách trong Inspector.
// Không cần NavMesh, không cần EnemyController/EnemySpawner.
//
// TIDAL CONNECTION:
// - Triều cường (Spring Tide): nước chảy nhanh hơn, hướng vào (forward)
// - Triều kém   (Neap Tide):   nước chảy chậm hơn, đảo hướng (backward)
// - Tốc độ được nội suy mượt theo TidalIntensity (0→1)
// =============================================================================
public class SaltyWaterPatrol : MonoBehaviour
{
    [Header("Waypoints — kéo thả các điểm vào đây")]
    [SerializeField] private List<Transform> wayPoints;

    [Header("Movement Settings / Cài đặt di chuyển")]
    [Tooltip("Tốc độ di chuyển cơ sở (units/s)")]
    [SerializeField] private float moveSpeed = 2f;

    [Tooltip("Tốc độ xoay hướng (degrees/s)")]
    [SerializeField] private float rotateSpeed = 5f;

    [Tooltip("Khoảng cách đến waypoint coi như đã tới")]
    [SerializeField] private float arrivalThreshold = 0.3f;

    [Header("Loop Mode / Chế độ lặp")]
    [Tooltip("true = đi vòng lặp (A→B→C→A→B→C…)\nfalse = đi qua lại (A→B→C→B→A…)")]
    [SerializeField] private bool loopCircular = true;

    [Tooltip("Dừng lại bao lâu tại mỗi điểm (giây). 0 = không dừng.")]
    [SerializeField] private float waitTimeAtPoint = 0f;

    [Header("Tidal Settings / Cài đặt triều")]
    [Tooltip("Hệ số tốc độ khi triều cường (Spring Tide). Nhân với moveSpeed.\n"
           + "Speed multiplier at peak Spring Tide.")]
    [SerializeField] private float springTideSpeedMultiplier = 2.0f;

    [Tooltip("Hệ số tốc độ khi triều kém (Neap Tide). Nhân với moveSpeed.\n"
           + "Speed multiplier at peak Neap Tide.")]
    [SerializeField] private float neapTideSpeedMultiplier = 0.3f;

    [Tooltip("Đảo hướng di chuyển khi triều kém (nước rút ra)?\n"
           + "Reverse waypoint direction during Neap Tide?")]
    [SerializeField] private bool reverseOnNeapTide = true;

    private int currentIndex = 0;
    private int direction = 1;        // 1 = forward, -1 = backward (ping-pong)
    private float waitTimer = 0f;
    private float _currentSpeedMultiplier = 1f;
    private TidalState _lastTidalState = TidalState.SpringTide;

    void OnEnable()
    {
        TidalClockManager.OnTidalIntensityUpdated += OnTidalIntensityUpdated;
        TidalClockManager.OnTidalStateChanged += OnTidalStateChanged;
    }

    void OnDisable()
    {
        TidalClockManager.OnTidalIntensityUpdated -= OnTidalIntensityUpdated;
        TidalClockManager.OnTidalStateChanged -= OnTidalStateChanged;
    }

    private void OnTidalIntensityUpdated(float intensity)
    {
        // intensity 0 = triều kém mạnh nhất, 1 = triều cường mạnh nhất
        _currentSpeedMultiplier = Mathf.Lerp(neapTideSpeedMultiplier, springTideSpeedMultiplier, intensity);
    }

    private void OnTidalStateChanged(TidalState state)
    {
        if (state == _lastTidalState) return;
        _lastTidalState = state;

        if (!reverseOnNeapTide || loopCircular) return;

        // Ping-pong mode: đảo hướng di chuyển khi triều kém (nước rút)
        if (state == TidalState.NeapTide)
            direction = -1;
        else
            direction = 1;
    }

    void Update()
    {
        if (wayPoints == null || wayPoints.Count < 2) return;

        // Đang chờ tại điểm
        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        Transform target = wayPoints[currentIndex];
        if (target == null) { AdvanceIndex(); return; }

        Vector3 targetPos = target.position;
        float step = moveSpeed * _currentSpeedMultiplier * Time.deltaTime;

        // Di chuyển đến waypoint hiện tại
        transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

        // Xoay mặt theo hướng di chuyển
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                rotateSpeed * Time.deltaTime);
        }

        // Đã đến điểm → chuyển sang điểm kế tiếp
        if (Vector3.Distance(transform.position, targetPos) <= arrivalThreshold)
        {
            waitTimer = waitTimeAtPoint;
            AdvanceIndex();
        }
    }

    private void AdvanceIndex()
    {
        if (loopCircular)
        {
            // Vòng lặp: 0 → 1 → 2 → 0 → 1 → …
            currentIndex = (currentIndex + 1) % wayPoints.Count;
        }
        else
        {
            // Ping-pong: 0 → 1 → 2 → 1 → 0 → 1 → …
            currentIndex += direction;
            if (currentIndex >= wayPoints.Count)
            {
                currentIndex = wayPoints.Count - 2;
                direction = -1;
            }
            else if (currentIndex < 0)
            {
                currentIndex = 1;
                direction = 1;
            }
        }
    }

    // Vẽ đường đi trong Scene view để dễ thiết kế
    private void OnDrawGizmosSelected()
    {
        if (wayPoints == null || wayPoints.Count < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < wayPoints.Count; i++)
        {
            if (wayPoints[i] == null) continue;

            // Vẽ sphere tại mỗi waypoint
            Gizmos.DrawWireSphere(wayPoints[i].position, 0.3f);

            // Vẽ đường nối
            int next = (i + 1) % wayPoints.Count;
            if (wayPoints[next] != null)
            {
                if (loopCircular || next != 0)
                    Gizmos.DrawLine(wayPoints[i].position, wayPoints[next].position);
            }
        }
    }
}
