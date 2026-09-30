using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using System.Collections.Generic;

// =============================================================================
// FruitType - Types of collectible products in the game.
// FruitType - Các loại sản phẩm có thể thu hoạch trong game.
// =============================================================================
public enum FruitType 
{ 
    Coconut,  // Coconut / Dừa
    Durian,   // Durian / Sầu riêng
    Fish,     // Fish / Cá
    Shrimp,   // Shrimp / Tôm
    Rice,     // Rice / Lúa
    Egg       // Egg / Trứng
}

// =============================================================================
// David_Fruit - Handles collectible fruit/product behavior and scoring.
// David_Fruit - Xử lý hành vi thu hoạch trái cây/sản phẩm và tính điểm.
// 
// Attach this to any collectible prefab (fruits, fish, rice, etc.).
// When player's bag touches this object, it calculates score and disappears.
// 
// Gắn script này vào bất kỳ prefab có thể thu hoạch (trái cây, cá, lúa, v.v.).
// Khi túi của người chơi chạm vào object này, nó tính điểm và biến mất.
// 
// Score is calculated based on: Zone (Fresh/Salt) × Season (Rainy/Dry)
// Điểm được tính dựa trên: Vùng (Ngọt/Lợ) × Mùa (Mưa/Khô)
// 
// NEW: Auto-pull grab - When grabbed, automatically flies to hand in 0.2s
// MỚI: Auto-pull grab - Khi grab, tự động bay vào tay trong 0.2s
// =============================================================================
public class David_Fruit : MonoBehaviour
{
    [Header("Fruit Type / Loại sản phẩm")]
    // -------------------------------------------------------------------------
    // Select the type of product this object represents.
    // Chọn loại sản phẩm mà object này đại diện.
    // -------------------------------------------------------------------------
    public FruitType fruitType = FruitType.Coconut;
    
    [Tooltip("Icon for this product (for score UI display)")]
    public Sprite productIcon;
    
    [Header("Zone Source / Nguồn xác định Vùng")]
    [Tooltip("Drag the FarmArea here to determine Fresh/Salt zone. Auto-finds if empty.")]
    // -------------------------------------------------------------------------
    // Reference to parent FarmArea - determines if this is Fresh or Salt water zone.
    // Tham chiếu đến FarmArea cha - xác định đây là vùng nước Ngọt hay Lợ.
    // -------------------------------------------------------------------------
    public FarmArea ownerArea;
    
    [Header("Harvest Bag Tag / Tag túi thu hoạch")]
    // -------------------------------------------------------------------------
    // Tag of the player's collection bag. Default is "Bag".
    // Tag của túi thu hoạch của người chơi. Mặc định là "Bag".
    // -------------------------------------------------------------------------
    public string bagTag = "Bag";

    [Header("Other Config / Cấu hình khác")]
    [Tooltip("true: Destroy on collect. false: Deactivate only (for respawn).")]
    // -------------------------------------------------------------------------
    // If true, object is destroyed after collection.
    // If false, object is just deactivated (can be respawned by TreeSpawner).
    // Nếu true, object bị hủy sau khi thu hoạch.
    // Nếu false, object chỉ bị ẩn (có thể respawn bởi TreeSpawner).
    // -------------------------------------------------------------------------
    public bool destroyOnCollect = true;

    [Header("Audio (Optional)")]
    // -------------------------------------------------------------------------
    // Sound to play when collected. Leave empty for no sound.
    // Âm thanh phát khi thu hoạch. Để trống nếu không cần âm thanh.
    // -------------------------------------------------------------------------
    public AudioClip collectSound;

    [Header("Tree Harvesting / Thu hoạch trên cây")]
    // -------------------------------------------------------------------------
    // For fruits on trees (Coconut, Durian).
    // Cho trái cây trên cây (Dừa, Sầu riêng).
    // -------------------------------------------------------------------------
    [Tooltip("True nếu quả đang trên cây (chưa rụng)")]
    public bool isOnTree = false;
    
    [Tooltip("True nếu quả có thể thu hoạch (đã rụng xuống đất)")]
    public bool canCollect = true;
    
