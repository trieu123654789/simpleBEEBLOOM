using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// =============================================================================
// VU2SceneBootstrapper - Auto-creates all missing runtime systems at scene load.
// VU2SceneBootstrapper - Tự động tạo tất cả hệ thống còn thiếu khi load scene.
//
// WHY THIS EXISTS:
// The scene SCN_VU2_Level1_New only contains RulesoftheGame_VU2_1 (or VU2_2 for Level 2).
// All other management scripts (TidalClockManager, TidalClockUI, EnemySpawner,
// LevelManager, etc.) are NOT present. This bootstrapper creates them at runtime.
//
// TẠI SAO CẦN SCRIPT NÀY:
// Scene SCN_VU2_Level1_New chỉ chứa RulesoftheGame_VU2_1 (hoặc VU2_2 cho Level 2).
// Các script quản lý khác (TidalClockManager, TidalClockUI, EnemySpawner,
// LevelManager, v.v.) KHÔNG có mặt. Bootstrapper này tạo chúng lúc runtime.
//
// USAGE: Attached to the same "GameManager" GameObject as the Rules class.
// Call Bootstrap() from the Rules class's StartGame().
// SỬ DỤNG: Gắn vào cùng GameObject "GameManager" với class Rules.
// Gọi Bootstrap() từ StartGame() của class Rules.
// =============================================================================
public class VU2SceneBootstrapper : MonoBehaviour
{
    // =========================================================================
    // CONFIGURATION
    // CẤU HÌNH
    // =========================================================================
    [Header("Enemy Spawning / Sinh quái")]
    [Tooltip("Prefab name in Resources/Prefabs/ for SaltyWater enemy.\n"
           + "Tên prefab trong Resources/Prefabs/ cho SaltyWater.")]
    public string saltyWaterPrefabName = "Prefabs/SaltyWater";

    [Tooltip("Number of enemy spawners to create.\n"
           + "Số lượng spawner cần tạo.")]
    public int spawnerCount = 2;

    [Tooltip("Number of waypoints per spawner path.\n"
           + "Số waypoint mỗi đường đi của spawner.")]
    public int waypointsPerSpawner = 4;

    [Tooltip("Spawn interval in seconds.\n" 
           + "Khoảng cách sinh quái (giây).")]
    public float spawnRate = 6f;

    [Tooltip("Delay before first spawn wave (seconds).\n"
           + "Delay trước khi wave đầu tiên spawn (giây).")]
    public float firstSpawnDelay = 15f;

    [Header("Wave Settings / Cài đặt đợt quái")]
    [Tooltip("Số quái mỗi wave (đợt đầu và mặc định).\nEnemies per wave (initial + default).")]
    public int enemiesPerWave = 2;

    [Tooltip("Số quái mỗi wave khi độ mặn cao (>=0.5).\nEnemies per wave at high salinity.")]
    public int enemiesPerWaveHighSalinity = 4;

    [Tooltip("Số quái mỗi wave khi độ mặn cực đại (>=1.0).\nEnemies per wave at maximum salinity.")]
    public int enemiesPerWaveMaxSalinity = 6;

    [Tooltip("Khoảng thời gian giữa các wave (giây).\nSeconds between waves.")]
    public float waveInterval = 60f;

    [Header("Spawn Positions / Vị trí spawn")]
    [Tooltip("Spawn positions. Auto-detected from water objects if empty.\n"
           + "Vị trí spawn. Tự phát hiện từ water objects nếu để trống.")]
    public Vector3[] spawnPositions = new Vector3[0];

    [Tooltip("Target positions (trees/farm area). Auto-detected if empty.\n"
           + "Vị trí đích (cây/farm). Tự phát hiện nếu để trống.")]
    public Vector3[] targetPositions = new Vector3[0];

    [Header("Tidal Clock UI / UI Đồng hồ triều")]
    [Tooltip("Size of the TidalClock panel in world-space UI (meters).\n"
           + "Kích thước panel TidalClock trong UI world-space (mét).")]
    public float tidalPanelScale = 0.002f;

    // =========================================================================
    // INTERNAL STATE
    // TRẠNG THÁI NỘI BỘ
    // =========================================================================
    private bool _bootstrapped = false;
    private List<EnemySpawner> _createdSpawners = new List<EnemySpawner>();
    private GameObject _tidalClockRoot;

