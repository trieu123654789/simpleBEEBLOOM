using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// David_InstantGrab - Makes any object snap to hand immediately when grabbed.
/// David_InstantGrab - Làm bất kỳ object nào snap về tay ngay lập tức khi grab.
/// 
/// Add this script to any object with XRGrabInteractable to enable instant grab.
/// Thêm script này vào bất kỳ object nào có XRGrabInteractable để bật instant grab.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class David_InstantGrab : MonoBehaviour
{
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    // Track grab state
    private Transform _currentGrabTarget;
    private XRGrabInteractable _grabInteractable;
    
    private void Start()
    {
        SetupInstantGrab();
    }
    
    private void SetupInstantGrab()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        if (_grabInteractable != null)
        {
            // Configure for instant snap
            _grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            _grabInteractable.attachEaseInTime = 0f;
            _grabInteractable.useDynamicAttach = false;
            
            // Subscribe to events
            _grabInteractable.selectEntered.AddListener(OnGrabbed);
            _grabInteractable.selectExited.AddListener(OnReleased);
            
            if (showDebugLogs)
                Debug.Log($"[David_InstantGrab] Configured for {gameObject.name}");
        }
    }
    
    private void OnDestroy()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            _grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
    
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRBaseInteractor interactor)
        {
            // Use controller position, NOT ray hit point!
            _currentGrabTarget = interactor.transform;
            
            // Disable XR tracking - we handle position
            _grabInteractable.trackPosition = false;
            _grabInteractable.trackRotation = false;
            
            // Teleport to controller
            transform.position = _currentGrabTarget.position;
            transform.rotation = _currentGrabTarget.rotation;
            
            if (showDebugLogs)
                Debug.Log($"[David_InstantGrab] Teleported {gameObject.name} to controller");
        }
    }
    
    private void LateUpdate()
    {
        // Force position to controller every frame
        if (_currentGrabTarget != null)
        {
            transform.position = _currentGrabTarget.position;
            transform.rotation = _currentGrabTarget.rotation;
        }
    }
    
    private void OnReleased(SelectExitEventArgs args)
    {
        // Restore XR tracking
        if (_grabInteractable != null)
        {
            _grabInteractable.trackPosition = true;
            _grabInteractable.trackRotation = true;
        }
        
        _currentGrabTarget = null;
        
        if (showDebugLogs)
            Debug.Log($"[David_InstantGrab] Released {gameObject.name}");
    }
}
