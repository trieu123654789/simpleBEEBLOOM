using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// HarvestZoneType - Types of harvest zones in the game.
// =============================================================================
public enum HarvestZoneType
{
    None,
    CoconutTree,   // Cây dừa - gõ để rơi
    DurianTree,    // Cây sầu riêng - tự rơi
    RiceField,     // Ruộng lúa - liềm cắt
    FishPond,      // Ao cá - vợt vớt
    ShrimpPond,     // Ao tôm - vợt vớt
    Egg             // Trung
}

// =============================================================================
// HarvestZone - Base class for harvest zone detection and UI prompts.
// HarvestZone - Lớp cơ sở cho phát hiện vùng thu hoạch và hiển thị UI.
// =============================================================================
public class HarvestZone : MonoBehaviour
{
    [Header("Zone Config / Cấu hình vùng")]
    public HarvestZoneType zoneType = HarvestZoneType.None;
    public string playerTag = "Player";

    [Header("UI Prompt / Hiển thị gợi ý")]
    public string promptText = "Bóp Trigger để tương tác";
    public GameObject promptUI;

    [Header("Input / Điều khiển")]
    public InputActionReference interactAction;
    public bool autoInteractOnEnter = false;

    [Header("Keyboard Testing / Test bằng bàn phím")]
    public KeyCode testInteractKey = KeyCode.E;
    public bool enableKeyboardTesting = true;

    [Header("Events / Sự kiện")]
    public UnityEvent OnInteract;
    public UnityEvent OnPlayerEnter;
    public UnityEvent OnPlayerExit;

    protected bool _isPlayerInZone = false;
    protected GameObject _player;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================
    
    protected virtual void OnEnable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.Enable();
            interactAction.action.performed += OnInteractPressed;
        }
    }

    protected virtual void OnDisable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.performed -= OnInteractPressed;
        }
        HidePrompt();
    }

    protected virtual void Update()
    {
        // Keyboard testing: Press E when in zone.
        if (enableKeyboardTesting && _isPlayerInZone)
        {
            if (Input.GetKeyDown(testInteractKey))
            {
                PerformInteraction();
            }
        }
    }

    // =========================================================================
    // TRIGGER DETECTION
    // =========================================================================
    
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (_isPlayerInZone) return;
        
        _isPlayerInZone = true;
        _player = other.gameObject;
        
        ShowPrompt();
        OnPlayerEnter?.Invoke();
        
        
        if (autoInteractOnEnter)
        {
            PerformInteraction();
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        
        _isPlayerInZone = false;
        _player = null;
        
        HidePrompt();
        OnPlayerExit?.Invoke();
        
    }

    // =========================================================================
    // INPUT HANDLING
    // =========================================================================
    
    private void OnInteractPressed(InputAction.CallbackContext context)
    {
        if (!_isPlayerInZone) return;
        PerformInteraction();
    }

    protected virtual void PerformInteraction()
    {
        OnInteract?.Invoke();
    }

    // =========================================================================
    // UI PROMPT
    // =========================================================================
    
    protected virtual void ShowPrompt()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(true);
            
            // Tìm TMP_Text trên object hoặc children
            var tmpText = promptUI.GetComponent<TMPro.TMP_Text>();
            if (tmpText == null)
            {
                tmpText = promptUI.GetComponentInChildren<TMPro.TMP_Text>();
            }
            
            if (tmpText != null)
            {
                tmpText.text = promptText;
            }
            else
            {
                // Fallback cho UI Text cũ
                var uiText = promptUI.GetComponent<UnityEngine.UI.Text>();
                if (uiText == null)
                {
                    uiText = promptUI.GetComponentInChildren<UnityEngine.UI.Text>();
                }
                
                if (uiText != null)
                {
                    uiText.text = promptText;
                }
            }
        }
    }

    protected virtual void HidePrompt()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
    }

    // =========================================================================
    // PUBLIC METHODS
    // =========================================================================
    
    public bool IsPlayerInZone() => _isPlayerInZone;
    public GameObject GetPlayer() => _player;
    
    public void TriggerInteraction()
    {
        if (_isPlayerInZone)
        {
            PerformInteraction();
        }
    }
}
