// =============================================================================
// BeeHive - Tổ ong: điểm giao mật, tăng EconomicScore.
// BeeHive - Beehive delivery point: bee flies in → delivers nectar → Economic pillar grows.
//
// LUỒNG TƯƠNG TÁC / INTERACTION FLOW
//   1. Ong bay vào trigger zone của tổ (hoặc trong deliveryRadius)
//   2. Nếu ong đang mang mật (nectar01 > 0), tự động giao (hoặc yêu cầu nhấn nút)
//   3. EconomicScore += nectar giao × economicPointsPerNectar
//   4. BeeGameStats.AddHoney(1) per batch
//   5. VFX + SFX, tổ đổi trạng thái
//
// TRẠNG THÁI TỔ / HIVE STATES
//   Empty    : chưa có mật nào được giao
//   Filling  : đang tích lũy mật
//   Full     : đạt hiveCapacity → bonus social points (tổ đầy = cộng đồng vui)
//   Overflow : vượt capacity → không nhận thêm (gameplay pressure)
//
// TÍCH HỢP / INTEGRATION
//   BeeSustainabilityManager.OnHoneyDelivered(amount)
//   BeeGameStats.AddHoney(1)  // per honeyBatchSize nectar
// =============================================================================
using UnityEngine;

[DisallowMultipleComponent]
public class BeeHive : MonoBehaviour
{
    // =========================================================================
    // HIVE SETTINGS / CÀI ĐẶT TỔ ONG
    // =========================================================================
    [Header("Delivery / Giao mật")]
    [Tooltip("Bán kính vùng tổ (m). Ong vào đây sẽ tự giao mật.")]
    [Range(0.5f, 5f)]
    public float deliveryRadius = 2f;

    [Tooltip("Điểm kinh tế tăng cho mỗi đơn vị mật hoa (nectar01) giao về.")]
    [Range(1f, 50f)]
    public float economicPointsPerNectar = 25f;

    [Tooltip("Lượng mật hoa (nectar01) tích lũy = 1 đơn vị mật ong.")]
    [Range(0.1f, 1f)]
    public float honeyBatchSize = 0.5f;

    [Tooltip("Sức chứa tổ (đơn vị mật ong). Khi đầy → nhận bonus xã hội.")]
    [Range(5, 50)]
    public int hiveCapacity = 20;

    [Header("Auto Delivery")]
    [Tooltip("Tự động giao mật khi ong vào tổ (không cần nhấn nút).")]
    public bool autoDeliver = true;

    [Tooltip("Tốc độ giao mật (nectar01/giây) khi autoDeliver = true.")]
    [Range(0.1f, 2f)]
    public float deliveryRate = 0.8f;

    // =========================================================================
    // REFERENCES / THAM CHIẾU
    // =========================================================================
    [Header("References / Tham chiếu")]
    [Tooltip("BeeGameStats của player. Để trống sẽ tự tìm.")]
    public BeeGameStats playerStats;

    [Tooltip("BeeSustainabilityManager. Để trống sẽ tự tìm.")]
    public BeeSustainabilityManager sustainability;

    [Header("Visual / Hình ảnh")]
    [Tooltip("Renderer của tổ để đổi màu theo trạng thái.")]
    public Renderer hiveRenderer;

    [Tooltip("Particle phát ra khi nhận mật.")]
    public ParticleSystem deliveryVFX;

    [Tooltip("Particle phát ra khi tổ đầy.")]
    public ParticleSystem fullVFX;

    [Header("Audio")]
    public AudioSource deliveryAudio;
    public AudioSource fullAudio;

    // =========================================================================
    // ENUMS
    // =========================================================================
    public enum HiveState { Empty, Filling, Full, Overflow }

    // =========================================================================
    // PUBLIC STATE / TRẠNG THÁI
    // =========================================================================
    /// <summary>Tổng mật ong đã nhận (đơn vị nguyên).</summary>
    public int HoneyStored { get; private set; }

    /// <summary>Mật hoa lẻ đang tích lũy (0 đến honeyBatchSize).</summary>
    public float NectarBuffer { get; private set; }

    /// <summary>Trạng thái tổ hiện tại.</summary>
    public HiveState State { get; private set; } = HiveState.Empty;

    /// <summary>Tỉ lệ đầy tổ (0-1).</summary>
    public float FillRatio => hiveCapacity > 0 ? Mathf.Clamp01((float)HoneyStored / hiveCapacity) : 0f;

    /// <summary>Ong hiện đang trong vùng tổ.</summary>
    public bool IsBeeInside { get; private set; }

    // =========================================================================
    // EVENTS / SỰ KIỆN
    // =========================================================================
    /// <summary>Phát khi nhận một batch mật ong (param = tổng honey đã có).</summary>
    public System.Action<int> OnHoneyReceived;

    /// <summary>Phát khi tổ đầy lần đầu.</summary>
    public System.Action OnHiveFull;

    // =========================================================================
    // INTERNAL
    // =========================================================================
    private Transform _beeTransform;
    private BeePollinationSystem _pollination;
    private static readonly int ColorPropId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock _mpb;
    private bool _fullTriggered;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        if (hiveRenderer == null) hiveRenderer = GetComponentInChildren<Renderer>();