    /// <summary>
    /// Whether bootstrapping has already been performed.
    /// Bootstrapping đã được thực hiện chưa.
    /// </summary>
    public bool IsBootstrapped => _bootstrapped;

    /// <summary>
    /// The created spawners (available after Bootstrap()).
    /// Các spawner đã tạo (có sau khi Bootstrap()).
    /// </summary>
    public List<EnemySpawner> CreatedSpawners => _createdSpawners;

    // =========================================================================
    // PUBLIC API
    // API CÔNG KHAI
    // =========================================================================

    /// <summary>
    /// Run full bootstrap: create TidalClockManager, TidalClockUI, EnemySpawners.
    /// Chạy bootstrap đầy đủ: tạo TidalClockManager, TidalClockUI, EnemySpawners.
    /// Safe to call multiple times — only runs once.
    /// An toàn khi gọi nhiều lần — chỉ chạy 1 lần.
    /// </summary>
    public void Bootstrap()
    {
        if (_bootstrapped)
        {
            Debug.Log("[VU2Bootstrapper] Already bootstrapped — skipping.");
            return;
        }

        Debug.Log("[VU2Bootstrapper] ===== BOOTSTRAPPING SCENE =====");

        // Step 1: Create TidalClockManager
        SetupTidalClockManager();

        // Step 2: Create TidalClock UI
        SetupTidalClockUI();

        // Step 3: Auto-detect positions from scene
        AutoDetectPositions();

        // Step 4: Create EnemySpawners with waypoints
        SetupEnemySpawners();

        // Step 5: Start spawning after delay
        StartCoroutine(DelayedSpawnStart());

        _bootstrapped = true;
        Debug.Log("[VU2Bootstrapper] ===== BOOTSTRAP COMPLETE =====");
    }

    // =========================================================================
    // TIDAL CLOCK MANAGER SETUP
    // THIẾT LẬP TIDAL CLOCK MANAGER
    // =========================================================================
    private void SetupTidalClockManager()
    {
        if (TidalClockManager.Instance != null)
        {
            Debug.Log("[VU2Bootstrapper] TidalClockManager already exists — skipping.");
            return;
        }

        GameObject tcmObj = new GameObject("TidalClockManager");
        tcmObj.transform.SetParent(this.transform.parent);
        TidalClockManager tcm = tcmObj.AddComponent<TidalClockManager>();

        // Sync with game rules settings via provider.
        // Đồng bộ với cấu hình game rules qua provider.
        tcm.tidalCycleDuration = GameRulesProvider.MonthDuration;

        Debug.Log($"[VU2Bootstrapper] Created TidalClockManager (cycleDuration={tcm.tidalCycleDuration}s)");
    }

