using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

// =============================================================================
// David_ShrimpGrab - Shrimp fishing system with delay for immersive VR feel.
// David_ShrimpGrab - Hệ thống câu tôm với độ trễ cho trải nghiệm VR sống động.
//
// Flow: Grab → Fishing (5s delay + struggle animation) → Caught → Collect
// Luồng: Bắt → Câu (5s trễ + animation giãy giụa) → Bắt được → Thu hoạch
//
// If player releases during fishing phase, the attempt is cancelled.
// Nếu người chơi thả trong giai đoạn câu, lần câu bị hủy.
// =============================================================================
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class David_ShrimpGrab : MonoBehaviour
{
    // =========================================================================
    // FISHING STATES / CÁC TRẠNG THÁI CÂU
    // =========================================================================
    private enum FishingState
    {
        Idle,       // Swimming freely / Bơi tự do
        Fishing,    // Being fished - delay in progress / Đang câu - đang chờ
        Caught,     // Successfully caught - in hand / Bắt được - trong tay
        Collected   // Put in bag - done / Bỏ vào giỏ - xong
    }

    [Header("Shrimp Config / Cấu hình tôm")]
    [SerializeField] private int pointValue = 20;
    [SerializeField] private Sprite shrimpIcon;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private string bagTag = "Bag";
    
    [Header("Grab Settings / Cài đặt cầm")]
    [SerializeField] private Vector3 grabOffset = new Vector3(0, -0.35f, 0.1f);
    private float grabScale = 0.6f;

    // =========================================================================
    // FISHING SETTINGS / CÀI ĐẶT CÂU TÔM
    // =========================================================================
    [Header("Fishing Settings / Cài đặt câu tôm")]

    [Tooltip("Thời gian câu trước khi bắt được (giây)\n" +
             "Time to fish before catching (seconds)")]
    [SerializeField] private float fishingDuration = 5f;

    [Tooltip("Cường độ giãy giụa (tôm lắc khi đang câu)\n" +
             "Struggle intensity (shrimp shakes while being fished)")]
    [SerializeField] private float struggleIntensity = 0.15f;

    [Tooltip("Tốc độ giãy giụa (lắc/giây)\n" +
             "Struggle speed (shakes per second)")]
    [SerializeField] private float struggleSpeed = 15f;

    [Tooltip("Âm thanh khi bắt đầu câu\n" +
             "Sound when fishing starts")]
    [SerializeField] private AudioClip fishingStartSound;

    [Tooltip("Âm thanh khi câu thành công\n" +
             "Sound when catch succeeds")]
    [SerializeField] private AudioClip catchSuccessSound;

    [Header("Haptic / Rung tay cầm")]

    [Tooltip("Cường độ rung tay cầm khi tôm giãy (0-1)\n" +
             "Controller vibration amplitude during struggle (0-1)")]
    [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.6f;


    // =========================================================================
    // INTERNAL STATE / TRẠNG THÁI NỘI BỘ
    // =========================================================================
    private XRGrabInteractable _grabInteractable;
    private Rigidbody _rb;
    private Transform _currentHandTransform;
    private Vector3 _originalScale;
    private Vector3 _fishingAnchorPos;
    private Quaternion _fishingAnchorRot;
    private FishingState _state = FishingState.Idle;
    private float _fishingTimer = 0f;
    private Coroutine _fishingCoroutine;
    private XRBaseController _currentController;

    // Swimming AI reference (if exists)
    // Tham chiếu AI bơi (nếu có)
    private MonoBehaviour _swimmingAI;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();
        _originalScale = transform.localScale;
        _swimmingAI = GetComponent("Thuan_23127_ShrimpAI") as MonoBehaviour;

        SetupGrabInteractable();
    }

    /// <summary>
    /// Configure XRGrabInteractable for manual control.
    /// Cấu hình XRGrabInteractable để điều khiển thủ công.
    /// </summary>
    private void SetupGrabInteractable()
    {
        if (_grabInteractable == null) return;

        _grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        _grabInteractable.attachEaseInTime = 0f;
        _grabInteractable.useDynamicAttach = false;
        _grabInteractable.retainTransformParent = false;
        _grabInteractable.throwOnDetach = false;
        _grabInteractable.trackPosition = false;
        _grabInteractable.trackRotation = false;
        _grabInteractable.startingSingleGrabTransformers.Clear();
        _grabInteractable.startingMultipleGrabTransformers.Clear();

        _grabInteractable.selectEntered.AddListener(OnGrabbed);
        _grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDestroy()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            _grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    // =========================================================================
    // GRAB EVENTS / SỰ KIỆN BẮT
    // =========================================================================

    /// <summary>
    /// Called when player grabs the shrimp → Start fishing phase.
    /// Được gọi khi người chơi bắt tôm → Bắt đầu giai đoạn câu.
    /// </summary>
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (_state == FishingState.Collected) return;

        if (args.interactorObject is XRBaseInteractor interactor)
        {
            _currentHandTransform = interactor.transform;

            // Get controller for haptic feedback
            // Lấy controller để phản hồi rung
            _currentController = interactor.GetComponentInParent<XRBaseController>();

            // Disable swimming AI while being fished
            // Tắt AI bơi khi đang bị câu
            if (_swimmingAI != null)
                _swimmingAI.enabled = false;

            // Freeze physics
            // Đóng băng vật lý
            if (_rb != null)
            {
                _rb.isKinematic = true;
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            // Save current position as anchor for fishing animation
            // Lưu vị trí hiện tại làm neo cho animation câu
            _fishingAnchorPos = transform.position;
            _fishingAnchorRot = transform.rotation;

            // Start fishing phase
            // Bắt đầu giai đoạn câu
            StartFishing();
        }
    }

    /// <summary>
    /// Called when player releases → Cancel if still fishing, drop if caught.
    /// Được gọi khi người chơi thả → Hủy nếu đang câu, thả nếu đã bắt.
    /// </summary>
    private void OnReleased(SelectExitEventArgs args)
    {
        if (_state == FishingState.Collected) return;

        if (_state == FishingState.Fishing)
        {
            // Released too early → cancel fishing, shrimp escapes
            // Thả quá sớm → hủy câu, tôm thoát
            CancelFishing();
        }
        else if (_state == FishingState.Caught)
        {
            // Release caught shrimp → drop it
            // Thả tôm đã bắt → rơi xuống
            DropShrimp();
        }

        _currentHandTransform = null;
    }

    // =========================================================================
    // FISHING PHASE / GIAI ĐOẠN CÂU
    // =========================================================================

    /// <summary>
    /// Start the fishing delay. Shrimp struggles in place for fishingDuration seconds.
    /// Bắt đầu trễ câu. Tôm giãy giụa tại chỗ trong fishingDuration giây.
    /// </summary>
    private void StartFishing()
    {
        _state = FishingState.Fishing;
        _fishingTimer = 0f;

        // Play fishing start sound
        // Phát âm thanh bắt đầu câu
        if (fishingStartSound != null)
            AudioSource.PlayClipAtPoint(fishingStartSound, transform.position);

        // Start fishing coroutine
        if (_fishingCoroutine != null) StopCoroutine(_fishingCoroutine);
        _fishingCoroutine = StartCoroutine(FishingRoutine());
    }

    /// <summary>
    /// Fishing coroutine: struggle animation + timer → catch on complete.
    /// Coroutine câu: animation giãy giụa + đếm giờ → bắt khi hoàn thành.
    /// </summary>
    private IEnumerator FishingRoutine()
    {
        float elapsed = 0f;

        while (elapsed < fishingDuration)
        {
            elapsed += Time.deltaTime;
            _fishingTimer = elapsed;

            // Progress ratio 0→1
            float progress = Mathf.Clamp01(elapsed / fishingDuration);

            // --- STRUGGLE ANIMATION ---
            // Shrimp shakes side to side at anchor position, intensity decreases as it tires
            // Tôm lắc qua lại tại vị trí neo, cường độ giảm khi mệt
            float currentIntensity = struggleIntensity * (1f - progress * 0.5f);  // Weaken over time
            float shakeX = Mathf.Sin(elapsed * struggleSpeed) * currentIntensity;
            float shakeZ = Mathf.Cos(elapsed * struggleSpeed * 0.7f) * currentIntensity * 0.5f;

            // Gradually pull shrimp upward toward hand (visual feedback of "reeling in")
            // Kéo tôm dần lên phía tay (phản hồi trực quan "kéo lên")
            Vector3 pullTarget = _currentHandTransform != null
                ? _currentHandTransform.position + _currentHandTransform.TransformDirection(grabOffset)
                : _fishingAnchorPos;
            Vector3 currentPos = Vector3.Lerp(_fishingAnchorPos, pullTarget, progress * 0.3f);
            currentPos.x += shakeX;
            currentPos.z += shakeZ;
            transform.position = currentPos;

            // --- HAPTIC FEEDBACK ---
            // Send vibration to controller in sync with struggle
            // Gửi rung đến tay cầm đồng bộ với giãy giụa
            if (_currentController != null)
            {
                float hapticStrength = hapticAmplitude * (1f - progress * 0.5f);  // Weaken with struggle
                _currentController.SendHapticImpulse(hapticStrength, Time.deltaTime);
            }


            yield return null;
        }

        // Fishing complete → catch!
        // Câu xong → bắt được!
        OnFishingComplete();
    }

    /// <summary>
    /// Called when fishing timer completes. Shrimp is now caught.
    /// Được gọi khi đồng hồ câu kết thúc. Tôm đã bị bắt.
    /// </summary>
    private void OnFishingComplete()
    {
        _state = FishingState.Caught;
        _fishingCoroutine = null;

        // Play catch success sound
        // Phát âm thanh câu thành công
        if (catchSuccessSound != null)
            AudioSource.PlayClipAtPoint(catchSuccessSound, transform.position);

        // Snap to hand with final scale
        // Bắt vào tay với kích thước cuối
        transform.localScale = _originalScale * grabScale;

        if (_currentHandTransform != null)
        {
            Vector3 handPos = _currentHandTransform.position + _currentHandTransform.TransformDirection(grabOffset);
            transform.position = handPos;
            transform.rotation = _currentHandTransform.rotation;
        }
    }

    /// <summary>
    /// Cancel fishing (player released early). Shrimp escapes back.
    /// Hủy câu (người chơi thả sớm). Tôm thoát về.
    /// </summary>
    private void CancelFishing()
    {
        if (_fishingCoroutine != null)
        {
            StopCoroutine(_fishingCoroutine);
            _fishingCoroutine = null;
        }

        _state = FishingState.Idle;
        _fishingTimer = 0f;

        // Restore original transform
        // Khôi phục transform gốc
        transform.position = _fishingAnchorPos;
        transform.rotation = _fishingAnchorRot;
        transform.localScale = _originalScale;

        // Re-enable swimming AI
        // Bật lại AI bơi
        if (_swimmingAI != null)
            _swimmingAI.enabled = true;

        // Re-enable physics
        // Bật lại vật lý
        if (_rb != null)
        {
            _rb.isKinematic = false;
        }
    }

    /// <summary>
    /// Drop caught shrimp (released after caught).
    /// Thả tôm đã bắt (thả sau khi đã bắt).
    /// </summary>
    private void DropShrimp()
    {
        _state = FishingState.Idle;

        transform.localScale = _originalScale;

        // Re-enable swimming and physics
        // Bật lại bơi và vật lý
        if (_swimmingAI != null)
            _swimmingAI.enabled = true;

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
        }
    }

    // =========================================================================
    // LATE UPDATE - Keep shrimp attached to hand when caught
    // Giữ tôm gắn vào tay khi đã bắt được
    // =========================================================================

    private void LateUpdate()
    {
        if (_state == FishingState.Caught && _currentHandTransform != null)
        {
            transform.localScale = _originalScale * grabScale;

            Vector3 offsetPosition = _currentHandTransform.position + _currentHandTransform.TransformDirection(grabOffset);
            transform.position = offsetPosition;
            transform.rotation = _currentHandTransform.rotation;
        }
    }

    // =========================================================================
    // COLLECTION LOGIC - Detect bag collision
    // LOGIC THU HOẠCH - Phát hiện va chạm với giỏ
    // =========================================================================

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(bagTag)) return;
        if (_state == FishingState.Collected) return;
        if (!GameRulesProvider.GameActive) return;

        // Can only collect if NOT currently grabbed/being fished
        // Chỉ thu hoạch được nếu KHÔNG đang bị cầm/đang câu
        if (_state == FishingState.Fishing || _state == FishingState.Caught) return;

        CollectShrimp();
    }

    /// <summary>
    /// Collect shrimp: add score, track, destroy.
    /// Thu hoạch tôm: cộng điểm, theo dõi, hủy.
    /// </summary>
    private void CollectShrimp()
    {
        if (_state == FishingState.Collected) return;
        _state = FishingState.Collected;

        // Track in SeasonalSummary
        var summary = Thuan_23127_SeasonalSummary.Instance;
        if (summary != null && shrimpIcon != null)
        {
            summary.TrackDirect("Shrimp", shrimpIcon, pointValue);
        }

        // Add score to GameManager
        var gm = Thuan_23127_GameManager.Instance;
        if (gm != null)
        {
            gm.AddScore(pointValue);
        }

        // Play sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        // Destroy shrimp
        Destroy(gameObject);
    }

    // =========================================================================
    // PUBLIC PROPERTIES / THUỘC TÍNH CÔNG KHAI
    // =========================================================================

    /// <summary>
    /// Current fishing progress (0-1). Useful for UI.
    /// Tiến trình câu hiện tại (0-1). Hữu ích cho UI.
    /// </summary>
    public float FishingProgress => (fishingDuration > 0f) ? Mathf.Clamp01(_fishingTimer / fishingDuration) : 0f;

    /// <summary>
    /// Is the shrimp currently being fished?
    /// Tôm có đang bị câu không?
    /// </summary>
    public bool IsFishing => _state == FishingState.Fishing;

    /// <summary>
    /// Is the shrimp caught (in hand)?
    /// Tôm đã bị bắt (trong tay) chưa?
    /// </summary>
    public bool IsCaught => _state == FishingState.Caught;
}