    [Tooltip("Âm thanh khi rơi từ cây")]
    public AudioClip dropSound;
    
    [Header("Grab Settings / Cài đặt cầm")]
    [Tooltip("Offset vị trí khi cầm - Dừa cần xa hơn vì to, Sầu riêng gần hơn")]
    public Vector3 grabOffset = new Vector3(0f, -0.3f, 0.3f); // Default cho dừa
    
    [Tooltip("Scale khi đang cầm (1 = giữ nguyên) - Sầu riêng nên để 1.0")]
    [Range(0.1f, 1f)]
    public float grabScale = 0.5f; // Default cho dừa
    
    [Tooltip("Nếu true, sẽ tự động điều chỉnh offset/scale theo loại trái cây")]
    public bool autoAdjustByFruitType = true;
    
    // Store original scale for restore
    private Vector3 _originalScale;
    
    // Store original position for reset
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Transform _originalParent;
    
    // Prevents double-collection in same frame.
    // Ngăn thu hoạch hai lần trong cùng một frame.
    // -------------------------------------------------------------------------
    private bool _collected = false;

    // =========================================================================
    // HARVEST LIMITS — Giới hạn số lượng thu hoạch mỗi game (static, toàn cục).
    // Durian: 15, Rice: 25, Shrimp: 5. Others: unlimited.
    // =========================================================================
    private static readonly Dictionary<FruitType, int> HarvestLimits = new Dictionary<FruitType, int>
    {
        { FruitType.Durian, 15 },
        { FruitType.Rice,   25 },
        { FruitType.Shrimp,  5 },
    };

    // Tracks how many of each type have been collected this game.
    // Theo dõi đã thu hoạch bao nhiêu mỗi loại trong game này.
    private static Dictionary<FruitType, int> _harvestCounts = new Dictionary<FruitType, int>();

    /// <summary>
    /// Returns how many of this type have been harvested so far.
    /// Trả về đã thu hoạch bao nhiêu loại này.
    /// </summary>
    public static int GetHarvestCount(FruitType type)
    {
        return _harvestCounts.TryGetValue(type, out int count) ? count : 0;
    }

    /// <summary>
    /// Returns the max harvest allowed (-1 = unlimited).
    /// Trả về giới hạn thu hoạch (-1 = không giới hạn).
    /// </summary>
    public static int GetHarvestLimit(FruitType type)
    {
        return HarvestLimits.TryGetValue(type, out int limit) ? limit : -1;
    }

    /// <summary>
    /// Checks if this fruit type has reached its harvest limit.
    /// Kiểm tra loại trái này đã đạt giới hạn thu hoạch chưa.
    /// </summary>
    public static bool IsHarvestLimitReached(FruitType type)
    {
        if (!HarvestLimits.TryGetValue(type, out int limit)) return false; // no limit
        int current = GetHarvestCount(type);
        return current >= limit;
    }

    /// <summary>
    /// Resets all harvest counts (call when game restarts or new round).
    /// Reset tất cả bộ đếm thu hoạch (gọi khi game restart hoặc vòng mới).
    /// </summary>
    public static void ResetAllHarvestCounts()
    {
        _harvestCounts.Clear();
        Debug.Log("[David_Fruit] Đã reset tất cả bộ đếm thu hoạch.");
    }
    
    // =========================================================================
    // Awake - Setup instant grab IMMEDIATELY for runtime spawned objects (eggs)
    // Awake - Setup instant grab NGAY LẬP TỨC cho object spawn runtime (trứng)
    // =========================================================================
    private void Awake()
    {
        // For runtime spawned objects (like eggs), we need to setup ASAP
        // Đối với object spawn runtime (như trứng), cần setup càng sớm càng tốt
        SetupInstantGrab();
    }
    
