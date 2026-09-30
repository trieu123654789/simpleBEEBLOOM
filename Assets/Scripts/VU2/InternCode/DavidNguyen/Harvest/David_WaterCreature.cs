using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// CreatureType - Types of water creatures.
// CreatureType - Các loại sinh vật nước.
// =============================================================================
public enum CreatureType
{
    Fish,   // Cá
    Shrimp  // Tôm
}

// =============================================================================
// David_WaterCreature - Fish or shrimp that can be caught with net.
// David_WaterCreature - Cá hoặc tôm có thể bắt bằng vợt.
// 
// Net hits creature → Score calculated → Creature hidden → Respawn after delay
// Vợt chạm sinh vật → Tính điểm → Sinh vật ẩn → Respawn sau delay
// =============================================================================
public class David_WaterCreature : MonoBehaviour
{
    [Header("Creature Config / Cấu hình sinh vật")]
    
    [Tooltip("Loại sinh vật")]
    public CreatureType creatureType = CreatureType.Fish;
    
    [Tooltip("Điểm cơ bản khi bắt được")]
    public int baseScore = 15;
    
    [Tooltip("Có thể bắt được không?")]
    public bool canCatch = true;

    [Header("Zone Reference")]
    [Tooltip("Vùng FarmArea chứa sinh vật này")]
    public FarmArea ownerArea;

    [Header("Visual")]
    [Tooltip("GameObject hiển thị sinh vật (ẩn khi bị bắt)")]
    public GameObject creatureVisual;

    [Header("Movement (Optional)")]
    [Tooltip("Sinh vật có di chuyển không?")]
    public bool enableMovement = true;
    
    [Tooltip("Tốc độ bơi")]
    public float swimSpeed = 1f;
    
    [Tooltip("Phạm vi bơi (từ vị trí gốc)")]
    public float swimRange = 2f;

    [Header("Audio")]
    public AudioClip catchSound;

    // State
    private bool _caught = false;
    private Vector3 _originPosition;
    private Vector3 _targetPosition;
    private float _moveTimer;

