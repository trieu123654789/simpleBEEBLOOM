using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// David_ToolManager - Central manager for harvest tools.
// David_ToolManager - Quan ly trung tam cho dung cu thu hoach.
// 
// Controls tool spawning, controller visibility, and hand model hiding.
// Quan ly spawn dung cu, visibility controller, va an hien hand model.
// 
// NOTE: Teleport/Ray interactors are NOT disabled so player can still teleport!
// =============================================================================
public class David_ToolManager : MonoBehaviour
{
    public static David_ToolManager Instance { get; private set; }

    [Header("Controller References / Tham chieu Controller")]
    [Tooltip("Left Controller transform (auto-find if empty)")]
    public Transform leftController;
    
    [Tooltip("Right Controller transform (auto-find if empty)")]
    public Transform rightController;
    
    [Header("Hand/Controller Models / Mo hinh tay/controller")]
    [Tooltip("Left hand/controller model to hide (auto-find if empty)")]
    public List<GameObject> leftHandModels = new List<GameObject>();
    
    [Tooltip("Right hand/controller model to hide (auto-find if empty)")]
    public List<GameObject> rightHandModels = new List<GameObject>();
    
    [Tooltip("Names to search for hand/controller models")]
    public string[] handModelNames = new string[] { "SM_votbattom", "Hand", "Model", "Controller" };

