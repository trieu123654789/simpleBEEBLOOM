// =============================================================================
// BeeSustainabilityManager - Singleton quản lý 3 chỉ số Phát triển Bền vững.
// BeeSustainabilityManager - Singleton managing 3 Sustainability pillars.
//
// 3 PILLARS / 3 TRỤ CỘT
//   🌿 Environment (Môi trường) : thụ phấn đúng, không khai thác quá mức
//   💰 Economic (Kinh tế)       : giao đủ mật về tổ, không lãng phí
//   👥 Social (Xã hội)          : thụ phấn đa dạng loài, đa dạng sinh học
//
// THANG ĐO / SCALE
//   Mỗi pillar từ 0 đến 100 (float).
//   Score tổng = trung bình 3 pillar.
//
// NGƯỠNG KẾT QUẢ / OUTCOME THRESHOLDS
//   > 70 : Bội thu — hoa nở thêm, NPC vui vẻ
//   40-70: Ổn định — không sụp đổ, không phát triển
//   < 40 : Suy thoái — hoa bắt đầu héo nhanh hơn
//   < 20 : Sụp đổ — Game Over
//
// DECAY / SUY GIẢM TỰ NHIÊN
//   Mỗi pillar tự giảm theo thời gian (decay rate) nếu không được "nạp lại".
//   Mô phỏng thực tế: không chăm sóc → hệ sinh thái suy giảm.
// =============================================================================
using UnityEngine;

[DisallowMultipleComponent]
public class BeeSustainabilityManager : MonoBehaviour
{
    // =========================================================================
    // SINGLETON
    // =========================================================================
    public static BeeSustainabilityManager Instance { get; private set; }

    // =========================================================================
    // PILLAR SETTINGS / CÀI ĐẶT TRỤ CỘT
    // =========================================================================
    [Header("Environment / Môi trường 🌿")]
    [Tooltip("Điểm môi trường ban đầu (0-100).")]
    [Range(0f, 100f)]
    public float environmentStart = 50f;

    [Tooltip("Mức tối đa điểm môi trường.")]
    public float environmentMax = 100f;

    [Tooltip("Điểm môi trường giảm mỗi giây khi không có hoạt động thụ phấn.")]
    [Range(0f, 5f)]
    public float environmentDecayPerSecond = 0.5f;

    [Header("Economic / Kinh tế 💰")]
    [Tooltip("Điểm kinh tế ban đầu (0-100).")]
    [Range(0f, 100f)]
    public float economicStart = 50f;

    [Tooltip("Mức tối đa điểm kinh tế.")]
    public float economicMax = 100f;

    [Tooltip("Điểm kinh tế giảm mỗi giây khi không giao mật.")]
    [Range(0f, 5f)]
    public float economicDecayPerSecond = 0.3f;

    [Header("Social / Xã hội 👥")]
    [Tooltip("Điểm xã hội ban đầu (0-100).")]
    [Range(0f, 100f)]
    public float socialStart = 50f;

    [Tooltip("Mức tối đa điểm xã hội.")]
    public float socialMax = 100f;

    [Tooltip("Điểm xã hội giảm mỗi giây (chậm nhất vì đa dạng sinh học bền hơn).")]
    [Range(0f, 5f)]
    public float socialDecayPerSecond = 0.2f;

    [Header("Thresholds / Ngưỡng kết quả")]
    [Tooltip("Ngưỡng bội thu — > giá trị này thì hoa nở thêm.")]
    public float thresholdThriving = 70f;

    [Tooltip("Ngưỡng suy thoái — < giá trị này thì hoa héo nhanh hơn.")]
    public float thresholdDecline = 40f;

    [Tooltip("Ngưỡng sụp đổ — < giá trị này → Game Over.")]
    public float thresholdCollapse = 20f;

    // =========================================================================
    // PUBLIC STATE / TRẠNG THÁI
    // =========================================================================
    /// <summary>Điểm môi trường hiện tại (0-100).</summary>
    public float EnvironmentScore { get; private set; }

    /// <summary>Điểm kinh tế hiện tại (0-100).</summary>
    public float EconomicScore { get; private set; }

    /// <summary>Điểm xã hội hiện tại (0-100).</summary>
    public float SocialScore { get; private set; }

    /// <summary>Điểm bền vững tổng (trung bình 3 pillar, 0-100).</summary>
    public float TotalScore => (EnvironmentScore + EconomicScore + SocialScore) / 3f;

    /// <summary>Điểm tổng chuẩn hóa 0-1.</summary>
    public float TotalScore01 => TotalScore / 100f;

    /// <summary>Trạng thái hệ sinh thái hiện tại.</summary>
    public EcosystemStatus Status { get; private set; }

    // =========================================================================
    // ENUMS
    // =========================================================================
    public enum EcosystemStatus
    {
        Thriving,   // Bội thu
        Stable,     // Ổn định
        Declining,  // Suy thoái
        Collapsed   // Sụp đổ
    }

    // =========================================================================
    // EVENTS / SỰ KIỆN
    // =========================================================================
    /// <summary>Phát khi điểm bất kỳ pillar thay đổi. (env, eco, social, total)</summary>
    public System.Action<float, float, float, float> OnScoresChanged;

    /// <summary>Phát khi trạng thái hệ sinh thái thay đổi.</summary>
    public System.Action<EcosystemStatus> OnStatusChanged;

