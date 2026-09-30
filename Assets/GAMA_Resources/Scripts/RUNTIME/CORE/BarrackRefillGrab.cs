using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// BarrackRefillGrab — Player walks up to the water pump to add water.
// Hai chế độ song song / Two parallel modes:
//   1) GRAB MODE: nếu prefab có XRBaseInteractable, người chơi bóp grip để nạp.
//   2) PROXIMITY MODE: chỉ cần tay (controller / collider có tag "Hands"
//      hoặc layer Hands/Grab) chạm vào Sphere Collider trigger → tự nạp.
//
// Mode (2) là fallback an toàn khi XR Interactable bị block bởi Layer matrix
// hoặc Interaction Layer mask. Ưu tiên tay phải (controller có XRBaseController)
// để rung haptic.
// =============================================================================
[RequireComponent(typeof(Barrack))]
public class BarrackRefillGrab : MonoBehaviour
{
    [Header("Refill Settings / Cài đặt nạp nước")]
    [Tooltip("Thời gian giữ tay/grab để cộng 1 đợt nước (giây).")]
    [SerializeField] private float refillDuration = 5f;

    [Tooltip("Lượng nước cộng thêm mỗi chu kỳ.")]
    [SerializeField] private int refillAmount = 20;

    [Tooltip("Tự lặp chu kỳ nếu vẫn đang chạm/giữ.")]
    [SerializeField] private bool repeatWhileHeld = true;