    // =========================================================================
    // TIDAL CLOCK UI SETUP
    // THIẾT LẬP UI ĐỒNG HỒ TRIỀU
    // =========================================================================
    private void SetupTidalClockUI()
    {
        if (FindObjectOfType<TidalClockUI>() != null)
        {
            Debug.Log("[VU2Bootstrapper] TidalClockUI already exists — skipping.");
            return;
        }

        // Create a world-space Canvas for the Tidal Clock panel.
        // Tạo Canvas world-space cho panel Đồng hồ triều.
        _tidalClockRoot = new GameObject("TidalClock_Panel");

        // Try to find the player's camera/XR rig to position the panel.
        // Tìm camera/XR rig để đặt vị trí panel.
        Camera mainCam = Camera.main;
        Transform player = GameRulesProvider.Player;

        // Position: in front of player, slightly to the right and above.
        // Vị trí: phía trước người chơi, hơi sang phải và trên.
        Vector3 panelPos;
        if (player != null)
        {
            panelPos = player.position + player.forward * 2f + Vector3.up * 1.5f + player.right * 1.2f;
        }
        else if (mainCam != null)
        {
            panelPos = mainCam.transform.position + mainCam.transform.forward * 2f + Vector3.up * 1f;
        }
        else
        {
            panelPos = new Vector3(50f, 3f, -6f); // Fallback near GameManager position
        }

        // Create Canvas.
        Canvas canvas = _tidalClockRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        _tidalClockRoot.AddComponent<CanvasScaler>();

        // GraphicRaycaster tắt vì panel này chỉ hiển thị, không cần tương tác.
        // GraphicRaycaster disabled — display-only panel, no interaction needed.
        var gr = _tidalClockRoot.AddComponent<GraphicRaycaster>();
        gr.enabled = false;

        // Ngăn Canvas chặn XR ray (panel này chỉ hiển thị, không tương tác).
        // Prevent Canvas from blocking XR rays (display-only panel).
        CanvasGroup tidalCg = _tidalClockRoot.AddComponent<CanvasGroup>();
        tidalCg.blocksRaycasts = false;
        tidalCg.interactable = false;

        RectTransform canvasRect = _tidalClockRoot.GetComponent<RectTransform>();
        canvasRect.position = panelPos;
        canvasRect.sizeDelta = new Vector2(400, 400);
        canvasRect.localScale = Vector3.one * tidalPanelScale;

        // Make canvas face the player.
        if (player != null)
        {
            _tidalClockRoot.transform.LookAt(player.position + Vector3.up * 1.5f);
            _tidalClockRoot.transform.Rotate(0, 180, 0); // Face toward player
        }

        // --- Build UI hierarchy ---
        // --- Xây dựng hierarchy UI ---

        // Background panel.
        GameObject bgPanel = CreateUIElement("Background", _tidalClockRoot.transform, 
            new Vector2(0.5f, 0.5f), new Vector2(380, 380));
        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.1f, 0.2f, 0.85f);