    // =========================================================================
    // Start - Auto-find parent FarmArea if not assigned in Inspector.
    // Start - Tự động tìm FarmArea cha nếu chưa gán trong Inspector.
    // =========================================================================
    private void Start()
    {
        // Auto-find parent FarmArea if not assigned.
        // Tự động tìm FarmArea cha nếu chưa gán.
        if (ownerArea == null)
        {
            ownerArea = GetComponentInParent<FarmArea>();
        }
        
        // Save original transform for ResetToTree
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
        _originalParent = transform.parent;
        
        // Setup XR Grab Interactable for instant grab
        SetupInstantGrab();
        
        // Auto-setup tree fruits
        if (fruitType == FruitType.Coconut || fruitType == FruitType.Durian)
        {
            // If parent has tree script, assume on tree
            if (GetComponentInParent<David_CoconutTree>() != null || 
                GetComponentInParent<David_DurianTree>() != null)
            {
                isOnTree = true;
                canCollect = false;
                
                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
        }
    }
    
    // =========================================================================
    // SetupInstantGrab - Configure XR Grab for instant snap to hand.
    // SetupInstantGrab - Cấu hình XR Grab để snap ngay lập tức vào tay.
    // =========================================================================
    private bool _isInstantGrabSetup = false;
    
    private void OnEnable()
    {
        // Reset collection state when re-enabled (respawn)
        _collected = false;
        
        // CRITICAL: Force re-setup EVERY TIME when enabled
        // This ensures events are subscribed for runtime-spawned objects like eggs
        _isInstantGrabSetup = false; // Reset flag to allow re-setup
        SetupInstantGrab();
    }
    
    /// <summary>
    /// Call this after dropping from tree to setup instant grab.
    /// Gọi method này sau khi rơi khỏi cây để setup instant grab.
    /// </summary>
    public void SetupAfterDrop()
    {
        Debug.Log($"[David_Fruit] {gameObject.name}: SetupAfterDrop called");
        
        var grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            // Force re-registration with XRInteractionManager
            // This is needed because detaching from parent can invalidate the registration
            // Buộc đăng ký lại với XRInteractionManager
            // Cần thiết vì việc detach khỏi parent có thể làm mất đăng ký
            
            // Temporarily disable and re-enable to force re-registration
            grabInteractable.enabled = false;
            grabInteractable.enabled = true;
            
            // =========================================================================
            // FIX: Ensure collider is NOT a trigger! XR Ray doesn't detect triggers by default.
            // KHẮC PHỤC: Đảm bảo collider KHÔNG phải trigger! XR Ray không detect trigger mặc định.
            // =========================================================================
            Collider col = GetComponent<Collider>();
            if (col != null && col.isTrigger)
            {
                col.isTrigger = false;
                Debug.LogWarning($"[David_Fruit] {gameObject.name}: Collider was TRIGGER, changed to non-trigger for grab!");
            }
            
            Debug.Log($"[David_Fruit] {gameObject.name}: Force re-registered XRGrabInteractable. Collider isTrigger={col?.isTrigger}");
        }
        
        // Reset flag so setup runs again
        _isInstantGrabSetup = false;
        SetupInstantGrab();
    }
    
