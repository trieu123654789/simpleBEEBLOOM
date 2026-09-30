using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// David_Sickle - Sickle tool for cutting rice.
// David_Sickle - Liềm dùng để cắt lúa.
// 
// Player grabs sickle → Swings at rice → Rice gets cut → Score added
// Player cầm liềm → Vung về phía lúa → Lúa bị cắt → Cộng điểm
// =============================================================================
[RequireComponent(typeof(Rigidbody))]
public class David_Sickle : MonoBehaviour
{
    [Header("Sickle Config / Cấu hình liềm")]
    
    [Tooltip("Tốc độ tối thiểu để cắt được (m/s)")]
    public float minCutSpeed = 1.5f;
    
    [Tooltip("Tag của lúa")]
    public string riceTag = "Rice";

    [Header("Audio")]
    public AudioClip cutSound;
    public AudioClip swingSound;

    [Header("VFX")]
    public GameObject cutVFX;

    // References
    private David_RiceField _riceField;
    private Rigidbody _rb;
    private XRGrabInteractable _grabInteractable;
    private Vector3 _lastPosition;
    private float _currentSpeed;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void Start()
    {
        _lastPosition = transform.position;
    }

    private void Update()
    {
        // Calculate swing speed
        _currentSpeed = (transform.position - _lastPosition).magnitude / Time.deltaTime;
        _lastPosition = transform.position;
    }

    // =========================================================================
    // COLLISION DETECTION
    // =========================================================================

    private void OnTriggerEnter(Collider other)
    {
        TryCutRice(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCutRice(collision.gameObject);
    }

    /// <summary>
    /// Attempts to cut rice if conditions are met.
    /// Cố gắng cắt lúa nếu đủ điều kiện.
    /// </summary>
    private void TryCutRice(GameObject target)
    {
        // Check if target is rice
        if (!target.CompareTag(riceTag))
        {
            // Also check for David_Rice component
            var rice = target.GetComponent<David_Rice>();
            if (rice == null)
            {
                rice = target.GetComponentInParent<David_Rice>();
            }
            
            if (rice == null) return;
            
            CutRice(rice);
            return;
        }

        // Has rice tag, find component
        var riceComponent = target.GetComponent<David_Rice>();
        if (riceComponent == null)
        {
            riceComponent = target.GetComponentInParent<David_Rice>();
        }

        if (riceComponent != null)
        {
            CutRice(riceComponent);
        }
    }

    /// <summary>
    /// Cuts the rice.
    /// Cắt lúa.
    /// </summary>
    private void CutRice(David_Rice rice)
    {
        // Check speed threshold
        if (_currentSpeed < minCutSpeed)
        {
            return;
        }

        // Check if rice can be harvested
        if (!rice.CanHarvest())
        {
            return;
        }

        // Notify field if assigned
        if (_riceField != null)
        {
            _riceField.OnRiceCut(rice);
        }
        else
        {
            // Direct harvest
            rice.Harvest();
        }

        // Play effects
        PlayCutEffects(rice.transform.position);

    }

    // =========================================================================
    // EFFECTS
    // =========================================================================

    private void PlayCutEffects(Vector3 position)
    {
        // Sound
        if (cutSound != null)
        {
            AudioSource.PlayClipAtPoint(cutSound, position);
        }

        // VFX
        if (cutVFX != null)
        {
            var vfx = Instantiate(cutVFX, position, Quaternion.identity);
            Destroy(vfx, 2f);
        }
    }

    // =========================================================================
    // PUBLIC METHODS
    // =========================================================================

    /// <summary>
    /// Sets the rice field this sickle belongs to.
    /// Đặt ruộng lúa mà liềm này thuộc về.
    /// </summary>
    public void SetRiceField(David_RiceField field)
    {
        _riceField = field;
    }

    /// <summary>
    /// Gets current swing speed.
    /// Lấy tốc độ vung hiện tại.
    /// </summary>
    public float GetSwingSpeed()
    {
        return _currentSpeed;
    }
}
