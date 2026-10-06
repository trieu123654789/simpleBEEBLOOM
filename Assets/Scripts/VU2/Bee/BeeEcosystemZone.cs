// =============================================================================
// BeeEcosystemZone - Quản lý một vùng hệ sinh thái (nhóm hoa cùng khu vực).
// BeeEcosystemZone - Manages one ecosystem zone (group of flowers in an area).
//
// MỤC ĐÍCH / PURPOSE
//   Một scene có thể có nhiều zone (ví dụ: vùng Sen, vùng Dừa, vùng Lúa).
//   Mỗi zone:
//     - Chứa danh sách BeeFlower
//     - Tính "zone health" = % hoa còn khoẻ trong zone
//     - Khi zone health xuống thấp → spawn thêm ong dịch (enemy bee)
//     - Khi zone thriving → spawn thêm hoa mới (bonus spawn)
//
// SUSTAINABILITY FEEDBACK LOOP
//   Zone healthy ←→ Ong thụ phấn nhiều ←→ Score cao ←→ Zone phát triển
//   Zone dying   ←→ Ong bỏ bê / khai thác quá mức ←→ Score thấp ←→ Zone chết
// =============================================================================
using System.Collections.Generic;
using UnityEngine;

public class BeeEcosystemZone : MonoBehaviour
{
    // =========================================================================
    // SETTINGS / CÀI ĐẶT
    // =========================================================================
    [Header("Zone Identity")]
    [Tooltip("Tên vùng hiển thị trên HUD.")]
    public string zoneName = "Vùng Sen";

    [Tooltip("Loài hoa đặc trưng của vùng này.")]
    public BeeFlower.FlowerSpecies dominantSpecies = BeeFlower.FlowerSpecies.Lotus;

    [Header("Flowers / Hoa")]
    [Tooltip("Danh sách tất cả BeeFlower trong zone này. Để trống = tự tìm từ children.")]
    public List<BeeFlower> flowers = new();

    [Header("Spawn Settings / Spawn hoa mới")]
    [Tooltip("Prefab hoa để spawn khi zone đang thriving.")]
    public GameObject flowerPrefab;

    [Tooltip("Số hoa tối đa trong zone. Không spawn thêm nếu đã đủ.")]
    public int maxFlowers = 8;

    [Tooltip("Khoảng thời gian giữa các lần kiểm tra spawn hoa mới (giây).")]
    [Range(5f, 60f)]
    public float spawnCheckInterval = 15f;

    [Header("Enemy Bee Trigger")]
    [Tooltip("Ngưỡng zone health (0-1) để spawn ong dịch. 0 = không bao giờ spawn.")]
    [Range(0f, 1f)]
    public float enemySpawnThreshold = 0.3f;

    [Tooltip("Prefab ong dịch.")]
    public GameObject enemyBeePrefab;

    [Tooltip("Số lượng ong dịch tối đa trong zone.")]
    [Range(0, 5)]
    public int maxEnemyBees = 2;

    [Header("Visual Feedback")]
    [Tooltip("Particle phát ra khi zone đang thriving (hoa nở rộ).")]
    public ParticleSystem thrivingVFX;

    [Tooltip("Fog hoặc particle mù mờ khi zone đang chết.")]
    public ParticleSystem dyingVFX;

    // =========================================================================
    // PUBLIC STATE / TRẠNG THÁI
    // =========================================================================
    /// <summary>Health của zone: % hoa còn khoẻ (Healthy/Recovering/Wilting), 0-1.</summary>
    public float ZoneHealth { get; private set; }

    /// <summary>Số hoa đang khoẻ (Healthy).</summary>
    public int HealthyFlowerCount { get; private set; }

    /// <summary>Số hoa đã chết trong zone.</summary>
    public int DeadFlowerCount { get; private set; }

    /// <summary>Zone đang bội thu (health > 0.7).</summary>
    public bool IsThriving => ZoneHealth > 0.7f;

    /// <summary>Zone đang suy thoái (health < 0.3).</summary>
    public bool IsDeclining => ZoneHealth < 0.3f;

    // =========================================================================
    // EVENTS
    // =========================================================================
    /// <summary>Phát khi zone health thay đổi.</summary>
    public System.Action<float> OnZoneHealthChanged;

    // =========================================================================
    // INTERNAL
    // =========================================================================
    private float _spawnTimer;
    private readonly List<GameObject> _spawnedEnemies = new();
    private BeeSustainabilityManager _sustainability;
    private float _lastHealth = -1f;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        _sustainability = FindObjectOfType<BeeSustainabilityManager>();

        // Tự tìm hoa từ children nếu list trống
        if (flowers.Count == 0)
        {
            GetComponentsInChildren(flowers);
        }

        // Đăng ký events từ mỗi hoa
        foreach (var f in flowers)
        {
            if (f == null) continue;
            f.OnDied  += HandleFlowerDied;
            f.OnWilted += HandleFlowerWilted;
        }

