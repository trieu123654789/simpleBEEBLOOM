// =============================================================================
// BeeFlower - Component đặt trên từng bông hoa trong thế giới game.
// BeeFlower - Component placed on each flower in the game world.
//
// DATA MỖI HOA / PER-FLOWER DATA
//   species      : loài hoa (dùng để khớp thụ phấn đúng loài)
//   nectarAmount : lượng mật hoa còn lại (0-1)
//   pollenReady  : có phấn sẵn để ong mang đi không
//
// VÒNG ĐỜI / LIFECYCLE
//   Healthy → Depleted (hết mật) → Recovering → Healthy
//   Nếu bị hút cạn quá nhiều lần liên tiếp → Wilting (héo)
//   Nếu héo quá lâu → Dead (chết, không hồi)
//
// SUSTAINABILITY HOOK
//   Mỗi lần ong hút mật đúng cách → environmentScore tăng
//   Mỗi lần hoa chết → environmentScore giảm mạnh
// =============================================================================
using System.Collections;
using UnityEngine;

public class BeeFlower : MonoBehaviour
{
    // =========================================================================
    // ENUMS
    // =========================================================================
    public enum FlowerSpecies
    {
        Lotus     = 0,   // Sen — loài đặc trưng ĐBSCL
        Coconut   = 1,   // Dừa
        Mango     = 2,   // Xoài
        Rice      = 3,   // Lúa
        Wildflower= 4,   // Hoa dại
        Sugarcane = 5,   // Mía
    }

    public enum FlowerState
    {
        Healthy,     // Khoẻ mạnh, đầy mật
        Depleted,    // Hết mật, đang hồi phục
        Recovering,  // Đang lấy lại mật
        Wilting,     // Héo do khai thác quá mức
        Dead         // Chết, không hồi phục
    }

    // =========================================================================
    // SETTINGS / CÀI ĐẶT
    // =========================================================================
    [Header("Identity / Loài hoa")]
    [Tooltip("Loài hoa — ong phải mang phấn đúng loài để thụ phấn thành công.")]
    public FlowerSpecies species = FlowerSpecies.Lotus;

    [Header("Nectar / Mật hoa")]
    [Tooltip("Lượng mật hiện tại (0 = cạn, 1 = đầy).")]
    [Range(0f, 1f)]
    public float nectarAmount = 1f;

    [Tooltip("Lượng mật ong hút được mỗi giây khi đứng trên hoa (giữ trigger).")]
    [Range(0.05f, 0.5f)]
    public float nectarDrainRate = 0.15f;

    [Tooltip("Lượng mật được chuyển vào bình ong mỗi lần hút (0-1).")]
    [Range(0.05f, 0.3f)]
    public float nectarPerSip = 0.1f;

    [Header("Recovery / Hồi phục")]
    [Tooltip("Thời gian hồi phục sau khi bị hút cạn (giây).")]
    [Range(5f, 60f)]
    public float recoveryTime = 20f;

    [Tooltip("Số lần bị hút cạn liên tiếp trước khi héo.")]
    [Range(1, 5)]
    public int wiltsAfterDepletes = 3;

    [Tooltip("Thời gian héo trước khi chết nếu không được thụ phấn (giây).")]
    [Range(10f, 120f)]
    public float wiltDuration = 45f;

    [Header("Pollen / Phấn hoa")]
    [Tooltip("Hoa có phấn sẵn để ong mang đi không. Reset sau mỗi lần thụ phấn.")]
    public bool pollenReady = true;

    [Tooltip("Thời gian để phấn mới sinh ra sau khi bị lấy (giây).")]
    [Range(5f, 30f)]
    public float pollenRegenTime = 10f;

    [Header("Visual / Hình ảnh")]
    [Tooltip("Renderer của hoa để đổi màu theo trạng thái.")]
    public Renderer flowerRenderer;

    [Tooltip("Màu khi hoa khoẻ mạnh.")]
    public Color colorHealthy = new Color(1f, 0.4f, 0.7f, 1f);    // Hồng

    [Tooltip("Màu khi hoa hết mật (đang hồi).")]
    public Color colorDepleted = new Color(0.8f, 0.8f, 0.8f, 1f); // Xám nhạt