    /// <summary>Phát khi hệ sinh thái sụp đổ (Game Over condition).</summary>
    public System.Action OnCollapsed;

    // =========================================================================
    // INTERNAL
    // =========================================================================
    private EcosystemStatus _lastStatus;
    private bool _collapsed;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnvironmentScore = environmentStart;
        EconomicScore = economicStart;
        SocialScore = socialStart;
        _lastStatus = ComputeStatus();
        Status = _lastStatus;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (_collapsed) return;

        ApplyDecay();
        UpdateStatus();
        NotifyChange();
    }

    // =========================================================================
    // PUBLIC API — Thêm điểm các pillar
    // =========================================================================

    /// <summary>Thêm điểm môi trường (dương = tốt, âm = xấu).</summary>
    public void AddEnvironmentPoints(float points)
    {
        EnvironmentScore = Mathf.Clamp(EnvironmentScore + points, 0f, environmentMax);
    }

    /// <summary>Thêm điểm kinh tế.</summary>
    public void AddEconomicPoints(float points)
    {
        EconomicScore = Mathf.Clamp(EconomicScore + points, 0f, economicMax);
    }

    /// <summary>Thêm điểm xã hội.</summary>
    public void AddSocialPoints(float points)
    {
        SocialScore = Mathf.Clamp(SocialScore + points, 0f, socialMax);
    }

    /// <summary>
    /// Gọi khi hoa chết — môi trường bị ảnh hưởng nặng.
    /// </summary>
    public void OnFlowerDied()
    {
        AddEnvironmentPoints(-20f);
        Debug.Log($"[BeeSustainabilityManager] Hoa chết! Env: {EnvironmentScore:F0}");
    }

    /// <summary>
    /// Gọi khi ong giao mật về tổ thành công.
    /// </summary>
    public void OnHoneyDelivered(int amount)
    {
        AddEconomicPoints(amount * 10f);
        Debug.Log($"[BeeSustainabilityManager] Giao {amount} mật. Eco: {EconomicScore:F0}");
    }

    /// <summary>Reset tất cả về giá trị ban đầu.</summary>
    public void ResetAll()
    {
        EnvironmentScore = environmentStart;
        EconomicScore = economicStart;
        SocialScore = socialStart;
        _collapsed = false;
        Status = ComputeStatus();
        _lastStatus = Status;
    }

    // =========================================================================
    // ApplyDecay - Suy giảm tự nhiên mỗi giây.
    // =========================================================================
    private void ApplyDecay()
    {
        EnvironmentScore = Mathf.Max(0f, EnvironmentScore - environmentDecayPerSecond * Time.deltaTime);
        EconomicScore    = Mathf.Max(0f, EconomicScore    - economicDecayPerSecond    * Time.deltaTime);
        SocialScore      = Mathf.Max(0f, SocialScore      - socialDecayPerSecond      * Time.deltaTime);
    }

    // =========================================================================
    // UpdateStatus - Tính trạng thái mới và phát event nếu đổi.
    // =========================================================================
    private void UpdateStatus()
    {
        var newStatus = ComputeStatus();
        if (newStatus == _lastStatus) return;

        _lastStatus = newStatus;
        Status = newStatus;
        OnStatusChanged?.Invoke(Status);

        if (Status == EcosystemStatus.Collapsed && !_collapsed)
        {
            _collapsed = true;
            OnCollapsed?.Invoke();
            Debug.Log("[BeeSustainabilityManager] ⚠️ Hệ sinh thái SỤP ĐỔ! Game Over.");
        }

        Debug.Log($"[BeeSustainabilityManager] Trạng thái: {Status} | " +
                  $"Env={EnvironmentScore:F0} Eco={EconomicScore:F0} Soc={SocialScore:F0} | " +
                  $"Total={TotalScore:F0}");
    }

    // =========================================================================
    // ComputeStatus - Xác định trạng thái từ điểm tổng.
    // =========================================================================
    private EcosystemStatus ComputeStatus()
    {
        float total = TotalScore;
        if (total >= thresholdThriving) return EcosystemStatus.Thriving;
        if (total >= thresholdDecline)  return EcosystemStatus.Stable;
        if (total >= thresholdCollapse) return EcosystemStatus.Declining;
        return EcosystemStatus.Collapsed;
    }

    // =========================================================================
    // NotifyChange - Phát event OnScoresChanged mỗi frame (HUD đọc).
    // =========================================================================
    private void NotifyChange()
    {
        OnScoresChanged?.Invoke(EnvironmentScore, EconomicScore, SocialScore, TotalScore);
    }

    // =========================================================================
    // Inspector Debug
    // =========================================================================
    [ContextMenu("Simulate Pollination Event")]
    private void DebugPollination()
    {
        AddEnvironmentPoints(15f);
        AddSocialPoints(10f);
        Debug.Log("[Debug] Simulated pollination.");
    }

    [ContextMenu("Simulate Honey Delivery")]
    private void DebugHoney()
    {
        OnHoneyDelivered(1);
        Debug.Log("[Debug] Simulated honey delivery.");
    }

    [ContextMenu("Simulate Flower Death")]
    private void DebugFlowerDeath()
    {
        OnFlowerDied();
        Debug.Log("[Debug] Simulated flower death.");
    }
}
