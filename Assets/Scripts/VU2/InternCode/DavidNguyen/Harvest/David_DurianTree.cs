using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// =============================================================================
// David_DurianTree - Durian tree with auto-dropping ripe fruits.
// David_DurianTree - Cây sầu riêng với quả tự rụng khi chín.
// 
// Durians automatically fall during rainy season (no player action needed).
// Sầu riêng tự động rơi trong mùa mưa 
// =============================================================================
public class David_DurianTree : MonoBehaviour
{
    [Header("Durian Tree Config / Cấu hình cây sầu riêng")]
    
    [Tooltip("Thời gian chờ tối thiểu giữa mỗi quả rơi (giây)")]
    public float minDropInterval = 3f;
    
    [Tooltip("Thời gian chờ tối đa giữa mỗi quả rơi (giây)")]
    public float maxDropInterval = 8f;
    
    [Tooltip("Chỉ rụng trong mùa mưa?")]
    public bool onlyDropInRainySeason = true;

    [Tooltip("Số trái tối đa rụng mỗi mùa (1 = mỗi cây chỉ rụng 1 trái)")]
    public int maxFruitsPerSeason = 1;

    [Header("Audio")]
    public AudioClip fallSound;

    [Header("References")]
    [Tooltip("Kéo tất cả David_Fruit (sầu riêng) con của cây vào đây")]
    public David_Fruit[] durians;

    // Internal state
    private List<David_Fruit> _duriansOnTree = new List<David_Fruit>();
    private Coroutine _autoDropCoroutine;
    private bool _isDropping = false;
    private int _droppedThisSeason = 0;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================
    
    // Reference to wilt controller to check if tree is healthy
    private David_TreeWiltController _wiltController;

    private void Start()
    {
        // Auto-find durians if not assigned
        if (durians == null || durians.Length == 0)
        {
            durians = GetComponentsInChildren<David_Fruit>(true);
            Debug.Log($"[David_DurianTree] Tìm thấy {durians.Length} quả sầu riêng");
        }
        
        _wiltController = GetComponent<David_TreeWiltController>();
        if (_wiltController == null)
        {
            _wiltController = GetComponentInParent<David_TreeWiltController>();
        }

        ResetAllDurians();
    }

    private void OnEnable()
    {
        GameRulesProvider.OnPhaseChanged += OnSeasonChanged;
        
        CheckAndStartAutoDrop();
    }

    private void OnDisable()
    {
        GameRulesProvider.OnPhaseChanged -= OnSeasonChanged;
        StopAutoDrop();
    }

    // =========================================================================
    // SEASON HANDLING
    // XỬ LÝ MÙA
    // =========================================================================

    private void OnSeasonChanged(SeasonPhase newPhase)
    {
        // Phase 1 (Rainy1 = T11-T1): reset and start dropping
        if (newPhase == SeasonPhase.Rainy1)
        {
            CleanupDroppedFruits();
            _droppedThisSeason = 0;
            ResetAllDurians();
            ShowAllDurians();
            StartAutoDrop();
        }
        // Phase 2 (Dry = T2-T3): continue dropping if durians remain
        else if (newPhase == SeasonPhase.Dry)
        {
            // Allow continued auto-drop in Phase 2
            if (!_isDropping && _duriansOnTree.Count > 0 && _droppedThisSeason < maxFruitsPerSeason)
                StartAutoDrop();
        }
        // Phase 3 (Rainy2 = T4): stop everything, hide all durians
        // Giai đoạn 3 (Rainy2 = T4): dừng mọi thứ, ẩn tất cả sầu riêng
        else if (newPhase == SeasonPhase.Rainy2)
        {
            StopAutoDrop();
            HideAllDurians();
        }
    }
    
    /// <summary>
    /// Cleanup/destroy fruits that have fallen to the ground but not collected.
    /// Dọn dẹp/xóa trái đã rơi xuống đất nhưng chưa được nhặt.
    /// </summary>
    private void CleanupDroppedFruits()
    {
        int cleanedCount = 0;
        
        foreach (var durian in durians)
        {
            if (durian == null) continue;
            
            if (!durian.isOnTree && durian.gameObject.activeInHierarchy)
            {
                Destroy(durian.gameObject);
                cleanedCount++;
            }
        }
    }

    private void CheckAndStartAutoDrop()
    {
        if (!onlyDropInRainySeason)
        {
            StartAutoDrop();
            return;
        }
        if (GameRulesProvider.Saltwater_Intrusion < 1f)
        {
            StartAutoDrop();
        }
    }

    // =========================================================================
    // AUTO DROP COROUTINE
    // COROUTINE TỰ ĐỘNG RƠI
    // =========================================================================

    private void StartAutoDrop()
    {
        if (_isDropping) return;
        if (_duriansOnTree.Count == 0) return;

        _isDropping = true;
        _autoDropCoroutine = StartCoroutine(AutoDropCoroutine());
    }

    private void StopAutoDrop()
    {
        if (_autoDropCoroutine != null)
        {
            StopCoroutine(_autoDropCoroutine);
            _autoDropCoroutine = null;
        }
        _isDropping = false;
    }