    [Header("Feedback / Phản hồi")]
    [SerializeField] private AudioClip refillStartSound;
    [SerializeField] private AudioClip refillCompleteSound;
    [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.4f;

    [Header("XR Grab Mode (optional) / Chế độ Grab XR (tuỳ chọn)")]
    [Tooltip("Interactable lắng nghe sự kiện select. Nếu bỏ trống tự lấy XRBaseInteractable trên cùng GameObject.")]
    [SerializeField] private XRBaseInteractable interactable;

    [Header("Proximity Mode / Chế độ chạm tay")]
    [Tooltip("Bật để cho phép nạp khi tay chỉ cần CHẠM vào Sphere Collider (không cần bóp grip).")]
    [SerializeField] private bool enableProximityRefill = true;

    [Tooltip("Tag của controller / bàn tay để nhận diện. Để trống = chấp nhận mọi collider có XRBaseController/Interactor cha.")]
    [SerializeField] private string handTag = "";

    [Tooltip("LayerMask của tay (Hands=3, Grab=6 mặc định). Bất kỳ collider nào ở layer này sẽ kích hoạt.")]
    [SerializeField] private LayerMask handLayerMask = (1 << 3) | (1 << 6);

    // Runtime state
    private Barrack _barrack;
    private XRBaseController _currentController;
    private Coroutine _refillCoroutine;
    private bool _isActive;          // true when grabbed OR a hand is inside trigger
    private float _progress01;
    private readonly System.Collections.Generic.HashSet<Collider> _handsInside = new System.Collections.Generic.HashSet<Collider>();

    public float Progress01 => _progress01;
    public bool IsRefilling => _isActive;

    private void Awake()
    {
        _barrack = GetComponent<Barrack>();
        if (interactable == null)
            interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnGrabbed);
            interactable.selectExited.AddListener(OnReleased);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnGrabbed);
            interactable.selectExited.RemoveListener(OnReleased);
        }
        _handsInside.Clear();
        StopRefill();
    }

    // =========================================================================
    // GRAB MODE
    // =========================================================================
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRBaseInteractor interactor)
            _currentController = interactor.GetComponentInParent<XRBaseController>();
        StartRefill();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // Nếu tay vẫn còn trong trigger → giữ tiếp ở proximity mode.
        if (!HasHandInside()) StopRefill();
    }

    // =========================================================================
    // PROXIMITY MODE — Sphere Collider trigger trên cùng GameObject.
    // =========================================================================
    private bool IsHand(Collider other)
    {
        if (!enableProximityRefill) return false;
        if (other == null) return false;

        // B\u1eaft bu\u1ed9c ph\u1ea3i l\u00e0 tay XR th\u1eadt s\u1ef1: c\u00f3 XRBaseController ho\u1eb7c XRBaseInteractor.
        // Tr\u00e1nh tr\u01b0\u1eddng h\u1ee3p player capsule / collider kh\u00e1c v\u00f4 t\u00ecnh ch\u1ea1m sphere t\u1eeb xa.
        bool isXrHand = other.GetComponentInParent<XRBaseController>() != null
                        || other.GetComponentInParent<XRBaseInteractor>() != null;
        if (!isXrHand) return false;

        // L\u1ecdc th\u00eam theo tag/layer n\u1ebfu \u0111\u01b0\u1ee3c c\u1ea5u h\u00ecnh.
        if (!string.IsNullOrEmpty(handTag) && !other.CompareTag(handTag)) return false;
        if (handLayerMask.value != 0 && handLayerMask.value != ~0
            && ((1 << other.gameObject.layer) & handLayerMask.value) == 0)
            return false;

        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsHand(other)) return;
        _handsInside.Add(other);
        if (_currentController == null)
            _currentController = other.GetComponentInParent<XRBaseController>();
        StartRefill();
    }

    private void OnTriggerStay(Collider other)
    {
        // Bảo hiểm: nếu OnTriggerEnter bị bỏ lỡ (vd: spawn ngay trong trigger).
        if (!_handsInside.Contains(other) && IsHand(other))
        {
            _handsInside.Add(other);
            if (_currentController == null)
                _currentController = other.GetComponentInParent<XRBaseController>();
            StartRefill();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_handsInside.Remove(other) && !HasHandInside())
            StopRefill();
    }

    private bool HasHandInside() => _handsInside.Count > 0;

    // =========================================================================
    // REFILL CONTROL
    // =========================================================================
    private void StartRefill()
    {
        if (_barrack == null || _barrack.IsDead()) return;
        if (_isActive) return;

        _isActive = true;
        if (refillStartSound != null)
            AudioSource.PlayClipAtPoint(refillStartSound, transform.position);

        if (_refillCoroutine != null) StopCoroutine(_refillCoroutine);
        _refillCoroutine = StartCoroutine(RefillRoutine());
    }

    private void StopRefill()
    {
        _isActive = false;
        _progress01 = 0f;
        _currentController = null;
        if (_refillCoroutine != null)
        {
            StopCoroutine(_refillCoroutine);
            _refillCoroutine = null;
        }
    }

    // Smooth refill: rải đều `refillAmount` HP trong `refillDuration` giây.
    // Mỗi frame cộng phần lẻ; phần nguyên vượt qua mới thực sự gọi Barrack.Refill(1).
    private IEnumerator RefillRoutine()
    {
        do
        {
            if (_barrack.Health >= _barrack.MaxHealth) break;

            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, refillDuration);
            float targetTotal = Mathf.Max(1, refillAmount);
            float accumulated = 0f; // số HP đã được rải (float, có phần lẻ)
            int givenInt = 0;       // số HP nguyên đã thực sự cộng vào Barrack

            while (elapsed < duration)
            {
                if (!_isActive) yield break;
                elapsed += Time.deltaTime;
                _progress01 = Mathf.Clamp01(elapsed / duration);

                // Tính lượng HP nên đã được cộng tại thời điểm này (linear ramp).
                accumulated = _progress01 * targetTotal;
                int shouldGive = Mathf.FloorToInt(accumulated);
                if (shouldGive > givenInt)
                {
                    int delta = shouldGive - givenInt;
                    int added = _barrack.Refill(delta);
                    givenInt += delta;
                    if (added > 0 && refillCompleteSound != null && givenInt == 1)
                        AudioSource.PlayClipAtPoint(refillCompleteSound, transform.position);
                    if (added <= 0) break; // đã đầy hoặc pump chết
                }

                // Haptic: rung mạnh dần theo tiến độ để cảm giác "đang bơm".
                if (_currentController != null)
                {
                    float amp = Mathf.Lerp(hapticAmplitude * 0.5f, hapticAmplitude, _progress01);
                    _currentController.SendHapticImpulse(amp, Time.deltaTime);
                }
                yield return null;
            }

            // Bù phần còn thiếu khi vòng kết thúc (vd: refillAmount = 20, đã cộng 19).
            int remainder = Mathf.Max(0, Mathf.RoundToInt(targetTotal) - givenInt);
            if (remainder > 0) _barrack.Refill(remainder);

            // Rung mạnh 1 nhịp khi hoàn tất chu kỳ.
            if (_currentController != null)
                _currentController.SendHapticImpulse(1f, 0.15f);

            _progress01 = 0f;
        }
        while (repeatWhileHeld && _isActive && _barrack != null && !_barrack.IsDead() && _barrack.Health < _barrack.MaxHealth);

        _refillCoroutine = null;
    }
}
