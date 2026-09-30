using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Điều khiển cổng bằng tay cầm VR: nhấn Grab vào Toggle → đổi trạng thái cổng (đóng/mở).
/// Chỉ đặt script này trên PFB_Switch (object chứa Toggle). KHÔNG cần đặt trên PFB_Gate_G2.
/// Script tự tìm PFB_Gate_G2 trong scene để điều khiển animation cổng.
///
/// KHI ĐÓNG: chặn enemy (nước mặn) bằng cách:
///   - Bật NavMeshObstacle trên cổng → chặn NavMesh pathfinding
///   - Dùng trigger zone bắt enemy đang trong vùng → SetTrapped(true)
///   - Enemy mới đi vào trigger khi cổng đóng cũng bị bắt
/// KHI MỞ: thả tất cả enemy bị bắt → SetTrapped(false)
///
/// Setup trong Unity Inspector:
///   1. Toggle cần có Collider (Box/Sphere) để tay detect được
///   2. Kéo Animator của Switch vào trường "Switch Anim" (tự tìm nếu bỏ trống)
///   3. (Tuỳ chọn) Kéo Animator của Gate vào "Gate Anim" (tự tìm PFB_Gate_G2 trong scene)
///   4. (Tuỳ chọn) Kéo Collider trên cánh cổng vào "Gate Blocker" để chặn quái
///   5. Trên PFB_Gate_G2: cần có trigger Collider để phát hiện enemy
/// </summary>
public class SwitchGate : MonoBehaviour
{
    [Header("Animator")]
    [Tooltip("Animator của Switch (chứa Switch_ON / Switch_OFF) - tự lấy trên object này nếu bỏ trống")]
    public Animator switchAnim;

    [Tooltip("Animator của Gate (chứa PFB_Gate2_ON / PFB_Gate2_OFF) - tự tìm PFB_Gate_G2 trong scene nếu bỏ trống")]
    public Animator gateAnim;

    [Header("Toggle Object (tự tìm child 'Toggle' nếu bỏ trống)")]
    public Transform toggleObject;

    [Header("Collider chặn quái khi cổng đóng (tuỳ chọn)")]
    [Tooltip("Collider trên cánh cổng - bật khi đóng để chặn nước mặn đi qua")]
    public Collider gateBlocker;

    [Header("Enemy Blocking / Chặn nước mặn")]
    [Tooltip("Bán kính vùng chặn enemy quanh cổng (auto-tạo trigger trên Gate)")]
    public float blockRadius = 5f;

    private bool isClosed = false;
    private XRBaseInteractable interactable;

    // Enemy blocking state
    private GameObject _gateObject;
    private NavMeshObstacle _navObstacle;
    private GateBlockerZone _blockerZone;

    void Start()
    {
        // Tìm Switch Animator trên chính object này
        if (switchAnim == null)
            switchAnim = GetComponent<Animator>();

        // Tìm Gate Animator - ưu tiên theo tên PFB_Gate_G2 trong scene
        if (gateAnim == null)
        {
            GameObject gateObj = GameObject.Find("PFB_Gate_G2");
            if (gateObj != null)
                gateAnim = gateObj.GetComponent<Animator>();
        }

        if (switchAnim != null)
            Debug.Log($"[SwitchGate] Switch Animator: {switchAnim.gameObject.name}", this);
        if (gateAnim != null)
            Debug.Log($"[SwitchGate] Gate Animator: {gateAnim.gameObject.name}", this);
        else
            Debug.LogWarning("[SwitchGate] Không tìm thấy Gate Animator (PFB_Gate_G2)!", this);

        // Setup enemy blocking trên gate object
        if (gateAnim != null)
        {
            _gateObject = gateAnim.gameObject;
            SetupEnemyBlocking(_gateObject);
        }

        // Tự tìm Toggle nếu chưa gán
        if (toggleObject == null)
            toggleObject = transform.Find("Toggle");
        if (toggleObject == null)
            toggleObject = FindChildRecursive(transform, "Toggle");

        if (toggleObject != null)
        {
            // Ưu tiên dùng XRGrabInteractable có sẵn nhưng tắt di chuyển
            XRGrabInteractable grab = toggleObject.GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                grab.trackPosition = false;
                grab.trackRotation = false;
                grab.throwOnDetach = false;
                grab.movementType = XRGrabInteractable.MovementType.Instantaneous;
                interactable = grab;
            }
            else
            {
                // Fallback: dùng XRSimpleInteractable
                XRSimpleInteractable simple = toggleObject.GetComponent<XRSimpleInteractable>();
                if (simple == null)
                    simple = toggleObject.gameObject.AddComponent<XRSimpleInteractable>();
                interactable = simple;
            }

            interactable.selectEntered.AddListener(OnGrab);

            // Khoá Rigidbody hoàn toàn
            Rigidbody rb = toggleObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
        }
        else
        {
            Debug.LogWarning($"[SwitchGate] Không tìm thấy Toggle trên {gameObject.name}!", this);
        }