        // Title text.
        GameObject titleObj = CreateUIElement("Title", bgPanel.transform,
            new Vector2(0.5f, 1f), new Vector2(350, 40));
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "ĐỒNG HỒ TRIỀU";
        titleText.fontSize = 28;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, -25);

        // Clock center (reference point for moon orbit).
        GameObject centerObj = CreateUIElement("ClockCenter", bgPanel.transform,
            new Vector2(0.5f, 0.5f), new Vector2(20, 20));
        Image centerImg = centerObj.AddComponent<Image>();
        centerImg.color = new Color(0.3f, 0.6f, 1f, 1f); // Earth blue
        RectTransform centerRect = centerObj.GetComponent<RectTransform>();
        centerRect.anchoredPosition = new Vector2(0, 10);

        // Moon icon (orbits around center).
        GameObject moonObj = CreateUIElement("MoonIcon", bgPanel.transform,
            new Vector2(0.5f, 0.5f), new Vector2(30, 30));
        Image moonImg = moonObj.AddComponent<Image>();
        moonImg.color = new Color(1f, 0.95f, 0.7f, 1f); // Moon yellow
        RectTransform moonRect = moonObj.GetComponent<RectTransform>();

        // 4 position markers around the orbit.
        Image[] posMarkers = new Image[4];
        string[] markerLabels = { "VT1\nKhông trăng", "VT2\nTrăng khuyết", "VT3\nTrăng tròn", "VT4\nTrăng khuyết" };
        Vector2[] markerPositions = { 
            new Vector2(-80, 10),  // Left (Position 1)
            new Vector2(0, -70),   // Bottom (Position 2)
            new Vector2(80, 10),   // Right (Position 3)
            new Vector2(0, 90)     // Top (Position 4)
        };
        for (int i = 0; i < 4; i++)
        {
            GameObject marker = CreateUIElement($"Marker_{i}", bgPanel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(16, 16));
            posMarkers[i] = marker.AddComponent<Image>();
            posMarkers[i].color = new Color(1f, 1f, 1f, 0.4f);
            marker.GetComponent<RectTransform>().anchoredPosition = markerPositions[i] + new Vector2(0, 10);

            // Label for each position.
            GameObject label = CreateUIElement($"Label_{i}", marker.transform,
                new Vector2(0.5f, 0.5f), new Vector2(100, 30));
            TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = markerLabels[i];
            labelText.fontSize = 10;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
            label.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
        }

        // Tide state text (Triều Cường / Triều Kém).
        GameObject tideTextObj = CreateUIElement("TideStateText", bgPanel.transform,
            new Vector2(0.5f, 0f), new Vector2(300, 35));
        TextMeshProUGUI tideText = tideTextObj.AddComponent<TextMeshProUGUI>();
        tideText.text = "Triều Kém";
        tideText.fontSize = 22;
        tideText.alignment = TextAlignmentOptions.Center;
        tideText.color = new Color(0.2f, 0.6f, 1f);
        tideTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 55);

        // Moon phase name text.
        GameObject phaseTextObj = CreateUIElement("MoonPhaseText", bgPanel.transform,
            new Vector2(0.5f, 0f), new Vector2(300, 25));
        TextMeshProUGUI phaseText = phaseTextObj.AddComponent<TextMeshProUGUI>();
        phaseText.text = "Không trăng";
        phaseText.fontSize = 16;
        phaseText.alignment = TextAlignmentOptions.Center;
        phaseText.color = Color.white;
        phaseTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);

        // Intensity bar.
        GameObject barBg = CreateUIElement("IntensityBarBG", bgPanel.transform,
            new Vector2(0.5f, 0f), new Vector2(200, 12));
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        barBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 12);

        GameObject barFill = CreateUIElement("IntensityFill", barBg.transform,
            new Vector2(0f, 0.5f), new Vector2(200, 12));
        Image fillImg = barFill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.6f, 1f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0f;
        RectTransform fillRect = barFill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Spring Tide warning icon (hidden by default).
        GameObject warningObj = CreateUIElement("SpringTideWarning", bgPanel.transform,
            new Vector2(1f, 1f), new Vector2(40, 40));
        TextMeshProUGUI warningText = warningObj.AddComponent<TextMeshProUGUI>();
        warningText.text = "⚠";
        warningText.fontSize = 32;
        warningText.alignment = TextAlignmentOptions.Center;
        warningText.color = new Color(1f, 0.3f, 0.1f);
        warningObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-30, -30);
        warningObj.SetActive(false);

        // --- Attach TidalClockUI component ---
        // --- Gắn component TidalClockUI ---
        TidalClockUI tidalUI = _tidalClockRoot.AddComponent<TidalClockUI>();
        tidalUI.moonIcon = moonRect;
        tidalUI.clockCenter = centerRect;
        tidalUI.orbitRadius = 80f;
        tidalUI.moonImage = moonImg;
        tidalUI.positionMarkers = posMarkers;
        tidalUI.activeMarkerColor = Color.yellow;
        tidalUI.inactiveMarkerColor = new Color(1f, 1f, 1f, 0.4f);
        tidalUI.tideStateText = tideText;
        tidalUI.moonPhaseText = phaseText;
        tidalUI.tidalIntensityFill = fillImg;
        tidalUI.springTideWarningIcon = warningObj;

        // Create a default gradient for intensity bar.
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.2f, 0.6f, 1f), 0f),
                new GradientColorKey(new Color(0.1f, 0.3f, 0.8f), 0.5f),
                new GradientColorKey(new Color(0.8f, 0.2f, 0.2f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        tidalUI.tidalIntensityGradient = grad;

        // Tắt raycastTarget trên toàn bộ Graphic con để XR ray xuyên qua.
        // Disable raycastTarget on all child Graphics so XR rays pass through.
        foreach (var g in _tidalClockRoot.GetComponentsInChildren<Graphic>(true))
        {
            g.raycastTarget = false;
        }

        Debug.Log("[VU2Bootstrapper] Created TidalClock UI panel (non-blocking)");
    }

    // =========================================================================
    // AUTO-DETECT POSITIONS
    // TỰ PHÁT HIỆN VỊ TRÍ
    // =========================================================================
    private void AutoDetectPositions()
    {
        // Auto-detect spawn positions from water objects in scene.
        // Tự phát hiện vị trí spawn từ water objects trong scene.
        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            // Find the water target (used by game rules for water movement).
            GameObject waterTarget = GameRulesProvider.Target;

            // Find all water-surface objects.
            List<Vector3> waterPositions = new List<Vector3>();
            
            // Use the water target as primary reference.
            if (waterTarget != null)
            {
                Vector3 waterPos = waterTarget.transform.position;
                // Spawn from 3 sides of the water edge (front, left, right).
                waterPositions.Add(waterPos + new Vector3(-30f, 0f, 0f));
                waterPositions.Add(waterPos + new Vector3(0f, 0f, -30f));
                waterPositions.Add(waterPos + new Vector3(30f, 0f, 0f));
            }
            else
            {
                // Fallback: generate spawn positions around origin edge.
                waterPositions.Add(new Vector3(-30f, 0f, -30f));
                waterPositions.Add(new Vector3(0f, 0f, -40f));
                waterPositions.Add(new Vector3(30f, 0f, -30f));
            }

            spawnPositions = waterPositions.ToArray();
            Debug.Log($"[VU2Bootstrapper] Auto-detected {spawnPositions.Length} spawn positions from water objects");
        }

        // Auto-detect target positions from Tree objects.
        // Tự phát hiện vị trí đích từ Tree objects.
        if (targetPositions == null || targetPositions.Length == 0)
        {
            Tree[] trees = FindObjectsOfType<Tree>();
            if (trees.Length > 0)
            {
                List<Vector3> treePositions = new List<Vector3>();
                foreach (Tree t in trees)
                {
                    treePositions.Add(t.transform.position);
                }
                targetPositions = treePositions.ToArray();
                Debug.Log($"[VU2Bootstrapper] Auto-detected {targetPositions.Length} target positions from trees");
            }
            else
            {
                // Fallback: use center of map.
                targetPositions = new Vector3[] {
                    new Vector3(50f, 0f, 0f),
                    new Vector3(40f, 0f, 10f),
                    new Vector3(60f, 0f, 10f)
                };
                Debug.LogWarning("[VU2Bootstrapper] No trees found — using fallback target positions");
            }
        }
    }

    // =========================================================================
    // ENEMY SPAWNER SETUP
    // THIẾT LẬP ENEMY SPAWNER
    // =========================================================================
    private void SetupEnemySpawners()
    {
        // Load SaltyWater prefab from Resources.
        // Tải prefab SaltyWater từ Resources.
        GameObject prefab = Resources.Load<GameObject>(saltyWaterPrefabName);
        if (prefab == null)
        {
            Debug.LogError($"[VU2Bootstrapper] Cannot find prefab at Resources/{saltyWaterPrefabName}! " +
                           "SaltyWater enemies will NOT spawn.");
            return;
        }

        // Create parent container.
        GameObject spawnersRoot = new GameObject("EnemySpawners");
        spawnersRoot.transform.SetParent(this.transform.parent);

        int count = Mathf.Min(spawnerCount, spawnPositions.Length);
        for (int i = 0; i < count; i++)
        {
            // Create spawner at spawn position.
            // Tạo spawner tại vị trí spawn.
            GameObject spawnerObj = new GameObject($"SpawnEnemy{i + 1}");
            spawnerObj.transform.SetParent(spawnersRoot.transform);
            spawnerObj.transform.position = spawnPositions[i];

            // Create waypoints from spawn to nearest target.
            // Tạo waypoints từ spawn đến đích gần nhất.
            GameObject waypointsRoot = new GameObject($"Waypoints_{i + 1}");
            waypointsRoot.transform.SetParent(spawnerObj.transform);

            Vector3 startPos = spawnPositions[i];
            Vector3 endPos = FindNearestTarget(startPos);
            
            List<Transform> waypoints = new List<Transform>();
            for (int w = 0; w < waypointsPerSpawner; w++)
            {
                float t = (float)(w + 1) / waypointsPerSpawner;
                Vector3 wpPos = Vector3.Lerp(startPos, endPos, t);
                // Add slight random offset to make paths look natural.
                wpPos += new Vector3(
                    Random.Range(-3f, 3f), 
                    0f, 
                    Random.Range(-3f, 3f)
                );

                GameObject wpObj = new GameObject($"waypoint{w + 1}");
                wpObj.transform.SetParent(waypointsRoot.transform);
                wpObj.transform.position = wpPos;
                waypoints.Add(wpObj.transform);
            }

            // Final waypoint is exactly at target.
            // Waypoint cuối chính xác tại đích.
            waypoints[waypoints.Count - 1].position = endPos;

            // Add EnemySpawner component.
            EnemySpawner spawner = spawnerObj.AddComponent<EnemySpawner>();
            
            // Set private serialized fields via reflection (since they're [SerializeField] private).
            // Đặt field private qua reflection.
            SetPrivateField(spawner, "spawnPrefab", prefab);
            SetPrivateField(spawner, "spawnRate", spawnRate);
            SetPrivateField(spawner, "wayPoints", waypoints);

            _createdSpawners.Add(spawner);

            Debug.Log($"[VU2Bootstrapper] Created SpawnEnemy{i + 1} at {startPos} → target {endPos} ({waypoints.Count} waypoints)");
        }
    }

    // =========================================================================
    // DELAYED SPAWN START
    // BẮT ĐẦU SPAWN SAU DELAY
    // =========================================================================
    private IEnumerator DelayedSpawnStart()
    {
        // Wait for game to actually start and NavMesh to be ready.
        // Chờ game thực sự bắt đầu và NavMesh sẵn sàng.
        yield return new WaitForSeconds(firstSpawnDelay);

        if (!GameRulesProvider.IsPlaying)
        {
            Debug.Log("[VU2Bootstrapper] Game not running yet — waiting for StartGame...");
            // Wait until game starts.
            while (!GameRulesProvider.IsPlaying)
            {
                yield return new WaitForSeconds(1f);
            }
            yield return new WaitForSeconds(firstSpawnDelay);
        }

        // Start spawning from each spawner.
        // Bắt đầu spawn từ mỗi spawner.
        Debug.Log($"[VU2Bootstrapper] Starting enemy spawning ({_createdSpawners.Count} spawners)...");
        foreach (var spawner in _createdSpawners)
        {
            if (spawner != null)
            {
                spawner.ReStartAutoSpawn(enemiesPerWave);
            }
        }

        // Periodic re-spawn based on tidal cycle.
        // Spawn lặp lại theo chu kỳ triều.
        while (true)
        {
            yield return new WaitForSeconds(waveInterval);

            if (!GameRulesProvider.IsPlaying) break;
            if (!GameRulesProvider.GameActive) break;

            foreach (var spawner in _createdSpawners)
            {
                if (spawner != null)
                {
                    int amount = enemiesPerWave;
                    if (GameRulesProvider.Saltwater_Intrusion >= 0.5f) amount = enemiesPerWaveHighSalinity;
                    if (GameRulesProvider.Saltwater_Intrusion >= 1.0f) amount = enemiesPerWaveMaxSalinity;

                    spawner.ReStartAutoSpawn(amount);
                    Debug.Log($"[VU2Bootstrapper] Re-spawning wave (salinity={GameRulesProvider.Saltwater_Intrusion:F1}, amount={amount})");
                }
            }
        }
    }

    // =========================================================================
    // HELPERS
    // HỖ TRỢ
    // =========================================================================

    /// <summary>
    /// Find the nearest target position from a given source.
    /// Tìm vị trí đích gần nhất từ nguồn.
    /// </summary>
    private Vector3 FindNearestTarget(Vector3 from)
    {
        if (targetPositions == null || targetPositions.Length == 0)
            return from + Vector3.forward * 30f;

        Vector3 nearest = targetPositions[0];
        float minDist = Vector3.Distance(from, nearest);

        for (int i = 1; i < targetPositions.Length; i++)
        {
            float d = Vector3.Distance(from, targetPositions[i]);
            if (d < minDist)
            {
                minDist = d;
                nearest = targetPositions[i];
            }
        }
        return nearest;
    }

    /// <summary>
    /// Create a UI element with RectTransform.
    /// Tạo element UI với RectTransform.
    /// </summary>
    private GameObject CreateUIElement(string name, Transform parent, Vector2 pivot, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.pivot = pivot;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return obj;
    }

    /// <summary>
    /// Set a private/serialized field via reflection.
    /// Đặt field private/serialized qua reflection.
    /// </summary>
    private void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
        else
        {
            Debug.LogWarning($"[VU2Bootstrapper] Field '{fieldName}' not found on {target.GetType().Name}");
        }
    }

    /// <summary>
    /// Called when the bootstrapper is destroyed (cleanup).
    /// Được gọi khi bootstrapper bị hủy.
    /// </summary>
    private void OnDestroy()
    {
        if (_tidalClockRoot != null)
        {
            Destroy(_tidalClockRoot);
        }
    }
}