        if (sustainability == null)
            sustainability = FindObjectOfType<BeeSustainabilityManager>();

        // Tìm player
        var beeCtrl = FindObjectOfType<BeeController>();
        if (beeCtrl != null)
        {
            _beeTransform = beeCtrl.transform;
            playerStats     = playerStats     ?? beeCtrl.GetComponent<BeeGameStats>();
            _pollination    = beeCtrl.GetComponent<BeePollinationSystem>();
        }

        UpdateVisual();
    }

    private void Update()
    {
        CheckBeeProximity();
        if (IsBeeInside && autoDeliver) ProcessDelivery();
        UpdateState();
        UpdateVisual();
    }

    // =========================================================================
    // CheckBeeProximity - Kiểm tra ong có trong vùng tổ không.
    // =========================================================================
    private void CheckBeeProximity()
    {
        if (_beeTransform == null) { IsBeeInside = false; return; }

        float dist = Vector3.Distance(transform.position, _beeTransform.position);
        bool wasInside = IsBeeInside;
        IsBeeInside = dist <= deliveryRadius;

        if (IsBeeInside && !wasInside)
            Debug.Log($"[BeeHive] Ong vào tổ. Mật đang mang: {playerStats?.nectar01:F2}");
    }

    // =========================================================================
    // ProcessDelivery - Tự động hút mật từ bình ong khi ở trong tổ.
    // =========================================================================
    private void ProcessDelivery()
    {
        if (playerStats == null) return;
        if (State == HiveState.Overflow) return;
        if (playerStats.nectar01 <= 0f) return;

        float toDeliver = Mathf.Min(deliveryRate * Time.deltaTime, playerStats.nectar01);
        playerStats.nectar01 = Mathf.Max(0f, playerStats.nectar01 - toDeliver);

        NectarBuffer += toDeliver;

        // Đủ một batch → đổi thành mật ong
        while (NectarBuffer >= honeyBatchSize && HoneyStored < hiveCapacity)
        {
            NectarBuffer -= honeyBatchSize;
            HoneyStored++;
            playerStats.AddHoney(1);
            sustainability?.OnHoneyDelivered(1);

            OnHoneyReceived?.Invoke(HoneyStored);
            PlayVFX(deliveryVFX);
            deliveryAudio?.Play();

            Debug.Log($"[BeeHive] +1 mật ong! Tổng: {HoneyStored}/{hiveCapacity}");
        }
    }

    // =========================================================================
    // DeliverManual - Giao mật thủ công (khi autoDeliver = false).
    // =========================================================================
    public void DeliverManual()
    {
        if (!IsBeeInside) return;
        ProcessDelivery();
    }

    // =========================================================================
    // UpdateState - Cập nhật trạng thái tổ.
    // =========================================================================
    private void UpdateState()
    {
        HiveState newState;
        if (HoneyStored <= 0)            newState = HiveState.Empty;
        else if (HoneyStored >= hiveCapacity) newState = HiveState.Full;
        else                             newState = HiveState.Filling;

        // Overflow nếu buffer còn nhưng tổ đầy
        if (HoneyStored >= hiveCapacity && NectarBuffer > 0f) newState = HiveState.Overflow;

        if (newState == State) return;
        State = newState;

        if (State == HiveState.Full && !_fullTriggered)
        {
            _fullTriggered = true;
            // Social bonus: tổ đầy = cộng đồng hưởng lợi
            sustainability?.AddSocialPoints(30f);
            PlayVFX(fullVFX);
            fullAudio?.Play();
            OnHiveFull?.Invoke();
            Debug.Log("[BeeHive] 🍯 TỔ ĐẦY! Social bonus +30.");
        }
    }

    // =========================================================================
    // UpdateVisual - Đổi màu tổ theo trạng thái.
    // =========================================================================
    private void UpdateVisual()
    {
        if (hiveRenderer == null) return;

        Color c = State switch
        {
            HiveState.Empty    => new Color(0.6f, 0.4f, 0.1f, 1f),  // Nâu nhạt
            HiveState.Filling  => Color.Lerp(
                                    new Color(0.6f, 0.4f, 0.1f, 1f),
                                    new Color(1f,   0.8f, 0f,   1f),
                                    FillRatio),
            HiveState.Full     => new Color(1f,   0.8f, 0f,   1f),  // Vàng mật
            HiveState.Overflow => new Color(1f,   0.5f, 0f,   1f),  // Cam
            _                  => Color.white
        };

        hiveRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(ColorPropId, c);
        hiveRenderer.SetPropertyBlock(_mpb);
    }

    // =========================================================================
    // Reset
    // =========================================================================
    public void ResetHive()
    {
        HoneyStored    = 0;
        NectarBuffer   = 0f;
        State          = HiveState.Empty;
        _fullTriggered = false;
        UpdateVisual();
    }

    // =========================================================================
    // PlayVFX
    // =========================================================================
    private static void PlayVFX(ParticleSystem ps)
    {
        if (ps != null && !ps.isPlaying) ps.Play();
    }

    // =========================================================================
    // Gizmos
    // =========================================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, deliveryRadius);
        Gizmos.color = new Color(1f, 0.8f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, deliveryRadius);
    }
}