        // Khởi tạo cổng mở
        SetGateState(false);
    }

    /// <summary>
    /// Thiết lập hệ thống chặn enemy trên gate object:
    /// 1. NavMeshObstacle: chặn pathfinding khi đóng
    /// 2. GateBlockerZone: trigger zone bắt/thả enemy
    /// </summary>
    private void SetupEnemyBlocking(GameObject gateObj)
    {
        // NavMeshObstacle — chặn NavMesh pathfinding khi cổng đóng
        _navObstacle = gateObj.GetComponent<NavMeshObstacle>();
        if (_navObstacle == null)
            _navObstacle = gateObj.AddComponent<NavMeshObstacle>();
        _navObstacle.carving = true;
        _navObstacle.shape = NavMeshObstacleShape.Box;
        _navObstacle.size = new Vector3(4f, 3f, 1f);
        _navObstacle.enabled = false; // Tắt khi cổng mở

        // GateBlockerZone — trigger zone bắt enemy khi cổng đóng
        _blockerZone = gateObj.GetComponent<GateBlockerZone>();
        if (_blockerZone == null)
            _blockerZone = gateObj.AddComponent<GateBlockerZone>();
        _blockerZone.SetRadius(blockRadius);
        _blockerZone.SetActive(false);

        Debug.Log($"[SwitchGate] Enemy blocking setup on {gateObj.name}, radius={blockRadius}");
    }

    void OnDestroy()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnGrab);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Nhấn Grab → đổi trạng thái cổng
        SetGateState(!isClosed);
    }

    private void SetGateState(bool closed)
    {
        isClosed = closed;

        if (switchAnim != null)
            switchAnim.Play(closed ? "Switch_ON" : "Switch_OFF", -1, 0f);

        if (gateAnim != null)
            gateAnim.Play(closed ? "PFB_Gate2_OFF" : "PFB_Gate2_ON", -1, 0f);

        if (gateBlocker != null)
            gateBlocker.enabled = closed;

        // Bật/tắt NavMeshObstacle
        if (_navObstacle != null)
            _navObstacle.enabled = closed;

        // Bật/tắt vùng chặn enemy
        if (_blockerZone != null)
        {
            if (closed)
                _blockerZone.SetActive(true);
            else
                _blockerZone.SetActive(false); // Thả tất cả enemy bị giữ
        }

        Debug.Log($"[SwitchGate] Gate {(closed ? "ĐÓNG — chặn enemy" : "MỞ — thả enemy")}");
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}