    [Tooltip("Màu khi hoa đang héo.")]
    public Color colorWilting = new Color(0.5f, 0.4f, 0.2f, 1f);  // Vàng nâu

    [Tooltip("Màu khi hoa chết.")]
    public Color colorDead = new Color(0.2f, 0.2f, 0.2f, 1f);     // Xám tối

    [Header("Interaction Zone")]
    [Tooltip("Bán kính vùng ong có thể hút mật (meter).")]
    [Range(0.3f, 3f)]
    public float interactionRadius = 1.2f;

    // =========================================================================
    // PUBLIC STATE / TRẠNG THÁI
    // =========================================================================
    /// <summary>Trạng thái hiện tại của hoa.</summary>
    public FlowerState State { get; private set; } = FlowerState.Healthy;

    /// <summary>Số lần bị hút cạn liên tiếp (dùng để tính wilting).</summary>
    public int ConsecutiveDepletes { get; private set; }

    /// <summary>Ong có thể tương tác với hoa này không.</summary>
    public bool IsInteractable => State == FlowerState.Healthy && nectarAmount > 0.01f;

    /// <summary>Hoa còn sống không.</summary>
    public bool IsAlive => State != FlowerState.Dead;

    // =========================================================================
    // EVENTS / SỰ KIỆN
    // =========================================================================
    /// <summary>Phát khi ong hút mật thành công. Param = lượng mật hút được (0-1).</summary>
    public System.Action<float> OnNectarCollected;

    /// <summary>Phát khi hoa nhận được phấn đúng loài (thụ phấn thành công).</summary>
    public System.Action OnPollinated;

    /// <summary>Phát khi hoa héo (quá tải khai thác).</summary>
    public System.Action OnWilted;

    /// <summary>Phát khi hoa chết.</summary>
    public System.Action OnDied;

    /// <summary>Phát khi hoa hồi phục về Healthy.</summary>
    public System.Action OnRecovered;