    private void SetupInstantGrab()
    {
        var grabInteractable = GetComponent<XRGrabInteractable>();
        
        if (grabInteractable == null)
        {
            Debug.LogWarning($"[David_Fruit] {gameObject.name}: No XRGrabInteractable found!");
            return;
        }
        
        if (!grabInteractable.enabled)
        {
            Debug.LogWarning($"[David_Fruit] {gameObject.name}: XRGrabInteractable is DISABLED!");
            return;
        }
        
        // REMOVE existing listeners first to prevent duplicates!
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
        
        // Only setup once to avoid duplicate event subscriptions
        if (_isInstantGrabSetup)
        {
            return;
        }
        _isInstantGrabSetup = true;
        
        // =========================================================================
        // CRITICAL: Ensure XRInteractionManager is assigned!
        // Without this, XR system won't register this interactable for selection.
        // QUAN TRỌNG: Đảm bảo XRInteractionManager được gán!
        // Không có nó, XR system sẽ không đăng ký interactable này để select.
        // =========================================================================
        if (grabInteractable.interactionManager == null)
        {
            var manager = FindObjectOfType<XRInteractionManager>();
            if (manager != null)
            {
                grabInteractable.interactionManager = manager;
                Debug.Log($"[David_Fruit] {gameObject.name}: Auto-assigned XRInteractionManager");
            }
            else
            {
                Debug.LogError($"[David_Fruit] {gameObject.name}: No XRInteractionManager found in scene!");
            }
        }
        
        // =========================================================================
        // CRITICAL: Ensure colliders are registered with XRGrabInteractable!
        // XR Interaction Toolkit requires colliders to detect ray/direct interaction.
        // QUAN TRỌNG: Đảm bảo colliders được đăng ký với XRGrabInteractable!
        // XR Interaction Toolkit cần colliders để detect tương tác ray/trực tiếp.
        // =========================================================================
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            // FIX: XR Ray doesn't detect trigger colliders by default!
            // KHẮC PHỤC: XR Ray không detect trigger colliders mặc định!
            if (col.isTrigger)
            {
                col.isTrigger = false;
                Debug.LogWarning($"[David_Fruit] {gameObject.name}: Changed collider {col.name} from TRIGGER to non-trigger!");
            }
            
            if (!grabInteractable.colliders.Contains(col))
            {
                grabInteractable.colliders.Add(col);
            }
        }
        Debug.Log($"[David_Fruit] {gameObject.name}: Registered {colliders.Length} colliders. All non-trigger.");
        
        // INSTANT SNAP TO HAND - Not to ray hit point!
        grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grabInteractable.attachEaseInTime = 0f;
        
        // IMPORTANT: Disable dynamic attach so object snaps to HAND, not ray hit point
        grabInteractable.useDynamicAttach = false;
        
        // CRITICAL: Don't retain parent! This prevents re-parenting to tree on grab!
        grabInteractable.retainTransformParent = false;
        
        // FIX: Disable throwOnDetach for kinematic rigidbody
        grabInteractable.throwOnDetach = false;
        
        // FIX: KEEP tracking ENABLED here! We disable it in OnGrabbed() after selection.
        // Disabling before grab prevents XR system from completing the selection.
        // KHẮC PHỤC: GIỮ tracking BẬT ở đây! Chỉ tắt trong OnGrabbed() sau khi đã select.
        // Tắt trước khi grab ngăn XR system hoàn thành selection.
        grabInteractable.trackPosition = true;
        grabInteractable.trackRotation = true;
        
        // Subscribe to grab/release events
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
        