    private IEnumerator AutoDropCoroutine()
    {
        while (_duriansOnTree.Count > 0)
        {
            // Stop if already dropped enough fruits this season.
            // Dừng nếu đã rụng đủ số trái trong mùa này.
            if (_droppedThisSeason >= maxFruitsPerSeason)
                break;

            // Wait random interval
            float waitTime = Random.Range(minDropInterval, maxDropInterval);
            yield return new WaitForSeconds(waitTime);

            // Check if still in rainy season
            if (onlyDropInRainySeason && !IsRainySeason())
            {
                break;
            }
            
            // CHECK IF TREE IS WILTED - Don't drop if tree is dead!
            if (_wiltController != null && _wiltController.IsWilted)
            {
                yield return new WaitForSeconds(2f);
                continue;
            }

            // Drop one random durian
            DropRandomDurian();
            _droppedThisSeason++;
        }

        _isDropping = false;
    }

    // =========================================================================
    // VISIBILITY — Phase 3 hides all durians on the tree.
    // ẨN/HIỆN — Giai đoạn 3 ẩn tất cả sầu riêng trên cây.
    // =========================================================================

    /// <summary>
    /// Hides all durians (on tree + uncollected fallen).
    /// Ẩn tất cả sầu riêng (trên cây + đã rơi nhưng chưa nhặt).
    /// </summary>
    private void HideAllDurians()
    {
        foreach (var durian in durians)
        {
            if (durian != null && durian.gameObject.activeInHierarchy)
            {
                durian.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Shows all durians that are still on the tree (not yet dropped).
    /// Hiện lại sầu riêng còn trên cây (chưa rơi).
    /// </summary>
    private void ShowAllDurians()
    {
        foreach (var durian in durians)
        {
            if (durian != null && durian.isOnTree)
            {
                durian.gameObject.SetActive(true);
            }
        }
    }

    // =========================================================================
    // DROP LOGIC
    // LOGIC RƠI
    // =========================================================================

    /// <summary>
    /// Drops a random durian from the tree.
    /// Rơi một quả sầu random từ cây.
    /// </summary>
    public void DropRandomDurian()
    {
        if (_duriansOnTree.Count == 0)
        {
            return;
        }

        // Pick random durian
        int index = Random.Range(0, _duriansOnTree.Count);
        David_Fruit durian = _duriansOnTree[index];
        
        // Remove from list
        _duriansOnTree.RemoveAt(index);

        // Drop it
        DropDurian(durian);
    }

    /// <summary>
    /// Drops a specific durian.
    /// Rơi một quả sầu cụ thể.
    /// </summary>
    public void DropDurian(David_Fruit durian)
    {
        if (durian == null) return;

        // Update fruit state
        durian.isOnTree = false;
        durian.canCollect = true;

        // =========================================================================
        // FIX: Ignore collision between durian and tree colliders
        // This prevents the durian from bouncing off the tree's Box Collider when it falls.
        // KHẮC PHỤC: Bỏ qua va chạm giữa sầu riêng và collider của cây
        // Điều này ngăn sầu riêng bị bật ra khỏi Box Collider của cây khi rơi.
        // =========================================================================
        Collider durianCol = durian.GetComponent<Collider>();
        Collider[] treeColliders = GetComponentsInChildren<Collider>();
        if (durianCol != null)
        {
            foreach (var treeCol in treeColliders)
            {
                // Skip if same object (the durian itself)
                if (treeCol.gameObject == durian.gameObject) continue;
                Physics.IgnoreCollision(durianCol, treeCol, true);
            }
        }

        // Enable physics
        var rb = durian.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Reset any existing velocity first
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            rb.isKinematic = false;
            rb.useGravity = true;
            
            // Add slight DOWNWARD force only (not random) to ensure it falls down
            // Thêm lực hướng XUỐNG (không random) để đảm bảo rơi xuống
            rb.AddForce(Vector3.down * 0.5f, ForceMode.Impulse);
        }

        // Detach from tree
        durian.transform.SetParent(null);
        
        // Enable XR Grab Interactable so player can pick up
        var grabInteractable = durian.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
            durian.SetupAfterDrop();
        }

        // Play fall sound
        if (fallSound != null)
        {
            AudioSource.PlayClipAtPoint(fallSound, durian.transform.position);
        }

    }

    // =========================================================================
    // UTILITY
    // =========================================================================

    private bool IsRainySeason()
    {
        return GameRulesProvider.Saltwater_Intrusion < 1f;
    }

    /// <summary>
    /// Gets count of durians still on tree.
    /// </summary>
    public int GetDuriansOnTree()
    {
        return _duriansOnTree.Count;
    }

    /// <summary>
    /// Resets all durians back to tree.
    /// </summary>
    public void ResetAllDurians()
    {
        _duriansOnTree.Clear();

        foreach (var durian in durians)
        {
            if (durian != null)
            {
                durian.isOnTree = true;
                durian.canCollect = false;
                
                // Make kinematic while on tree
                var rb = durian.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.velocity = Vector3.zero;
                }

                _duriansOnTree.Add(durian);
            }
        }

    }

    /// <summary>
    /// Manually trigger drop all (for testing).
    /// </summary>
    public void DropAllNow()
    {
        while (_duriansOnTree.Count > 0)
        {
            DropRandomDurian();
        }
    }
}