    // Components (Single Object Logic)
    private Rigidbody _rb;
    private XRGrabInteractable _grab;
    private David_Fruit _fruitLogic;
    private Collider _collider;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _grab = GetComponent<XRGrabInteractable>();
        _fruitLogic = GetComponent<David_Fruit>();
        _collider = GetComponent<Collider>();
    }

    private void Start()
    {
        // Auto-find FarmArea if not assigned
        if (ownerArea == null)
        {
            ownerArea = GetComponentInParent<FarmArea>();
        }

        // Auto-find visual if not assigned
        if (creatureVisual == null)
        {
            creatureVisual = gameObject;
        }

        // Store origin
        _originPosition = transform.position;
        
        // Configure XR Grab for INSTANT snap (like Durian/Coconut)
        if (_grab != null)
        {
            _grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            _grab.attachEaseInTime = 0f;
            _grab.useDynamicAttach = false; // Snap to hand, not ray point
            _grab.retainTransformParent = false; // Don't keep parent
        }
        
        // Initial State: Swimming
        SetupSwimmingState();
    }

    private void Update()
    {
        if (!enableMovement || _caught) return;
        
        // Simple swim movement
        MoveTowardsTarget();
    }

    private void OnEnable()
    {
        if (_grab != null)
        {
            _grab.selectEntered.AddListener(OnObjectGrabbed);
            _grab.selectExited.AddListener(OnObjectReleased);
        }
    }

    private void OnDisable()
    {
        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(OnObjectGrabbed);
            _grab.selectExited.RemoveListener(OnObjectReleased);
        }
    }

    private void OnObjectGrabbed(SelectEnterEventArgs args)
    {
        // If grabbed while swimming (not yet caught), transition to item state
        if (!_caught && canCatch)
        {
            _caught = true;
            canCatch = false;
            
            if (catchSound != null) AudioSource.PlayClipAtPoint(catchSound, transform.position);
            
            SetupItemState();
        }
    }

    private void OnObjectReleased(SelectExitEventArgs args)
    {
        // If released but NOT collected (still has _fruitLogic enabled)
        // Return to swimming state
        if (_fruitLogic != null && _fruitLogic.enabled)
        {
            // Return to origin position
            transform.position = _originPosition;
            transform.rotation = Quaternion.identity;
            
            // Reset to swimming state
            SetupSwimmingState();
        }
    }

    // =========================================================================
    // STATE MANAGEMENT
    // =========================================================================

    private void SetupSwimmingState()
    {
        _caught = false;
        canCatch = true;

        // Physics: Kinematic (Swimming) - No gravity while swimming
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }

        // Interaction: ALWAYS GRABBABLE (Like Durian/Coconut)
        if (_grab != null)
        {
            _grab.enabled = true;
        }

        // Fruit Logic: Enable so it can be collected when grabbed
        if (_fruitLogic != null)
        {
            _fruitLogic.enabled = true;
        }

        // Collider: SOLID so Ray Interactor can target it
        // (Not trigger - triggers can't be grabbed properly)
        if (_collider != null)
        {
            _collider.enabled = true;
            _collider.isTrigger = false; 
            
            // Ensure grab interactable knows about this collider
            if (_grab != null && !_grab.colliders.Contains(_collider))
            {
                _grab.colliders.Add(_collider);
            }
        }
        
        // Re-enable Pet_AI for swimming movement
        Component[] components = GetComponents<Component>();
        foreach(var c in components)
        {
            if (c.GetType().Name == "Pet_AI")
            {
                ((MonoBehaviour)c).enabled = true;
            }
        }

        PickNewTarget();
    }

    private void SetupItemState()
    {
        // Physics: KEEP KINEMATIC so XR can attach properly (like David_Fruit)
        // Don't enable gravity - it will fight XR attachment!
        if (_rb != null)
        {
            _rb.isKinematic = true;  // Keep kinematic for clean XR grab
            _rb.useGravity = false;  // No gravity while being held
        }

        // Interaction: Grabbable
        if (_grab != null)
        {
            _grab.enabled = true;
        }

        // Fruit Logic: Enable for bagging
        if (_fruitLogic != null)
        {
            _fruitLogic.enabled = true;
            _fruitLogic.canCollect = true;
            _fruitLogic.isOnTree = false; 
            
            if (ownerArea != null) _fruitLogic.ownerArea = ownerArea;
        }

        // Ensure collider is Solid for physics/grabbing
        if (_collider != null)
        {
            _collider.enabled = true;
            _collider.isTrigger = false; // Solid mode for holding
            
            // Fix: Explicitly assign collider to Grab Interactable if missing
            if (_grab != null && !_grab.colliders.Contains(_collider))
            {
                _grab.colliders.Add(_collider);
            }
        }
        
        // Fix: Disable Pet_AI if present so it doesn't fight physics
        Component[] components = GetComponents<Component>();
        foreach(var c in components)
        {
            if (c.GetType().Name == "Pet_AI")
            {
                ((MonoBehaviour)c).enabled = false;
            }
        }
    }

    // =========================================================================
    // MOVEMENT
    // =========================================================================

    private void MoveTowardsTarget()
    {
        if (Vector3.Distance(transform.position, _targetPosition) < 0.05f)
        {
             _moveTimer -= Time.deltaTime;
            if (_moveTimer <= 0)
            {
                PickNewTarget();
            }
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position, 
            _targetPosition, 
            swimSpeed * Time.deltaTime
        );

        Vector3 direction = _targetPosition - transform.position;
        if (direction.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 2f
            );
        }
    }

    private void PickNewTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * swimRange;
        _targetPosition = _originPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        _moveTimer = Random.Range(0.5f, 2f);
    }

    // =========================================================================
    // CATCHING
    // =========================================================================

    public bool CanCatch()
    {
        return canCatch && !_caught;
    }

    // =========================================================================
    // RESPAWN
    // =========================================================================

    public void Respawn()
    {
        // Handle respawn even if it was moved away
        // Reset flags
        _caught = false;
        canCatch = true;

        if (creatureVisual != null) creatureVisual.SetActive(true);
        gameObject.SetActive(true);
        
        // Return to home
        transform.position = _originPosition;
        transform.rotation = Quaternion.identity;
        
        SetupSwimmingState();
    }
}