    // =========================================================================
    // INTERNAL
    // =========================================================================
    private float _recoveryTimer;
    private float _wiltTimer;
    private float _pollenTimer;
    private static readonly int ColorPropId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock _mpb;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        if (flowerRenderer == null) flowerRenderer = GetComponentInChildren<Renderer>();
        UpdateVisual();
    }

    private void Update()
    {
        switch (State)
        {
            case FlowerState.Depleted:
            case FlowerState.Recovering:
                UpdateRecovery();
                break;
            case FlowerState.Wilting:
                UpdateWilting();
                break;
        }

        // Phấn hoa tự tái sinh sau thời gian
        if (!pollenReady && State == FlowerState.Healthy)
        {
            _pollenTimer -= Time.deltaTime;
            if (_pollenTimer <= 0f) pollenReady = true;
        }
    }

    // =========================================================================
    // CollectNectar - Ong hút mật. Trả về lượng mật thực sự lấy được.
    // =========================================================================
    /// <summary>
    /// Ong hút mật từ hoa. Gọi mỗi frame khi ong giữ trigger trên hoa.
    /// Trả về lượng mật hút được trong frame này (0 nếu hoa trống hoặc không thể tương tác).
    /// </summary>
    public float CollectNectar(float deltaTime)
    {
        if (!IsInteractable) return 0f;

        float amount = Mathf.Min(nectarDrainRate * deltaTime, nectarAmount);
        nectarAmount -= amount;
        nectarAmount = Mathf.Max(0f, nectarAmount);

        OnNectarCollected?.Invoke(amount);

        if (nectarAmount <= 0.01f)
        {
            SetState(FlowerState.Depleted);
        }

        UpdateVisual();
        return amount;
    }

    // =========================================================================
    // ReceivePollen - Ong thụ phấn. Trả về true nếu loài khớp.
    // =========================================================================
    /// <summary>
    /// Ong mang phấn đến hoa này. Thành công nếu phấn đúng loài và hoa chưa chết.
    /// </summary>
    public bool ReceivePollen(FlowerSpecies pollenSpecies)
    {
        if (!IsAlive) return false;
        if (pollenSpecies != species) return false;

        // Thụ phấn thành công: tăng tốc hồi phục, reset consecutive depletes
        ConsecutiveDepletes = Mathf.Max(0, ConsecutiveDepletes - 1);

        if (State == FlowerState.Wilting)
        {
            // Thụ phấn cứu hoa đang héo → hồi phục
            SetState(FlowerState.Recovering);
            _recoveryTimer = recoveryTime * 0.4f; // hồi nhanh hơn vì có phấn
        }
        else if (State == FlowerState.Depleted || State == FlowerState.Recovering)
        {
            // Đẩy nhanh hồi phục
            _recoveryTimer *= 0.6f;
        }

        OnPollinated?.Invoke();
        return true;
    }

    // =========================================================================
    // TakePollen - Ong lấy phấn từ hoa này.
    // =========================================================================
    /// <summary>
    /// Ong lấy phấn từ hoa. Trả về true nếu có phấn để lấy.
    /// </summary>
    public bool TakePollen(out FlowerSpecies takenSpecies)
    {
        if (pollenReady && IsAlive)
        {
            pollenReady = false;
            _pollenTimer = pollenRegenTime;
            takenSpecies = species;
            return true;
        }
        takenSpecies = species;
        return false;
    }

    // =========================================================================
    // UpdateRecovery - Hoa hồi phục dần.
    // =========================================================================
    private void UpdateRecovery()
    {
        _recoveryTimer -= Time.deltaTime;

        // Mật hoa tăng dần trong quá trình hồi phục
        float recoveryProgress = 1f - Mathf.Clamp01(_recoveryTimer / recoveryTime);
        nectarAmount = Mathf.Lerp(0f, 1f, recoveryProgress * recoveryProgress);

        if (_recoveryTimer <= 0f)
        {
            nectarAmount = 1f;
            pollenReady = true;
            SetState(FlowerState.Healthy);
            OnRecovered?.Invoke();
        }

        UpdateVisual();
    }

    // =========================================================================
    // UpdateWilting - Hoa héo dần, sẽ chết nếu không được thụ phấn.
    // =========================================================================
    private void UpdateWilting()
    {
        _wiltTimer -= Time.deltaTime;
        if (_wiltTimer <= 0f)
        {
            SetState(FlowerState.Dead);
            OnDied?.Invoke();
        }
        UpdateVisual();
    }

    // =========================================================================
    // SetState - Chuyển trạng thái và cài timers.
    // =========================================================================
    private void SetState(FlowerState newState)
    {
        if (State == newState) return;
        State = newState;

        switch (newState)
        {
            case FlowerState.Depleted:
                ConsecutiveDepletes++;
                _recoveryTimer = recoveryTime;
                if (ConsecutiveDepletes >= wiltsAfterDepletes)
                {
                    SetState(FlowerState.Wilting);
                    return;
                }
                break;

            case FlowerState.Recovering:
                // Timer đã được set bởi caller
                break;

            case FlowerState.Wilting:
                _wiltTimer = wiltDuration;
                OnWilted?.Invoke();
                break;

            case FlowerState.Dead:
                nectarAmount = 0f;
                pollenReady = false;
                break;

            case FlowerState.Healthy:
                ConsecutiveDepletes = 0;
                break;
        }

        UpdateVisual();
    }

    // =========================================================================
    // UpdateVisual - Đổi màu hoa theo trạng thái.
    // =========================================================================
    private void UpdateVisual()
    {
        if (flowerRenderer == null) return;

        Color targetColor = State switch
        {
            FlowerState.Healthy    => Color.Lerp(colorDepleted, colorHealthy, nectarAmount),
            FlowerState.Depleted   => colorDepleted,
            FlowerState.Recovering => Color.Lerp(colorDepleted, colorHealthy, nectarAmount),
            FlowerState.Wilting    => Color.Lerp(colorWilting, colorDepleted, _wiltTimer / wiltDuration),
            FlowerState.Dead       => colorDead,
            _                      => colorHealthy
        };

        flowerRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(ColorPropId, targetColor);
        flowerRenderer.SetPropertyBlock(_mpb);
    }

    // =========================================================================
    // Gizmos
    // =========================================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = State == FlowerState.Dead ? Color.red :
                       State == FlowerState.Wilting ? Color.yellow : Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
