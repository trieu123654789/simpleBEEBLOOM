using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// David_FishPond - Water zone for catching fish/shrimp with net and bucket.
// David_FishPond - Vung nuoc de bat ca/tom bang vot va xo.
// 
// Player enters pond -> Bucket (left) + Net (right) spawn -> Player scoops -> Caught
// =============================================================================
public class David_FishPond : HarvestZone
{
    [Header("Pond Config / Cau hinh ao")]
    
    [Tooltip("Prefab xo nuoc de spawn vao tay trai")]
    public GameObject bucketPrefab;
    
    [Tooltip("Prefab vot de spawn vao tay phai")]
    public GameObject fishingNetPrefab;
    
    [Tooltip("Tu dong spawn dung cu khi vao zone?")]
    public bool autoSpawnTools = true;
    
    [Header("Bucket Spawn Settings (TAY TRAI)")]
    [Tooltip("Vi tri offset cua xo")]
    public Vector3 bucketPositionOffset = Vector3.zero;
    
    [Tooltip("Rotation offset cua xo (Euler angles)")]
    public Vector3 bucketRotationOffset = new Vector3(0f, 0f, 0f);
    
    [Tooltip("Kich thuoc xo (1 = goc)")]
    public float bucketScale = 1f;
    
    [Header("Net Spawn Settings (TAY PHAI)")]
    [Tooltip("Vi tri offset cua vot")]
    public Vector3 netPositionOffset = Vector3.zero;
    
    [Tooltip("Rotation offset cua vot (Euler angles)")]
    public Vector3 netRotationOffset = new Vector3(-90f, 0f, 0f);
    
    [Tooltip("Kich thuoc vot (1 = goc)")]
    public float netScale = 1f;

    [Header("Creature Config / Cau hinh sinh vat")]
    [Tooltip("Cac ca/tom trong ao nay")]
    public David_WaterCreature[] creatures;
    
    [Tooltip("Thoi gian respawn sinh vat (giay)")]
    public float creatureRespawnTime = 45f;
    
    [Tooltip("So sinh vat toi da co the bat moi lan vao ao")]
    public int maxCatchPerVisit = 5;

    [Header("Audio")]
    public AudioClip toolSpawnSound;
    public AudioClip toolDestroySound;
    public AudioClip splashSound;

    // Runtime state
    private bool _hasTools = false;
    private int _catchCount = 0;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        if (gameObject.name.ToLower().Contains("shrimp") || 
            gameObject.tag.ToLower().Contains("shrimp"))
        {
            zoneType = HarvestZoneType.ShrimpPond;
            promptText = "Di vao de bat tom";
        }
        else
        {
            zoneType = HarvestZoneType.FishPond;
            promptText = "Di vao de bat ca";
        }
    }

    private void Start()
    {
        if (creatures == null || creatures.Length == 0)
        {
            creatures = GetComponentsInChildren<David_WaterCreature>(true);
        }
    }

    // =========================================================================
    // ZONE EVENTS - Override from HarvestZone
    // =========================================================================

    protected override void PerformInteraction()
    {
        if (!_hasTools)
        {
            SpawnTools();
        }
        base.PerformInteraction();
    }

    protected override void ShowPrompt()
    {
        base.ShowPrompt();
        
        _catchCount = 0;
        
        if (autoSpawnTools && !_hasTools)
        {
            SpawnTools();
        }

        if (splashSound != null)
        {
            AudioSource.PlayClipAtPoint(splashSound, transform.position);
        }
    }

    protected override void HidePrompt()
    {
        base.HidePrompt();
        
        if (_hasTools)
        {
            DestroyTools();
        }
    }

    // =========================================================================
    // TOOL MANAGEMENT (via ToolManager)
    // =========================================================================

    public void SpawnTools()
    {
        // Check if we have at least one tool to spawn
        if (fishingNetPrefab == null && bucketPrefab == null)
        {
            return;
        }

        if (_hasTools) return;

        // Create configs for each tool (if prefab exists)
        ToolSpawnConfig bucketConfig = null;
        if (bucketPrefab != null)
        {
            bucketConfig = new ToolSpawnConfig(bucketPrefab, bucketPositionOffset, bucketRotationOffset, bucketScale);
        }
        
        ToolSpawnConfig netConfig = null;
        if (fishingNetPrefab != null)
        {
            netConfig = new ToolSpawnConfig(fishingNetPrefab, netPositionOffset, netRotationOffset, netScale);
        }

        // Use ToolManager to equip (either or both can be null)
        if (David_ToolManager.Instance != null)
        {
            David_ToolManager.Instance.EquipToolsForZone(this, bucketConfig, netConfig, true);
            
            // Setup net reference to this pond (if net exists)
            if (netConfig != null)
            {
                var net = David_ToolManager.Instance.GetRightTool()?.GetComponent<David_FishingNet>();
                if (net != null)
                {
                    net.SetPond(this);
                }
            }
        }

        _hasTools = true;

        if (toolSpawnSound != null)
        {
            AudioSource.PlayClipAtPoint(toolSpawnSound, transform.position);
        }

    }

    public void DestroyTools()
    {
        if (toolDestroySound != null)
        {
            AudioSource.PlayClipAtPoint(toolDestroySound, transform.position);
        }

        if (David_ToolManager.Instance != null)
        {
            David_ToolManager.Instance.UnequipTools();
        }

        _hasTools = false;
    }

    // =========================================================================
    // CATCHING
    // =========================================================================

    public void OnCreatureCaught(David_WaterCreature creature)
    {
        if (creature == null) return;
        
        if (_catchCount >= maxCatchPerVisit)
        {
            return;
        }

        // Note: creature.Catch() removed - now handled by direct grab interaction
        _catchCount++;
        
    }

    public int GetCatchableCreatureCount()
    {
        int count = 0;
        foreach (var creature in creatures)
        {
            if (creature != null && creature.CanCatch()) count++;
        }
        return count;
    }

    public void RespawnAllCreatures()
    {
        StartCoroutine(RespawnCreaturesCoroutine());
    }

    private IEnumerator RespawnCreaturesCoroutine()
    {
        yield return new WaitForSeconds(creatureRespawnTime);
        foreach (var creature in creatures)
        {
            if (creature != null) creature.Respawn();
        }
    }
}