        Debug.Log($"[David_Fruit] {gameObject.name}: XRGrabInteractable setup complete. Colliders={grabInteractable.colliders.Count}, Manager={grabInteractable.interactionManager?.name ?? "NULL"}");
    }
    
    private void OnDestroy()
    {
        var grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
    
    // =========================================================================
    // OnGrabbed - Called when grabbed, teleport to hand immediately!
    // OnGrabbed - Gọi khi grab, dịch chuyển về tay ngay lập tức!
    // =========================================================================
    
    // Track grab state for LateUpdate
    private Transform _currentGrabTarget;
    private XRGrabInteractable _currentGrabInteractable;

    // =========================================================================
    // DURIAN HAPTIC HARVEST — controller vibration for 1.5s before collect.
    // THU HOẠCH SẦU RIÊNG RUNG TAY — rung tay cầm 1.5s trước khi thu hoạch.
    // =========================================================================
    [Header("Durian Harvest Haptic / Rung tay thu hoạch sầu riêng")]
    [Tooltip("Thời gian rung tay khi nhặt sầu riêng (giây)")]
    [SerializeField] private float durianHarvestDuration = 1.5f;
    [Tooltip("Cường độ rung tay (0-1)")]
    [SerializeField, Range(0f, 1f)] private float durianHapticAmplitude = 0.5f;
    [Tooltip("Tốc độ lắc quả sầu riêng khi đang nhặt")]
    [SerializeField] private float durianStruggleSpeed = 12f;
    [Tooltip("Cường độ lắc quả sầu riêng")]
    [SerializeField] private float durianStruggleIntensity = 0.02f;

    private Coroutine _durianHarvestCoroutine;
    private XRBaseController _durianController;
    private bool _durianHarvestComplete = false;
    
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log($"[David_Fruit] OnGrabbed: {gameObject.name}, fruitType={fruitType}, wasOnTree={isOnTree}");
        
        isOnTree = false;

        // All fruits (including Durian) can be collected immediately on grab.
        // Tất cả trái (kể cả Sầu riêng) có thể thu hoạch ngay khi grab.
        canCollect = true;
        
        // CRITICAL: DETACH FROM ANY PARENT FIRST!
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        if (args.interactorObject is XRBaseInteractor interactor)
        {
            _currentGrabTarget = interactor.transform;
            _currentGrabInteractable = GetComponent<XRGrabInteractable>();
            
            // Make rigidbody kinematic so XR doesn't fight us
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            transform.SetParent(null);
            
            Vector3 actualOffset = grabOffset;
            float actualScale = grabScale;
            
            if (autoAdjustByFruitType)
            {
                switch (fruitType)
                {
                    case FruitType.Durian:
                        actualOffset = new Vector3(0f, -0.1f, 0.15f);
                        actualScale = 1f;
                        break;
                    case FruitType.Coconut:
                        actualOffset = grabOffset;
                        actualScale = grabScale;
                        break;
                    case FruitType.Egg:
                        actualOffset = Vector3.zero;
                        actualScale = 1f;
                        break;
                    default:
                        actualOffset = Vector3.zero;
                        actualScale = 1f;
                        break;
                }
            }
            
            // Save original scale
            _originalScale = transform.localScale;
            transform.localScale = _originalScale * actualScale;
            
            // TELEPORT TO HAND IMMEDIATELY!
            Vector3 offsetPosition = _currentGrabTarget.position + _currentGrabTarget.TransformDirection(actualOffset);
            transform.position = offsetPosition;
            transform.rotation = _currentGrabTarget.rotation;

            // NOW disable tracking so WE control position via LateUpdate, not XR system!
            // GIỜ tắt tracking để CHÚNG TA điều khiển vị trí qua LateUpdate, không phải XR system!
            if (_currentGrabInteractable != null)
            {
                _currentGrabInteractable.trackPosition = false;
                _currentGrabInteractable.trackRotation = false;
            }

            // Durian haptic harvest DISABLED — no shake/delay.
            // Rung tay sầu riêng ĐÃ TẮT — không rung/delay.
        }
    }
    
    // =========================================================================
    // LateUpdate - Force position to hand AFTER XR Update runs!
    // LateUpdate - Giữ vị trí tại tay SAU KHI XR Update chạy!
    // =========================================================================
    private void LateUpdate()
    {
        // Fail-safe: If interactable says NOT selected, but we still have a target -> Clear it to preventing sticking!
        if (_currentGrabTarget != null && _currentGrabInteractable != null && !_currentGrabInteractable.isSelected)
        {
            OnReleased(new SelectExitEventArgs()); // Simulate release
            return;
        }

        // If grabbed, force position to hand
        if (_currentGrabTarget != null)
        {
            // FORCE: Keep detached from any parent!
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            
            // DETERMINE ACTUAL OFFSET based on fruit type (same logic as OnGrabbed)
            Vector3 actualOffset = grabOffset;
            if (autoAdjustByFruitType)
            {
                actualOffset = fruitType switch
                {
                    FruitType.Durian => new Vector3(0f, -0.1f, 0.15f),
                    FruitType.Coconut => grabOffset,
                    FruitType.Egg => Vector3.zero,
                    _ => Vector3.zero
                };
            }
            
            // FORCE: Position at controller WITH OFFSET
            Vector3 offsetPosition = _currentGrabTarget.position + _currentGrabTarget.TransformDirection(actualOffset);
            transform.position = offsetPosition;
            transform.rotation = _currentGrabTarget.rotation;
        }
    }
    
    // =========================================================================
    // HarvestDurianRoutine — DISABLED (shake + delay removed).
    // Thu hoạch sầu riêng — ĐÃ TẮT (bỏ rung + delay).
    // =========================================================================
    // Durian now collects instantly like other fruits.
    // Sầu riêng giờ thu hoạch ngay lập tức như các trái khác.

    // =========================================================================
    // OnReleased - Called when released, restore XR tracking.
    // OnReleased - Gọi khi thả, khôi phục XR tracking.
    // =========================================================================
    private void OnReleased(SelectExitEventArgs args)
    {
        // Durian haptic cancel DISABLED — no shake/delay to cancel.
        // Hủy rung tay sầu riêng ĐÃ TẮT — không có rung/delay để hủy.

        // Restore XR tracking
        if (_currentGrabInteractable != null)
        {
            _currentGrabInteractable.trackPosition = true;
            _currentGrabInteractable.trackRotation = true;
        }
        
        // RESTORE ORIGINAL SCALE
        if (_originalScale != Vector3.zero)
        {
            transform.localScale = _originalScale;
        }
        
        // RESTORE RIGIDBODY - Enable physics again
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        
        _currentGrabTarget = null;
    }
    
    // =========================================================================
    // OnTriggerEnter - Detects when player's bag touches this fruit (trigger mode).
    // OnTriggerEnter - Phát hiện khi túi người chơi chạm trái cây này (chế độ trigger).
    // =========================================================================
    private void OnTriggerEnter(Collider other)
    {
        // Already collected - ignore.
        // Đã thu hoạch rồi - bỏ qua.
        if (_collected) return;
        
        // Only react to objects with bag tag.
        // Chỉ phản ứng với object có tag túi.
        if (!other.CompareTag(bagTag)) return;
        
        TryCollect();
    }
    
    // =========================================================================
    // OnCollisionEnter - Detects when player's bag touches this fruit (collision mode).
    // OnCollisionEnter - Phát hiện khi túi chạm trái cây này (chế độ collision).
    // =========================================================================
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(bagTag)) return;
        TryCollect();
    }
    
    // =========================================================================
    // TryCollect - Validates collection conditions before actually collecting.
    // TryCollect - Kiểm tra điều kiện thu hoạch trước khi thực sự thu hoạch.
    // 
    // Checks: Is game active? Is this fruit harvestable in current season?
    // Kiểm tra: Game có đang chạy không? Trái này có thể thu hoạch trong mùa hiện tại không?
    // 
    // FIXED: Now allows collection while holding fruit (grab → put in bag).
    // KHẮC PHỤC: Giờ cho phép thu hoạch khi đang cầm (grab → bỏ vào túi).
    // =========================================================================
    private void TryCollect()
    {
        // REMOVED: Previously blocked collection while grabbed.
        // Now allows "grab fruit → put in bag" workflow as intended.
        // ĐÃ XÓA: Trước đây chặn thu hoạch khi đang cầm.
        // Giờ cho phép workflow "cầm trái → bỏ vào túi" như mong muốn.
        
        // Check if fruit can be collected (for tree fruits).
        // Kiểm tra xem quả có thể thu hoạch không (cho quả trên cây).
        if (!canCollect)
        {
            return;
        }
        
        // Game must be active to collect.
        // Game phải đang hoạt động mới thu hoạch được.
        if (!GameRulesProvider.GameActive)
        {
            return;
        }
        
        // SPECIAL RULE: Durian can only be harvested in rainy season.
        // Durian wilts and dies in dry season (high salinity).
        // QUY TẮC ĐẶC BIỆT: Sầu riêng chỉ hái được vào mùa mưa.
        // Sầu riêng héo và chết trong mùa khô (độ mặn cao).
        if (fruitType == FruitType.Durian)
        {
            if (GameRulesProvider.Saltwater_Intrusion >= 1f)
            {
                return;
            }
        }
        
        // HARVEST LIMIT CHECK — block if this type is at max.
        // KIỂM TRA GIỚI HẠN THU HOẠCH — chặn nếu loại này đã đạt tối đa.
        if (IsHarvestLimitReached(fruitType))
        {
            int limit = GetHarvestLimit(fruitType);
            Debug.Log($"[David_Fruit] {fruitType} đã đạt giới hạn thu hoạch ({limit}). Không thể thu thêm!");
            return;
        }
        
        // Force-release grab BEFORE collecting to prevent XR from holding destroyed object.
        // Buộc thả grab TRƯỚC KHI thu hoạch để ngăn XR cầm object đã bị hủy.
        ForceReleaseGrab();
        
        CollectFruit();
    }
    
    /// <summary>
    /// Forces the XR system to release this object if it's being grabbed.
    /// Buộc hệ thống XR thả object này nếu đang được cầm.
    /// </summary>
    private void ForceReleaseGrab()
    {
        if (_currentGrabInteractable != null && _currentGrabInteractable.isSelected)
        {
            // Get the XR Interaction Manager from the interactable itself
            // Lấy XR Interaction Manager từ chính interactable
            var interactionManager = _currentGrabInteractable.interactionManager;
            if (interactionManager != null)
            {
                // Cancel selection (force drop)
                interactionManager.CancelInteractableSelection((IXRSelectInteractable)_currentGrabInteractable);
            }
        }
        
        // Clear tracking state
        _currentGrabTarget = null;
    }
    
    // =========================================================================
    // GetTableScore - Returns score based on Zone × Season lookup table.
    // GetTableScore - Trả về điểm dựa trên bảng tra cứu Vùng × Mùa.
    // 
    // SCORE TABLE (updated with production-based values):
    // Negative scores = crop failure (plant dies, loss of investment).
    // Điểm âm = thất bại mùa vụ (cây chết, mất vốn đầu tư).
    // =========================================================================
    private int GetTableScore()
    {
        // Determine zone type (default to Fresh if no FarmArea).
        // Xác định loại vùng (mặc định là Ngọt nếu không có FarmArea).
        bool isFresh = true;
        if (ownerArea != null)
        {
            isFresh = (ownerArea.waterType == WaterType.Fresh);
        }
        
        // Determine season (Rainy = low salinity, Dry = high salinity).
        // Xác định mùa (Mưa = độ mặn thấp, Khô = độ mặn cao).
        bool isRainy = (GameRulesProvider.Saltwater_Intrusion < 1f);
        
        switch (fruitType)
        {
            case FruitType.Durian:
                // Durian: Score depends on salinity phase (3 phases).
                // Sầu riêng: Điểm phụ thuộc giai đoạn độ mặn (3 giai đoạn).
                // Phase 1 (T11-T1, Intrusion=0.0): 150 pts — peak harvest season
                // Phase 2 (T2-T3,  Intrusion=0.5):  75 pts — transitional
                // Phase 3 (T4,     Intrusion=1.0):   0 pts  — blocked by TryCollect
                {
                    float intrusion = GameRulesProvider.Saltwater_Intrusion;
                    if (intrusion < 0.1f) return 150;   // Phase 1 — mùa mưa đỉnh điểm
                    if (intrusion < 1f)   return 75;    // Phase 2 — chuyển tiếp
                    return 0;                           // Phase 3 — không thể thu hoạch
                }
                
            case FruitType.Coconut:
                // Coconut: Tolerates salt better than durian.
                // Dừa: Chịu mặn tốt hơn sầu riêng.
                // Production: 20 tons/ha, each tree = 5 ha.
                // Sản lượng: 20 tấn/ha, mỗi cây = 5 ha.
                if (isFresh) return isRainy ? 100 : 80;
                else         return isRainy ? 60 : 50;
                
            case FruitType.Fish:
                // Fish: Prefers brackish/salt water, especially in dry season.
                // Cá: Thích nước lợ/mặn, đặc biệt trong mùa khô.
                if (isFresh) return isRainy ? 10 : 20;
                else         return isRainy ? 30 : 40;
            
            case FruitType.Shrimp:
                // Shrimp: Consistent yield regardless of conditions.
                // Tôm: Năng suất ổn định bất kể điều kiện.
                // Production: 2 tons/ha, each unit = 10 ha.
                // Sản lượng: 2 tấn/ha, mỗi con = 10 ha.
                if (isFresh) return isRainy ? 20 : 20;
                else         return isRainy ? 20 : 20;

            case FruitType.Rice:
                // Rice: Score depends on salinity phase (3 phases).
                // Lúa: Điểm phụ thuộc giai đoạn độ mặn (3 giai đoạn).
                // Phase 1 (T11-T1, Intrusion=0.0): 60 pts — mùa mưa, nước ngọt dồi dào
                // Phase 2 (T2-T3,  Intrusion=0.5): 30 pts — bắt đầu xâm nhập mặn
                // Phase 3 (T4,     Intrusion=1.0):  0 pts — mặn quá cao, mất trắng
                {
                    float intrusion = GameRulesProvider.Saltwater_Intrusion;
                    if (intrusion < 0.1f) return 60;  // Phase 1 — lúa tốt
                    if (intrusion < 1f)   return 30;  // Phase 2 — giảm năng suất
                    return 0;                         // Phase 3 — thất thu
                }

            case FruitType.Egg:
                // Egg: Fixed score, no zone/season modifiers.
                // Trứng: Điểm cố định, không bị ảnh hưởng vùng/mùa.
                return 3;

            default:
                return 1;
        }
    }
    
    // =========================================================================
    // CollectFruit - Executes the collection: score, sound, and removal.
    // CollectFruit - Thực hiện thu hoạch: tính điểm, âm thanh, và xóa object.
    // =========================================================================
    private void CollectFruit()
    {
        // Mark as collected to prevent double-collection.
        // Đánh dấu đã thu hoạch để tránh thu hoạch hai lần.
        _collected = true;
        
        // INCREMENT HARVEST COUNTER for this fruit type.
        // TĂNG BỘ ĐẾM THU HOẠCH cho loại trái này.
        if (!_harvestCounts.ContainsKey(fruitType))
            _harvestCounts[fruitType] = 0;
        _harvestCounts[fruitType]++;
        
        int harvestLimit = GetHarvestLimit(fruitType);
        string limitStr = harvestLimit > 0 ? $"/{harvestLimit}" : "";
        Debug.Log($"[David_Fruit] Thu hoạch {fruitType}: {_harvestCounts[fruitType]}{limitStr}");
        
        // Calculate score from lookup table.
        // Tính điểm từ bảng tra cứu.
        int points = GetTableScore();
        
        // Track in SeasonalSummary
        var summary = Thuan_23127_SeasonalSummary.Instance;
        if (summary != null && productIcon != null)
        {
            summary.TrackDirect(fruitType.ToString(), productIcon, points);
        }
        
        // Add score to GameManager.
        // Cộng điểm vào GameManager.
        var gm = Thuan_23127_GameManager.Instance;
        if (gm != null)
        {
            gm.AddScore(points);
        }
        
        // Play collection sound effect.
        // Phát âm thanh thu hoạch.
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        // Get display name for debug log.
        // Lấy tên hiển thị cho debug log.
        string fruitName = fruitType switch
        {
            FruitType.Coconut => "Dừa",
            FruitType.Durian => "Sầu riêng",
            FruitType.Fish => "Cá",
            FruitType.Shrimp => "Tôm",
            FruitType.Rice => "Lúa",
            FruitType.Egg => "Trứng",
            _ => "Unknown"
        };
        bool isFresh = ownerArea != null && ownerArea.waterType == WaterType.Fresh;
        bool isRainy = GameRulesProvider.Saltwater_Intrusion < 1f;
        
        // Remove object from scene.
        // Xóa object khỏi scene.
        if (destroyOnCollect)
        {
            // Permanent removal.
            // Xóa vĩnh viễn.
            Destroy(gameObject);
        }
        else
        {
            // Temporary hide (for respawn by TreeSpawner).
            gameObject.SetActive(false);
        }
    }

    // =========================================================================
    // ResetToTree - Resets fruit back to tree position (for respawning).
    // ResetToTree - Reset quả về vị trí cây (để respawn).
    // =========================================================================
    public void ResetToTree()
    {
        // Reset position and parent
        if (_originalParent != null)
        {
            transform.SetParent(_originalParent);
        }
        transform.position = _originalPosition;
        transform.rotation = _originalRotation;

        // Reset physics
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Reset state
        isOnTree = true;
        canCollect = false;
        _collected = false;
        
        gameObject.SetActive(true);
    }
}
