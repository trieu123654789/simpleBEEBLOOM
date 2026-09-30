using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vùng được phép trồng cây. Người chơi phải đi vào vùng này (collider trigger)
/// thì mới có thể đặt cây xuống khi đang mang cây trên tay.
///
/// Setup:
/// - Tạo GameObject rỗng đặt tại vị trí muốn cho phép trồng cây.
/// - Gắn 1 Collider (vd: BoxCollider / SphereCollider) với <c>isTrigger = true</c>.
/// - Gắn script này.
/// - Gán <c>playerTag</c> trùng với tag của XR Origin / Character (vd: "Player").
/// - (tuỳ chọn) Gán <c>highlightObject</c> = một GameObject hiệu ứng (decal, ring, ...)
///   để bật/tắt khi người chơi đi vào.
///
/// BuildSystemManager sẽ truy vấn <see cref="GetActiveZone"/> để biết người chơi
/// đang đứng trong zone nào, rồi đặt cây tại <see cref="PlantPosition"/>.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PlantingZone : MonoBehaviour
{
    [Tooltip("(Tuỳ chọn) Tag của Player. Để trống để tắt check tag (BuildSystemManager sẽ tự dò vị trí camera/rig).")]
    [SerializeField] private string playerTag = "";

    [Tooltip("Object hiệu ứng (decal/ring) hiện khi player đứng trong zone (bật bởi BuildSystemManager khi player đang carry).")]
    [SerializeField] private GameObject highlightObject;

    [Tooltip("Object hiệu ứng chỉ bật khi player đang mang cây và đứng trong zone (sẵn sàng trồng).")]
    [SerializeField] private GameObject readyToPlantObject;

    [Tooltip("Nếu bật: zone bị huỷ sau khi 1 cây được trồng (zone dùng 1 lần).")]
    [SerializeField] private bool consumeOnPlant = false;

    [Tooltip("Vị trí đặt cây. Nếu để trống sẽ dùng vị trí của zone.")]
    [SerializeField] private Transform plantAnchor;

    // ===== Static registry =====
    private static readonly List<PlantingZone> _allZones = new List<PlantingZone>();
    public static IReadOnlyList<PlantingZone> AllZones => _allZones;

    // ===== Public API =====
    public Collider Collider { get; private set; }
    public Vector3 PlantPosition => plantAnchor != null ? plantAnchor.position : transform.position;
    public Quaternion PlantRotation => plantAnchor != null ? plantAnchor.rotation : transform.rotation;

    /// <summary>Kiểm tra vị trí có nằm trong collider của zone này không (kể cả trigger).</summary>
    public bool ContainsPoint(Vector3 worldPos)
    {
        if (Collider == null) return false;
        // ClosestPoint trả về chính điểm đó nếu điểm nằm bên trong collider (loại trừ MeshCollider non-convex).
        if (Collider is MeshCollider mc && !mc.convex)
        {
            return Collider.bounds.Contains(worldPos);
        }
        Vector3 closest = Collider.ClosestPoint(worldPos);
        return (closest - worldPos).sqrMagnitude < 0.0001f;
    }

    /// <summary>Tìm zone đầu tiên chứa vị trí này. Null nếu không có.</summary>
    public static PlantingZone FindZoneContaining(Vector3 worldPos)
    {
        for (int i = 0; i < _allZones.Count; i++)
        {
            var z = _allZones[i];
            if (z != null && z.isActiveAndEnabled && z.ContainsPoint(worldPos)) return z;
        }
        return null;
    }

    /// <summary>Tìm zone gần vị trí nhất (theo PlantPosition). Null nếu chưa có zone nào.</summary>
    public static PlantingZone FindNearestZone(Vector3 worldPos)
    {
        PlantingZone best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < _allZones.Count; i++)
        {
            var z = _allZones[i];
            if (z == null || !z.isActiveAndEnabled) continue;
            float sqr = (z.PlantPosition - worldPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = z;
            }
        }
        return best;
    }

    /// <summary>BuildSystemManager gọi mỗi frame trong carry mode để cập nhật highlight theo vị trí player.</summary>
    public static void UpdateHighlights(Vector3 playerPos, bool carrying)
    {
        for (int i = 0; i < _allZones.Count; i++)
        {
            var z = _allZones[i];
            if (z == null) continue;
            bool inside = carrying && z.isActiveAndEnabled && z.ContainsPoint(playerPos);
            if (z.highlightObject != null && z.highlightObject.activeSelf != carrying)
                z.highlightObject.SetActive(carrying); // hiện mọi zone khi đang carry
            if (z.readyToPlantObject != null && z.readyToPlantObject.activeSelf != inside)
                z.readyToPlantObject.SetActive(inside);
        }
    }

    /// <summary>Tắt toàn bộ highlight (gọi khi thoát carry mode).</summary>
    public static void ClearAllHighlights()
    {
        for (int i = 0; i < _allZones.Count; i++)
        {
            var z = _allZones[i];
            if (z == null) continue;
            if (z.highlightObject != null) z.highlightObject.SetActive(false);
            if (z.readyToPlantObject != null) z.readyToPlantObject.SetActive(false);
        }
    }

    /// <summary>Gọi sau khi trồng cây trong zone này.</summary>
    public void NotifyPlanted()
    {
        if (consumeOnPlant)
        {
            _allZones.Remove(this);
            Destroy(gameObject);
        }
    }

    private void Awake()
    {
        Collider = GetComponent<Collider>();
        if (Collider != null && !Collider.isTrigger)
        {
            Debug.LogWarning($"[PlantingZone] Collider on {name} should be isTrigger = true.");
        }
        if (highlightObject != null) highlightObject.SetActive(false);
        if (readyToPlantObject != null) readyToPlantObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (!_allZones.Contains(this)) _allZones.Add(this);
    }

    private void OnDisable()
    {
        _allZones.Remove(this);
        if (highlightObject != null) highlightObject.SetActive(false);
        if (readyToPlantObject != null) readyToPlantObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.35f);
        var col = GetComponent<Collider>();
        if (col is BoxCollider bc)
        {
            Matrix4x4 m = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(bc.center, bc.size);
            Gizmos.matrix = m;
        }
        else if (col is SphereCollider sc)
        {
            Gizmos.DrawSphere(transform.TransformPoint(sc.center), sc.radius * transform.lossyScale.x);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
