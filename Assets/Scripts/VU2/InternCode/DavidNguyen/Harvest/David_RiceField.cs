using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// David_RiceField - Rice field zone that spawns sickle into player's hand.
// David_RiceField - Vung ruong lua spawn liem vao tay player.
// 
// Player enters field -> Sickle auto-spawns into hand -> Swings sickle -> Rice cut
// =============================================================================
public class David_RiceField : HarvestZone
{
    [Header("Rice Field Config / Cau hinh ruong lua")]
    
    [Tooltip("Prefab liem de spawn vao tay phai")]
    public GameObject sicklePrefab;
    
    [Tooltip("Tu dong spawn liem khi vao zone?")]
    public bool autoSpawnSickle = true;
    
    [Header("Sickle Spawn Settings / Cai dat spawn liem")]
    [Tooltip("Vi tri offset cua liem")]
    public Vector3 sicklePositionOffset = Vector3.zero;
    
    [Tooltip("Rotation offset cua liem (Euler angles)")]
    public Vector3 sickleRotationOffset = new Vector3(-90f, 0f, 0f);
    
    [Tooltip("Kich thuoc liem (1 = goc)")]
    public float sickleScale = 1f;

    [Header("Rice Config / Cau hinh lua")]
    [Tooltip("Cac cay lua trong ruong nay")]
    public David_Rice[] riceStems;
    
    [Tooltip("Thoi gian respawn lua (giay)")]
    public float riceRespawnTime = 30f;

    [Header("Audio")]
    public AudioClip sickleSpawnSound;
    public AudioClip sickleDestroySound;

    [Header("Sickle Destroy Delay / Huy liem co do tre")]
    [Tooltip("Thời gian chờ (giây) trước khi hủy liềm khi rời zone.\n" +
             "Delay (seconds) before destroying sickle after leaving zone.\n" +
             "Prevents sickle from disappearing mid-swing.")]
    public float sickleDestroyDelay = 3f;

    // Runtime state
    private bool _hasSickle = false;
    private Coroutine _destroySickleCoroutine = null;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        zoneType = HarvestZoneType.RiceField;
        promptText = "Di vao de nhan liem";
    }

    private void Start()
    {
        // Auto-find rice stems if not assigned
        if (riceStems == null || riceStems.Length == 0)
        {
            riceStems = GetComponentsInChildren<David_Rice>(true);
        }
    }

    // =========================================================================
    // ZONE EVENTS - Override from HarvestZone
    // =========================================================================

    protected override void PerformInteraction()
    {
        if (!_hasSickle)
        {
            SpawnSickle();
        }
        base.PerformInteraction();
    }

    protected override void ShowPrompt()
    {
        base.ShowPrompt();

        // Cancel any pending sickle destruction (player re-entered zone).
        // Hủy lệnh hủy liềm đang chờ (player quay lại zone).
        if (_destroySickleCoroutine != null)
        {
            StopCoroutine(_destroySickleCoroutine);
            _destroySickleCoroutine = null;
        }

        if (autoSpawnSickle && !_hasSickle)
        {
            SpawnSickle();
        }
    }

    protected override void HidePrompt()
    {
        base.HidePrompt();
        
        // Delay sickle destruction so it doesn't vanish mid-swing.
        // Trì hoãn hủy liềm để không biến mất giữa lúc thu hoạch.
        if (_hasSickle && _destroySickleCoroutine == null)
        {
            _destroySickleCoroutine = StartCoroutine(DestroyDelayedSickleCoroutine());
        }
    }

    private IEnumerator DestroyDelayedSickleCoroutine()
    {
        yield return new WaitForSeconds(sickleDestroyDelay);
        DestroySickle();
        _destroySickleCoroutine = null;
    }

    // =========================================================================
    // SICKLE MANAGEMENT (via ToolManager)
    // =========================================================================

    public void SpawnSickle()
    {
        if (sicklePrefab == null)
        {
            return;
        }

        if (_hasSickle) return;

        // Create config for sickle (RIGHT hand only)
        var sickleConfig = new ToolSpawnConfig(sicklePrefab, sicklePositionOffset, sickleRotationOffset, sickleScale);

        // Use ToolManager to equip
        if (David_ToolManager.Instance != null)
        {
            David_ToolManager.Instance.EquipToolsForZone(this, null, sickleConfig, true);
            
            // Setup sickle reference to this field
            var sickle = David_ToolManager.Instance.GetRightTool()?.GetComponent<David_Sickle>();
            if (sickle != null)
            {
                sickle.SetRiceField(this);
            }
        }

        _hasSickle = true;

        if (sickleSpawnSound != null)
        {
            AudioSource.PlayClipAtPoint(sickleSpawnSound, transform.position);
        }

    }

    public void DestroySickle()
    {
        if (sickleDestroySound != null)
        {
            AudioSource.PlayClipAtPoint(sickleDestroySound, transform.position);
        }

        if (David_ToolManager.Instance != null)
        {
            David_ToolManager.Instance.UnequipTools();
        }

        _hasSickle = false;
    }

    // =========================================================================
    // RICE HARVESTING
    // =========================================================================

    public void OnRiceCut(David_Rice rice)
    {
        if (rice == null) return;
        rice.Harvest();
    }

    public int GetHarvestableRiceCount()
    {
        int count = 0;
        foreach (var rice in riceStems)
        {
            if (rice != null && rice.CanHarvest()) count++;
        }
        return count;
    }

    public void RespawnAllRice()
    {
        StartCoroutine(RespawnRiceCoroutine());
    }

    private IEnumerator RespawnRiceCoroutine()
    {
        yield return new WaitForSeconds(riceRespawnTime);
        foreach (var rice in riceStems)
        {
            if (rice != null) rice.Respawn();
        }
    }
}