// =============================================================================
// GateBlockerZone - Trigger zone gắn trên cổng, bắt/thả enemy khi đóng/mở.
// GateBlockerZone - Trigger zone attached to gate, traps/releases enemies.
//
// Khi active (cổng đóng):
//   - Enemy đi vào trigger → SetTrapped(true), dừng di chuyển
//   - Enemy bị giữ tại vị trí trước cổng
// Khi deactive (cổng mở):
//   - Tất cả enemy bị giữ → SetTrapped(false), tiếp tục di chuyển
// =============================================================================
public class GateBlockerZone : MonoBehaviour
{
    /// <summary>
    /// Tổng số enemy đã bị PFB_Gate_G2 chặn (tích lũy trong toàn level).
    /// Reset khi reload scene (static field tự reset trong Editor PlayMode tuỳ cấu hình).
    /// </summary>
    public static int TotalEnemiesBlocked = 0;

    public static void ResetCounter() { TotalEnemiesBlocked = 0; }

    private SphereCollider _trigger;
    private bool _isActive = false;
    private readonly List<EnemyController> _trappedEnemies = new List<EnemyController>();

    /// <summary>
    /// Thiết lập bán kính trigger zone.
    /// </summary>
    public void SetRadius(float radius)
    {
        if (_trigger == null)
        {
            _trigger = GetComponent<SphereCollider>();
            if (_trigger == null)
                _trigger = gameObject.AddComponent<SphereCollider>();
        }
        _trigger.isTrigger = true;
        _trigger.radius = radius;
        _trigger.center = Vector3.zero;

        // Cần Rigidbody (kinematic) để trigger hoạt động
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    /// <summary>
    /// Bật/tắt vùng chặn. Khi tắt, thả tất cả enemy.
    /// </summary>
    public void SetActive(bool active)
    {
        bool wasActive = _isActive;
        _isActive = active;

        if (_trigger != null)
            _trigger.enabled = active;

        if (!active && wasActive)
        {
            // Cổng mở → thả tất cả enemy bị giữ
            ReleaseAllEnemies();
        }

        if (active)
        {
            // Cổng vừa đóng → bắt enemy đang trong vùng
            TrapEnemiesInRange();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_isActive) return;
        if (!other.CompareTag("Enemy")) return;

        var controller = other.GetComponent<EnemyController>();
        if (controller == null) return;

        var enemy = other.GetComponent<Enemy>();
        if (enemy != null && enemy.IsDead()) return;

        // Đã bị bắt rồi thì bỏ qua
        if (controller.IsTrapped) return;

        TrapEnemy(controller);
    }

    void OnTriggerExit(Collider other)
    {
        // Khi cổng mở và enemy thoát ra → xóa khỏi danh sách
        if (!other.CompareTag("Enemy")) return;
        var controller = other.GetComponent<EnemyController>();
        if (controller != null)
            _trappedEnemies.Remove(controller);
    }

    void Update()
    {
        if (!_isActive) return;

        // Dọn dẹp enemy đã bị hủy
        _trappedEnemies.RemoveAll(e => e == null);
    }

    private void TrapEnemy(EnemyController controller)
    {
        controller.SetTrapped(true);
        if (!_trappedEnemies.Contains(controller))
        {
            _trappedEnemies.Add(controller);
            TotalEnemiesBlocked++;   // đếm số enemy bị cổng chặn
        }
        Debug.Log($"[GateBlocker] Trapped {controller.gameObject.name} (total blocked: {TotalEnemiesBlocked})");
    }

    private void ReleaseAllEnemies()
    {
        foreach (var controller in _trappedEnemies)
        {
            if (controller != null)
            {
                controller.SetTrapped(false);
                Debug.Log($"[GateBlocker] Released {controller.gameObject.name}");
            }
        }
        _trappedEnemies.Clear();
    }

    /// <summary>
    /// Bắt tất cả enemy đang trong bán kính khi cổng vừa đóng.
    /// </summary>
    private void TrapEnemiesInRange()
    {
        float radius = _trigger != null ? _trigger.radius : 5f;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var controller = hit.GetComponent<EnemyController>();
            if (controller == null) continue;
            var enemy = hit.GetComponent<Enemy>();
            if (enemy != null && enemy.IsDead()) continue;
            if (controller.IsTrapped) continue;
            TrapEnemy(controller);
        }
    }
}