        _spawnTimer = spawnCheckInterval;
    }

    private void OnDestroy()
    {
        foreach (var f in flowers)
        {
            if (f == null) continue;
            f.OnDied  -= HandleFlowerDied;
            f.OnWilted -= HandleFlowerWilted;
        }
    }

    private void Update()
    {
        RecalculateHealth();
        UpdateVFX();
        UpdateSpawnTimer();
    }

    // =========================================================================
    // RecalculateHealth - Tính lại health zone mỗi frame.
    // =========================================================================
    private void RecalculateHealth()
    {
        int total = flowers.Count;
        if (total == 0) { ZoneHealth = 0f; return; }

        int healthy = 0, dead = 0;
        foreach (var f in flowers)
        {
            if (f == null) { dead++; continue; }
            switch (f.State)
            {
                case BeeFlower.FlowerState.Healthy:
                case BeeFlower.FlowerState.Recovering:
                    healthy++;
                    break;
                case BeeFlower.FlowerState.Dead:
                    dead++;
                    break;
            }
        }

        HealthyFlowerCount = healthy;
        DeadFlowerCount = dead;
        ZoneHealth = (float)healthy / total;

        if (Mathf.Abs(ZoneHealth - _lastHealth) > 0.01f)
        {
            _lastHealth = ZoneHealth;
            OnZoneHealthChanged?.Invoke(ZoneHealth);
        }
    }

    // =========================================================================
    // UpdateVFX - Bật/tắt hiệu ứng theo zone health.
    // =========================================================================
    private void UpdateVFX()
    {
        if (thrivingVFX != null)
        {
            if (IsThriving && !thrivingVFX.isPlaying) thrivingVFX.Play();
            else if (!IsThriving && thrivingVFX.isPlaying) thrivingVFX.Stop();
        }

        if (dyingVFX != null)
        {
            if (IsDeclining && !dyingVFX.isPlaying) dyingVFX.Play();
            else if (!IsDeclining && dyingVFX.isPlaying) dyingVFX.Stop();
        }
    }

    // =========================================================================
    // UpdateSpawnTimer - Kiểm tra spawn hoa mới và ong dịch.
    // =========================================================================
    private void UpdateSpawnTimer()
    {
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f) return;
        _spawnTimer = spawnCheckInterval;

        if (IsThriving) TrySpawnFlower();
        if (IsDeclining) TrySpawnEnemy();

        // Dọn dẹp enemies đã bị tiêu diệt
        _spawnedEnemies.RemoveAll(e => e == null);
    }

    // =========================================================================
    // TrySpawnFlower - Spawn thêm hoa khi zone đang bội thu.
    // =========================================================================
    private void TrySpawnFlower()
    {
        if (flowerPrefab == null) return;
        if (flowers.Count >= maxFlowers) return;

        Vector3 spawnPos = GetRandomSpawnPosition();
        var go = Instantiate(flowerPrefab, spawnPos, Quaternion.identity, transform);
        var newFlower = go.GetComponent<BeeFlower>();

        if (newFlower != null)
        {
            newFlower.species = dominantSpecies;
            newFlower.OnDied  += HandleFlowerDied;
            newFlower.OnWilted += HandleFlowerWilted;
            flowers.Add(newFlower);

            Debug.Log($"[BeeEcosystemZone] {zoneName}: Hoa mới nở! Tổng: {flowers.Count}");
        }
    }

    // =========================================================================
    // TrySpawnEnemy - Spawn ong dịch khi zone đang suy thoái.
    // =========================================================================
    private void TrySpawnEnemy()
    {
        if (enemyBeePrefab == null) return;
        if (_spawnedEnemies.Count >= maxEnemyBees) return;

        Vector3 spawnPos = GetRandomSpawnPosition() + Vector3.up * 2f;
        var enemy = Instantiate(enemyBeePrefab, spawnPos, Quaternion.identity);
        _spawnedEnemies.Add(enemy);

        Debug.Log($"[BeeEcosystemZone] {zoneName}: Ong dịch xuất hiện! ({_spawnedEnemies.Count}/{maxEnemyBees})");
    }

    // =========================================================================
    // Event Handlers
    // =========================================================================
    private void HandleFlowerDied()
    {
        _sustainability?.OnFlowerDied();
        Debug.Log($"[BeeEcosystemZone] {zoneName}: Một bông hoa đã CHẾT. Health: {ZoneHealth:P0}");
    }

    private void HandleFlowerWilted()
    {
        // Cảnh báo cho người chơi qua GameStats nếu cần
        Debug.Log($"[BeeEcosystemZone] {zoneName}: Một bông hoa đang HÉO. Hãy thụ phấn ngay!");
    }

    // =========================================================================
    // GetRandomSpawnPosition - Vị trí ngẫu nhiên trong zone.
    // =========================================================================
    private Vector3 GetRandomSpawnPosition()
    {
        // Lấy random từ các hoa hiện có làm điểm tham chiếu
        if (flowers.Count > 0)
        {
            var refFlower = flowers[Random.Range(0, flowers.Count)];
            if (refFlower != null)
            {
                return refFlower.transform.position
                     + new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            }
        }

        // Fallback: ngẫu nhiên gần zone center
        return transform.position + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
    }

    // =========================================================================
    // Gizmos
    // =========================================================================
    private void OnDrawGizmos()
    {
        Color c = flowers.Count == 0 ? Color.gray :
                  IsThriving ? Color.green :
                  IsDeclining ? Color.red : Color.yellow;

        Gizmos.color = new Color(c.r, c.g, c.b, 0.2f);
        Gizmos.DrawSphere(transform.position, 5f);

        Gizmos.color = c;
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 6f,
            $"{zoneName}\n♥ {ZoneHealth:P0}");
#endif
    }
}