    // Runtime state
    private GameObject _leftTool;
    private GameObject _rightTool;
    private bool _toolsEquipped = false;
    private HarvestZone _currentZone;
    private List<Renderer> _leftRenderers = new List<Renderer>();
    private List<Renderer> _rightRenderers = new List<Renderer>();
    private List<XRDirectInteractor> _directInteractors = new List<XRDirectInteractor>();

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        FindControllers();
        FindHandModels();
        FindDirectInteractors();
        CacheRenderers();
    }

    // =========================================================================
    // AUTO-FIND REFERENCES
    // =========================================================================

    private void FindControllers()
    {
        if (leftController == null || rightController == null)
        {
            // Try finding XR Controllers first
            var xrControllers = FindObjectsOfType<XRBaseController>();
            foreach (var controller in xrControllers)
            {
                string name = controller.name.ToLower();
                string parentName = controller.transform.parent?.name.ToLower() ?? "";
                
                if (name.Contains("left") || parentName.Contains("left"))
                {
                    if (leftController == null)
                    {
                        leftController = controller.transform;
                    }
                }
                else if (name.Contains("right") || parentName.Contains("right"))
                {
                    if (rightController == null)
                    {
                        rightController = controller.transform;
                    }
                }
            }
            
            // Fallback to XRDirectInteractor
            if (leftController == null || rightController == null)
            {
                var interactors = FindObjectsOfType<XRDirectInteractor>();
                foreach (var interactor in interactors)
                {
                    string name = interactor.name.ToLower();
                    string parentName = interactor.transform.parent?.name.ToLower() ?? "";
                    
                    if (name.Contains("left") || parentName.Contains("left"))
                    {
                        if (leftController == null)
                        {
                            leftController = interactor.transform.parent ?? interactor.transform;
                        }
                    }
                    else if (name.Contains("right") || parentName.Contains("right"))
                    {
                        if (rightController == null)
                        {
                            rightController = interactor.transform.parent ?? interactor.transform;
                        }
                    }
                }
            }
        }
    }

    private void FindHandModels()
    {
        if (leftHandModels.Count == 0 && leftController != null)
        {
            FindHandModelsInController(leftController, leftHandModels);
        }
        
        if (rightHandModels.Count == 0 && rightController != null)
        {
            FindHandModelsInController(rightController, rightHandModels);
        }
    }

    private void FindHandModelsInController(Transform controller, List<GameObject> modelList)
    {
        foreach (Transform child in controller.GetComponentsInChildren<Transform>(true))
        {
            // Skip the controller itself
            if (child == controller) continue;
            
            // Check for matching names
            foreach (string searchName in handModelNames)
            {
                if (child.name.ToLower().Contains(searchName.ToLower()))
                {
                    // Verify it has a renderer
                    if (child.GetComponent<MeshRenderer>() != null || 
                        child.GetComponent<SkinnedMeshRenderer>() != null ||
                        child.GetComponent<MeshFilter>() != null)
                    {
                        if (!modelList.Contains(child.gameObject))
                        {
                            modelList.Add(child.gameObject);
                        }
                    }
                }
            }
        }
    }

    private void FindDirectInteractors()
    {
        _directInteractors.Clear();
        var allDirectInteractors = FindObjectsOfType<XRDirectInteractor>();
        foreach (var interactor in allDirectInteractors)
        {
            _directInteractors.Add(interactor);
        }
    }
    
    private void CacheRenderers()
    {
        if (leftController != null)
        {
            _leftRenderers.Clear();
            _leftRenderers.AddRange(leftController.GetComponentsInChildren<Renderer>(true));
        }
        
        if (rightController != null)
        {
            _rightRenderers.Clear();
            _rightRenderers.AddRange(rightController.GetComponentsInChildren<Renderer>(true));
        }
    }

    // =========================================================================
    // TOOL MANAGEMENT
    // =========================================================================

    /// <summary>
    /// Equips tools for a harvest zone with custom offsets from the zone.
    /// Trang bi dung cu cho zone voi offset tu zone.
    /// </summary>
    public void EquipToolsForZone(HarvestZone zone, ToolSpawnConfig leftConfig, ToolSpawnConfig rightConfig, bool hideHands = true)
    {
        if (_toolsEquipped)
        {
            return;
        }

        _currentZone = zone;
        _toolsEquipped = true;

        // Hide hands FIRST before spawning tools
        if (hideHands)
        {
            SetControllersVisible(false);
        }

        // Spawn left tool
        if (leftConfig != null && leftConfig.prefab != null && leftController != null)
        {
            _leftTool = SpawnToolToHand(leftConfig, leftController);
        }

        // Spawn right tool
        if (rightConfig != null && rightConfig.prefab != null && rightController != null)
        {
            _rightTool = SpawnToolToHand(rightConfig, rightController);
        }

    }

    /// <summary>
    /// Spawns a tool prefab and attaches it to a controller.
    /// </summary>
    private GameObject SpawnToolToHand(ToolSpawnConfig config, Transform controller)
    {
        Vector3 spawnPos = controller.position;
        Quaternion spawnRot = controller.rotation;
        
        GameObject tool = Instantiate(config.prefab, spawnPos, spawnRot);
        
        // Parent to controller
        tool.transform.SetParent(controller);
        tool.transform.localPosition = config.positionOffset;
        tool.transform.localRotation = Quaternion.Euler(config.rotationOffset);
        tool.transform.localScale = Vector3.one * config.scale;
        
        // Disable XR Grab since it's attached
        var grabInteractable = tool.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.enabled = false;
        }
        
        // Make rigidbody kinematic
        var rb = tool.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        return tool;
    }

    /// <summary>
    /// Unequips all tools and restores hands.
    /// </summary>
    public void UnequipTools()
    {
        if (!_toolsEquipped) return;

        if (_leftTool != null)
        {
            Destroy(_leftTool);
            _leftTool = null;
        }

        if (_rightTool != null)
        {
            Destroy(_rightTool);
            _rightTool = null;
        }

        SetControllersVisible(true);

        _currentZone = null;
        _toolsEquipped = false;

    }

    // =========================================================================
    // VISIBILITY CONTROL
    // =========================================================================

    public void SetControllersVisible(bool visible)
    {
        foreach (var renderer in _leftRenderers)
        {
            if (renderer != null) renderer.enabled = visible;
        }
        
        foreach (var renderer in _rightRenderers)
        {
            if (renderer != null) renderer.enabled = visible;
        }
        
        foreach (var model in leftHandModels)
        {
            if (model != null) model.SetActive(visible);
        }
        
        foreach (var model in rightHandModels)
        {
            if (model != null) model.SetActive(visible);
        }
        
        SetDirectInteractorsActive(visible);

    }

    private void SetDirectInteractorsActive(bool active)
    {
        foreach (var interactor in _directInteractors)
        {
            if (interactor != null) interactor.enabled = active;
        }
    }

    // =========================================================================
    // PUBLIC GETTERS
    // =========================================================================

    public bool IsToolsEquipped() => _toolsEquipped;
    public HarvestZone GetCurrentZone() => _currentZone;
    public GameObject GetLeftTool() => _leftTool;
    public GameObject GetRightTool() => _rightTool;
}

// =============================================================================
// ToolSpawnConfig - Configuration for spawning a tool
// =============================================================================
[System.Serializable]
public class ToolSpawnConfig
{
    [Tooltip("Prefab dung cu")]
    public GameObject prefab;
    
    [Tooltip("Vi tri offset")]
    public Vector3 positionOffset = Vector3.zero;
    
    [Tooltip("Rotation offset (Euler angles)")]
    public Vector3 rotationOffset = Vector3.zero;
    
    [Tooltip("Scale (1 = original)")]
    public float scale = 1f;
    
    public ToolSpawnConfig() { }
    
    public ToolSpawnConfig(GameObject prefab, Vector3 pos, Vector3 rot, float scale)
    {
        this.prefab = prefab;
        this.positionOffset = pos;
        this.rotationOffset = rot;
        this.scale = scale;
    }
}
