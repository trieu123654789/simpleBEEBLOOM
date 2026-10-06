// =============================================================================
// BeePollinationSystem - Hệ thống thụ phấn: ong mang phấn từ hoa A đến hoa B.
// BeePollinationSystem - Pollination system: bee carries pollen between flowers.
//
// VÒNG ĐỜI THỤ PHẤN / POLLINATION LIFECYCLE
//   1. Ong đến hoa A (có phấn) → Grip trigger → lấy phấn (pollenCarried = loài A)
//   2. Ong bay đến hoa B cùng loài → Grip trigger → thụ phấn thành công
//   3. VFX phấn phát ra, social score tăng nếu là loài mới
//   4. Phấn mất đi, ong cần quay lại hoa khác để lấy phấn mới
//
// INTERACTION MODEL / MÔ HÌNH TƯƠNG TÁC (giải thích thêm)
//   - Trigger (R) giữ khi gần hoa → hút mật vào bình
//   - Grip (R) nhấn khi gần hoa có phấn → lấy phấn
//   - Grip (R) nhấn khi gần hoa cùng loài → thụ phấn
//   Script này quản lý logic đó và giao tiếp với BeeGameStats và BeeSustainabilityManager.
// =============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class BeePollinationSystem : MonoBehaviour
{
    // =========================================================================
    // REFERENCES / THAM CHIẾU
    // =========================================================================
    [Header("References / Tham chiếu")]
    [Tooltip("BeeController để biết ong đang ở đâu.")]
    public BeeController bee;

    [Tooltip("BeeGameStats để cập nhật mật hoa và mật ong.")]
    public BeeGameStats stats;

    [Tooltip("BeeSustainabilityManager để cập nhật 3 chỉ số bền vững.")]
    public BeeSustainabilityManager sustainability;

    [Tooltip("BeeQuestManager để tracking pollination progress. Để trống sẽ tự tìm.")]
    public BeeQuestManager questManager;

    // =========================================================================
    // INPUT / ĐẦU VÀO
    // =========================================================================
    [Header("Input Actions")]
    [Tooltip("Action hút mật (giữ trigger khi gần hoa). XR Right Trigger.")]
    public InputActionProperty collectNectarAction;

    [Tooltip("Action lấy/thả phấn (grip). XR Right Grip.")]
    public InputActionProperty pollinateAction;

    // =========================================================================
    // SETTINGS / CÀI ĐẶT
    // =========================================================================
    [Header("Detection / Phát hiện")]
    [Tooltip("Khoảng cách tối đa ong có thể tương tác với hoa (meter).")]
    [Range(0.5f, 5f)]
    public float detectionRange = 2f;

    [Tooltip("Layer mask của hoa trong scene.")]
    public LayerMask flowerLayer = ~0;

    [Header("VFX / Hiệu ứng")]
    [Tooltip("Particle phấn hoa bay ra khi thụ phấn thành công.")]
    public ParticleSystem pollinationVFX;

    [Tooltip("Particle mật hoa nhỏ giọt khi hút mật.")]
    public ParticleSystem nectarVFX;

    [Header("Audio / Âm thanh")]
    [Tooltip("Tiếng ong hút mật.")]
    public AudioSource nectarAudio;

    [Tooltip("Tiếng thụ phấn thành công.")]
    public AudioSource pollinationAudio;

    // =========================================================================
    // PUBLIC STATE / TRẠNG THÁI
    // =========================================================================
    /// <summary>Phấn ong đang mang (null = không có).</summary>
    public BeeFlower.FlowerSpecies? CarriedPollenSpecies { get; private set; }

    /// <summary>Hoa gần nhất hiện tại (trong detectionRange).</summary>
    public BeeFlower NearestFlower { get; private set; }

    /// <summary>Đang hút mật không.</summary>
    public bool IsCollectingNectar { get; private set; }

    // =========================================================================
    // INTERNAL
    // =========================================================================
    private readonly HashSet<BeeFlower.FlowerSpecies> _pollinatedSpeciesThisRun = new();
    private readonly Collider[] _overlapBuffer = new Collider[16];

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        if (bee == null) bee = GetComponent<BeeController>();
        if (stats == null) stats = GetComponent<BeeGameStats>();
        if (sustainability == null) sustainability = FindObjectOfType<BeeSustainabilityManager>();
        if (questManager   == null) questManager   = FindObjectOfType<BeeQuestManager>();
    }

    private void OnEnable()
    {
        collectNectarAction.action?.Enable();
        pollinateAction.action?.Enable();
    }

    private void OnDisable()
    {
        collectNectarAction.action?.Disable();
        pollinateAction.action?.Disable();
    }

    private void Update()
    {
        FindNearestFlower();
        HandleNectarCollection();
        HandlePollination();
    }

    // =========================================================================
    // FindNearestFlower - OverlapSphere để tìm hoa gần nhất.
    // =========================================================================
    private void FindNearestFlower()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, detectionRange, _overlapBuffer, flowerLayer);

        BeeFlower nearest = null;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var flower = _overlapBuffer[i].GetComponentInParent<BeeFlower>();
            if (flower == null || !flower.IsAlive) continue;

            float dist = Vector3.Distance(transform.position, flower.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = flower;
            }
        }

        NearestFlower = nearest;
    }

    // =========================================================================
    // HandleNectarCollection - Hút mật khi giữ trigger gần hoa.
    // =========================================================================
    private void HandleNectarCollection()
    {
        bool triggerHeld = collectNectarAction.action != null
                        && collectNectarAction.action.IsPressed();

        if (triggerHeld && NearestFlower != null && NearestFlower.IsInteractable)
        {
            float collected = NearestFlower.CollectNectar(Time.deltaTime);
            if (collected > 0f)
            {
                IsCollectingNectar = true;
                stats?.AddNectar01(collected);
                sustainability?.AddEnvironmentPoints(collected * 5f); // nhỏ giọt từ từ

                PlayVFX(nectarVFX, NearestFlower.transform.position);
            }
            else
            {
                IsCollectingNectar = false;
            }
        }
        else
        {
            IsCollectingNectar = false;
        }

        // Âm thanh hút mật
        if (nectarAudio != null)
        {
            if (IsCollectingNectar && !nectarAudio.isPlaying) nectarAudio.Play();
            else if (!IsCollectingNectar && nectarAudio.isPlaying) nectarAudio.Stop();
        }
    }

    // =========================================================================
    // HandlePollination - Lấy/thả phấn khi nhấn grip gần hoa.
    // =========================================================================
    private void HandlePollination()
    {
        bool gripDown = pollinateAction.action != null
                     && pollinateAction.action.WasPressedThisFrame();

        if (!gripDown || NearestFlower == null) return;

        // Trường hợp 1: Chưa có phấn → lấy phấn từ hoa gần nhất
        if (!CarriedPollenSpecies.HasValue)
        {
            if (NearestFlower.TakePollen(out var pollen))
            {
                CarriedPollenSpecies = pollen;
                Debug.Log($"[BeePollinationSystem] Lấy phấn {pollen} từ {NearestFlower.name}");
            }
            return;
        }

        // Trường hợp 2: Đang mang phấn → thử thụ phấn cho hoa gần nhất
        bool success = NearestFlower.ReceivePollen(CarriedPollenSpecies.Value);

        if (success)
        {
            OnPollinationSuccess(NearestFlower.species);
            CarriedPollenSpecies = null; // Phấn đã được dùng
        }
        else
        {
            // Sai loài → feedback thất bại (không mất phấn)
            BeeAudioManager.Instance?.PlaySFX(BeeAudioManager.SFXType.FlowerWilt,
                NearestFlower?.transform.position ?? transform.position);
            Debug.Log($"[BeePollinationSystem] Sai loài: cần {NearestFlower.species}, đang mang {CarriedPollenSpecies}");
        }
    }

    // =========================================================================
    // OnPollinationSuccess - Cập nhật điểm khi thụ phấn thành công.
    // =========================================================================
    private void OnPollinationSuccess(BeeFlower.FlowerSpecies species)
    {
        PlayVFX(pollinationVFX, NearestFlower?.transform.position ?? transform.position);
        pollinationAudio?.Play();
        BeeAudioManager.Instance?.PlaySFX(BeeAudioManager.SFXType.Pollinate,
            NearestFlower?.transform.position ?? transform.position);

        // Điểm môi trường: thụ phấn = sinh sản = tốt cho môi trường
        sustainability?.AddEnvironmentPoints(15f);

        // Điểm xã hội: thụ phấn loài mới = đa dạng sinh học tăng
        bool isNewSpecies = _pollinatedSpeciesThisRun.Add(species);
        if (isNewSpecies)
        {
            sustainability?.AddSocialPoints(20f);  // Bonus đa dạng sinh học
            Debug.Log($"[BeePollinationSystem] Loài mới: {species}! Social bonus +20");
        }
        else
        {
            sustainability?.AddSocialPoints(5f);   // Điểm thường cho loài đã biết
        }

        // Thông báo cho QuestManager
        questManager?.OnPollinationSuccess(species);

        Debug.Log($"[BeePollinationSystem] Thụ phấn thành công! Loài: {species}. " +
                  $"Tổng loài đã thụ phấn: {_pollinatedSpeciesThisRun.Count}");
    }

    // =========================================================================
    // ResetRun - Gọi khi bắt đầu màn mới.
    // =========================================================================
    public void ResetRun()
    {
        CarriedPollenSpecies = null;
        _pollinatedSpeciesThisRun.Clear();
        IsCollectingNectar = false;
    }

    // =========================================================================
    // PlayVFX - Phát particle tại vị trí.
    // =========================================================================
    private void PlayVFX(ParticleSystem ps, Vector3 position)
    {
        if (ps == null) return;
        ps.transform.position = position;
        ps.Play();
    }

    // =========================================================================
    // Gizmos
    // =========================================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = CarriedPollenSpecies.HasValue ? Color.yellow : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (NearestFlower != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, NearestFlower.transform.position);
        }
    }
}
