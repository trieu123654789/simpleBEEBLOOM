// =============================================================================
// BeeFlowerSpawner - Sinh hoa theo pattern có cấu hình, thay thế logic inline
//                    trong BeeEcosystemZone.
// BeeFlowerSpawner - Configurable flower spawner, hex-grid / cluster / row pattern.
//
// SỬ DỤNG / USAGE
//   Đặt component này trên cùng GameObject với BeeEcosystemZone.
//   BeeEcosystemZone vẫn giữ danh sách flowers[] — BeeFlowerSpawner thêm vào đó.
//
// PATTERN
//   Hex    : lưới hex chuẩn, khoảng cách đều nhau (đẹp nhất cho thực địa)
//   Cluster: cụm ngẫu nhiên quanh tâm (tự nhiên nhất)
//   Row    : hàng thẳng (cho lúa, mía, v.v.)
//   Ring   : vòng tròn (cho sen, lotus)
//
// INITIAL SPAWN vs. BONUS SPAWN
//   InitialSpawn() gọi khi Start để đặt hoa ban đầu.
//   SpawnBonus()   gọi bởi BeeEcosystemZone khi zone Thriving để thêm hoa mới.
// =============================================================================
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BeeFlowerSpawner : MonoBehaviour
{
    // =========================================================================
    // ENUMS
    // =========================================================================
    public enum SpawnPattern { Hex, Cluster, Row, Ring }

    // =========================================================================
    // SETTINGS / CÀI ĐẶT
    // =========================================================================
    [Header("Spawn Pattern / Kiểu sinh hoa")]
    public SpawnPattern pattern = SpawnPattern.Cluster;

    [Header("Prefabs / Loài hoa")]
    [Tooltip("Danh sách prefab hoa. Nếu nhiều prefab sẽ chọn ngẫu nhiên.")]
    public List<GameObject> flowerPrefabs = new();

    [Tooltip("Loài hoa đặc trưng để gán cho tất cả hoa spawn ra.")]
    public BeeFlower.FlowerSpecies species = BeeFlower.FlowerSpecies.Lotus;

    [Header("Quantity / Số lượng")]
    [Tooltip("Số hoa sinh khi bắt đầu.")]
    [Range(1, 30)]
    public int initialCount = 6;

    [Tooltip("Số hoa sinh khi bonus (thriving). <= 0 để tắt.")]
    [Range(0, 10)]
    public int bonusCount = 2;

    [Tooltip("Số hoa tối đa tổng cộng trong zone này.")]
    [Range(1, 50)]
    public int maxCount = 12;

    [Header("Layout / Bố cục")]
    [Tooltip("Khoảng cách giữa các hoa (m).")]
    [Range(0.5f, 5f)]
    public float spacing = 1.8f;

    [Tooltip("Nhiễu vị trí để trông tự nhiên hơn (m).")]
    [Range(0f, 1f)]
    public float positionJitter = 0.3f;

    [Tooltip("Chiều cao ngẫu nhiên (+/-) so với tâm zone (m).")]
    [Range(0f, 2f)]
    public float heightVariance = 0.1f;

    [Tooltip("Xoay hoa ngẫu nhiên theo Y.")]
    public bool randomRotation = true;

    [Header("Row Pattern Only")]
    [Tooltip("Số cột cho pattern Row.")]
    [Range(1, 10)]
    public int rowColumns = 3;

    [Header("Ring Pattern Only")]
    [Tooltip("Bán kính vòng tròn (m).")]
    [Range(1f, 15f)]
    public float ringRadius = 4f;

    // =========================================================================
    // REFERENCES
    // =========================================================================
    [Header("References")]
    [Tooltip("BeeEcosystemZone cùng zone. Để trống sẽ tự tìm.")]
    public BeeEcosystemZone zone;

    // =========================================================================
    // PUBLIC STATE
    // =========================================================================
    public int TotalSpawned { get; private set; }
    public List<BeeFlower> SpawnedFlowers { get; private set; } = new();

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        if (zone == null) zone = GetComponent<BeeEcosystemZone>();
    }

    private void Start()
    {
        InitialSpawn();
    }

    // =========================================================================
    // InitialSpawn - Sinh hoa ban đầu khi scene load.
    // =========================================================================
    public void InitialSpawn()
    {
        if (flowerPrefabs == null || flowerPrefabs.Count == 0)
        {
            Debug.LogWarning("[BeeFlowerSpawner] Chưa gán flowerPrefabs!");
            return;
        }

        List<Vector3> positions = GeneratePositions(initialCount);
        foreach (var pos in positions)
            SpawnAt(pos);

        Debug.Log($"[BeeFlowerSpawner] Đã sinh {TotalSpawned} hoa kiểu {pattern} tại {gameObject.name}");
    }

    // =========================================================================
    // SpawnBonus - Sinh thêm hoa khi zone thriving.
    // =========================================================================
    public void SpawnBonus()
    {
        if (bonusCount <= 0) return;
        if (TotalSpawned >= maxCount) return;

        int canSpawn = Mathf.Min(bonusCount, maxCount - TotalSpawned);
        List<Vector3> positions = GeneratePositions(canSpawn);
        foreach (var pos in positions)
            SpawnAt(pos);

        Debug.Log($"[BeeFlowerSpawner] Bonus +{canSpawn} hoa. Tổng: {TotalSpawned}");
    }

    // =========================================================================
    // GeneratePositions - Tạo danh sách vị trí theo pattern.
    // =========================================================================
    private List<Vector3> GeneratePositions(int count)
    {
        return pattern switch
        {
            SpawnPattern.Hex     => GenHex(count),
            SpawnPattern.Row     => GenRow(count),
            SpawnPattern.Ring    => GenRing(count),
            _                    => GenCluster(count),  // Cluster (default)
        };
    }

    // Lưới hex
    private List<Vector3> GenHex(int count)
    {
        var pts = new List<Vector3>();
        // Spiral hex coords
        pts.Add(Center());
        if (count <= 1) return pts;

        int ring = 1;
        while (pts.Count < count)
        {
            Vector3 cursor = Center() + HexDir(4) * spacing * ring;
            for (int side = 0; side < 6; side++)
            {
                for (int step = 0; step < ring; step++)
                {
                    if (pts.Count >= count) break;
                    pts.Add(Jitter(cursor));
                    cursor += HexDir(side) * spacing;
                }
            }
            ring++;
        }
        return pts;
    }

    // Cụm ngẫu nhiên
    private List<Vector3> GenCluster(int count)
    {
        var pts = new List<Vector3>();
        float radius = spacing * Mathf.Sqrt(count) * 0.6f;
        for (int i = 0; i < count; i++)
        {
            Vector2 r = Random.insideUnitCircle * radius;
            pts.Add(Center() + new Vector3(r.x, 0, r.y));
        }
        return pts;
    }

    // Hàng thẳng
    private List<Vector3> GenRow(int count)
    {
        var pts = new List<Vector3>();
        int rows = Mathf.CeilToInt((float)count / rowColumns);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < rowColumns && pts.Count < count; c++)
            {
                float x = (c - (rowColumns - 1) * 0.5f) * spacing;
                float z = r * spacing;
                pts.Add(Center() + Jitter(new Vector3(x, 0, z)));
            }
        return pts;
    }

    // Vòng tròn
    private List<Vector3> GenRing(int count)
    {
        var pts = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            float r = ringRadius + Random.Range(-positionJitter, positionJitter);
            pts.Add(Center() + Jitter(new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r)));
        }
        return pts;
    }

    // =========================================================================
    // SpawnAt - Sinh 1 hoa tại vị trí.
    // =========================================================================
    private void SpawnAt(Vector3 worldPos)
    {
        if (flowerPrefabs.Count == 0) return;

        var prefab = flowerPrefabs[Random.Range(0, flowerPrefabs.Count)];
        if (prefab == null) return;

        // Snap to ground nếu có terrain
        float y = worldPos.y;
        if (Physics.Raycast(worldPos + Vector3.up * 10f, Vector3.down, out var hit, 20f))
            y = hit.point.y;

        worldPos.y = y + Random.Range(-heightVariance, heightVariance);

        Quaternion rot = randomRotation
            ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
            : Quaternion.identity;

        var go = Instantiate(prefab, worldPos, rot, transform);
        var flower = go.GetComponent<BeeFlower>();

        if (flower != null)
        {
            flower.species = species;
            SpawnedFlowers.Add(flower);

            // Đăng ký vào zone nếu có
            if (zone != null && !zone.flowers.Contains(flower))
                zone.flowers.Add(flower);
        }

        TotalSpawned++;
    }

    // =========================================================================
    // Helpers
    // =========================================================================
    private Vector3 Center() => transform.position;

    private Vector3 Jitter(Vector3 v)
    {
        v.x += Random.Range(-positionJitter, positionJitter);
        v.z += Random.Range(-positionJitter, positionJitter);
        return v;
    }

    private static Vector3 HexDir(int dir)
    {
        // 6 hướng hex (flat-top)
        float angle = dir * 60f * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }

    // =========================================================================
    // Gizmos
    // =========================================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, spacing * Mathf.Sqrt(initialCount) * 0.7f);

        if (pattern == SpawnPattern.Ring)
        {
            Gizmos.color = Color.cyan;
            DrawWireCircle(transform.position, ringRadius, 32);
        }
    }

    private static void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float a0 = i       * Mathf.PI * 2f / segments;
            float a1 = (i + 1) * Mathf.PI * 2f / segments;
            Gizmos.DrawLine(
                center + new Vector3(Mathf.Cos(a0) * radius, 0, Mathf.Sin(a0) * radius),
                center + new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius));
        }
    }
}
