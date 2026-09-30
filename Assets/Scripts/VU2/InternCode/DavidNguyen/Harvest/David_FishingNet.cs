using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// David_FishingNet - Fishing net tool for catching fish/shrimp.
// David_FishingNet - Vợt dùng để bắt cá/tôm.
// 
// Player grabs net → Scoops in water → Fish/Shrimp caught → Score added
// Player cầm vợt → Vớt trong nước → Cá/Tôm bị bắt → Cộng điểm
// =============================================================================
[RequireComponent(typeof(Rigidbody))]
public class David_FishingNet : MonoBehaviour
{
    [Header("Net Config / Cấu hình vợt")]
    
    [Header("Magnet Net Config")]
    [Tooltip("Bán kính hút của vợt (mét)")]
    public float magnetRadius = 3f;
    
    [Tooltip("Điểm tôm sẽ bay vào (đầu vợt). Nếu trống sẽ dùng vị trí của vợt.")]
    public Transform netTipPoint;
    
    [Tooltip("Layer của sinh vật (để tối ưu performace)")]
    public LayerMask creatureLayer = -1; // Default to all

    [Header("Audio")]
    public AudioClip catchSound;
    public AudioClip scoopSound;

    [Header("VFX")]
    public GameObject catchVFX;
    public GameObject waterSplashVFX;

    // References
    private David_FishPond _pond;
    private XRGrabInteractable _grabInteractable;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.activated.AddListener(OnActivateNet);
        }
    }

    private void OnDisable()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.activated.RemoveListener(OnActivateNet);
        }
    }

    // =========================================================================
    // MAGNET LOGIC
    // =========================================================================

    private void OnActivateNet(ActivateEventArgs args)
    {
        CatchNearestCreature();
    }

    private void CatchNearestCreature()
    {
        // 1. Find all creatures in range
        Collider[] hits = Physics.OverlapSphere(transform.position, magnetRadius, creatureLayer);
        
        David_WaterCreature nearestCreature = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            // Check if it is a water creature
            var creature = hit.GetComponentInParent<David_WaterCreature>();
            if (creature != null && creature.CanCatch())
            {
                float dist = Vector3.Distance(transform.position, creature.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestCreature = creature;
                }
            }
        }

        // 2. Pull the nearest one
        if (nearestCreature != null)
        {
            if (_pond != null) _pond.OnCreatureCaught(nearestCreature);

            PlayCatchEffects(nearestCreature.transform.position);
        }
    }

    // Old collision logic removed/disabled as requested for redesign
    private void OnTriggerEnter(Collider other) {}
    private void OnCollisionEnter(Collision collision) {}

    // =========================================================================
    // EFFECTS
    // =========================================================================

    private void PlayCatchEffects(Vector3 position)
    {
        // Sound
        if (catchSound != null)
        {
            AudioSource.PlayClipAtPoint(catchSound, position);
        }

        // VFX
        if (catchVFX != null)
        {
            var vfx = Instantiate(catchVFX, position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        // Water splash
        if (waterSplashVFX != null)
        {
            var splash = Instantiate(waterSplashVFX, position, Quaternion.identity);
            Destroy(splash, 2f);
        }
    }

    // =========================================================================
    // PUBLIC METHODS
    // =========================================================================

    /// <summary>
    /// Sets the pond this net belongs to.
    /// Đặt ao mà vợt này thuộc về.
    /// </summary>
    public void SetPond(David_FishPond pond)
    {
        _pond = pond;
    }
}
