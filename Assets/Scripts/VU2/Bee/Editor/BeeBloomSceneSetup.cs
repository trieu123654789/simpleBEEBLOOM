// =============================================================================
// LetItBeeSceneSetup - Editor Script tự động tạo toàn bộ LetItBee game objects.
// Chạy từ Menu: VU2/Bee/🐝 Setup Let It Bee Scene
//
// SAU KHI CHẠY sẽ tạo:
//   [LetItBee_Manager]    → BeeSustainabilityManager, BeeQuestManager, BeeAudioManager
//   [BeePlayer]           → BeeController, BeePollinationSystem, BeeGameStats, BeeShakeFlap,
//                           BeeControllerShakeAdapter, BeeFlapping, BeeFlightInput, BeeAltitudeHold
//   [BeeHUD]              → BeeHud, BeeSustainabilityHUD, BeeHudSprites (child of Main Camera)
//   [BeeHive]             → BeeHive (tổ ong, đặt tại 0,1,5)
//   [BeeGameOver]         → BeeGameOverScreen
//   [SenZone]             → BeeEcosystemZone + BeeFlowerSpawner (Ring, Lotus, r=5)
//   [DuaZone]             → BeeEcosystemZone + BeeFlowerSpawner (Cluster, Coconut)
//   [LuaZone]             → BeeEcosystemZone + BeeFlowerSpawner (Row, Rice)
// =============================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class LetItBeeSceneSetup
{
    [MenuItem("VU2/Bee/🐝 Setup Let It Bee Scene (Auto)")]
    public static void SetupScene()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "🐝 Setup Let It Bee",
            "Script sẽ tạo tất cả GameObjects cần thiết cho Let It Bee VR.\n\n" +
            "Lưu ý: Bạn vẫn cần gán prefab hoa và AudioClip thủ công trong Inspector.\n\n" +
            "Tiếp tục?",
            "✅ Tạo ngay", "❌ Huỷ");

        if (!confirm) return;

        // =========================================================
        // 1. LetItBee_Manager
        // =========================================================
        var manager = FindOrCreate("LetItBee_Manager");
        EnsureComp<BeeSustainabilityManager>(manager);
        EnsureComp<BeeQuestManager>(manager);
        EnsureComp<BeeAudioManager>(manager);

        // =========================================================
        // 2. BeePlayer
        // =========================================================
        var player = FindOrCreate("BeePlayer");
        player.transform.position = new Vector3(0f, 2f, 0f);

        var beeCtrl     = EnsureComp<BeeController>(player);
        var gameStats   = EnsureComp<BeeGameStats>(player);
        var pollination = EnsureComp<BeePollinationSystem>(player);
        var shakeFlap   = EnsureComp<BeeShakeFlap>(player);
        EnsureComp<BeeControllerShakeAdapter>(player);
        EnsureComp<BeeFlapping>(player);
        EnsureComp<BeeFlightInput>(player);
        EnsureComp<BeeAltitudeHold>(player);

        // Gán references tự động
        pollination.bee           = beeCtrl;
        pollination.stats         = gameStats;

        // =========================================================
        // 3. BeeHUD (child của Main Camera)
        // =========================================================
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("[LetItBeeSetup] Không tìm thấy Main Camera! BeeHUD sẽ là root object.");
        }

        Transform hudParent = mainCam != null ? mainCam.transform : null;
        var hudGO = FindOrCreateChild("BeeHUD", hudParent);

        var beeHud  = EnsureComp<BeeHud>(hudGO);
        var sustHUD = EnsureComp<BeeSustainabilityHUD>(hudGO);
        // BeeHudSprites là static class, không AddComponent — sprites tự sinh runtime

        beeHud.bee   = beeCtrl;
        beeHud.stats = gameStats;
        if (mainCam != null)
        {
            beeHud.targetCamera = mainCam;
            sustHUD.targetCamera = mainCam;
        }

        // =========================================================
        // 4. BeeHive (Tổ ong)
        // =========================================================
        var hiveGO = FindOrCreate("BeeHive");
        hiveGO.transform.position = new Vector3(0f, 0.5f, 8f); // Phía trước
        var hive = EnsureComp<BeeHive>(hiveGO);

        // Hive marker visual
        if (hiveGO.GetComponent<Renderer>() == null)
        {
            var meshGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meshGO.name = "HiveMesh";
            meshGO.transform.SetParent(hiveGO.transform, false);
            meshGO.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
            meshGO.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            hive.hiveRenderer = meshGO.GetComponent<Renderer>();
        }

        // =========================================================
        // 5. BeeGameOverScreen
        // =========================================================
        var gameOverGO = FindOrCreate("BeeGameOver");
        var gameOverScreen = EnsureComp<BeeGameOverScreen>(gameOverGO);
        if (mainCam != null) gameOverScreen.targetCamera = mainCam;

        // Nối vào QuestManager
        var questMgr = manager.GetComponent<BeeQuestManager>();
        if (questMgr != null)
            questMgr.gameOverScreen = gameOverScreen;

        // =========================================================
        // 6. Flower Zones
        // =========================================================
        CreateZone("SenZone",
            position:  new Vector3(-8f, 0f, 5f),
            zoneName:  "Vùng Sen",
            species:   BeeFlower.FlowerSpecies.Lotus,
            pattern:   BeeFlowerSpawner.SpawnPattern.Ring,
            count:     6,
            spacing:   1.5f,
            ringRadius:4f);

        CreateZone("DuaZone",
            position:  new Vector3(8f, 0f, 5f),
            zoneName:  "Vùng Dừa",
            species:   BeeFlower.FlowerSpecies.Coconut,
            pattern:   BeeFlowerSpawner.SpawnPattern.Cluster,
            count:     5,
            spacing:   2f,
            ringRadius:3f);

        CreateZone("LuaZone",
            position:  new Vector3(0f, 0f, 15f),
            zoneName:  "Vùng Lúa",
            species:   BeeFlower.FlowerSpecies.Rice,
            pattern:   BeeFlowerSpawner.SpawnPattern.Row,
            count:     8,
            spacing:   1.8f,
            ringRadius:4f);

        // =========================================================
        // 7. Placeholder flowers (Cube tạm nếu chưa có prefab)
        // =========================================================
        CreatePlaceholderFlowers();

        // =========================================================
        // 8. Save & Report
        // =========================================================
        EditorUtility.SetDirty(manager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "🐝 LetItBee Setup Hoàn tất!",
            "Đã tạo xong tất cả GameObjects!\n\n" +
            "VIỆC CÒN LẠI:\n" +
            "1. Gán Flower Prefab vào BeeFlowerSpawner.flowerPrefabs\n" +
            "2. Gán XR Input Actions vào BeeController.steerAction / flapAction\n" +
            "3. Gán Audio Clips vào BeeAudioManager\n" +
            "4. Gán XR Origin / Camera rig nếu dùng XR\n" +
            "5. Nhấn Play để test!\n\n" +
            "💡 Xem Console để biết thêm chi tiết.",
            "👍 OK");

        Debug.Log("[LetItBeeSetup] ✅ Setup hoàn tất! Kiểm tra Hierarchy.");
        Selection.activeGameObject = manager;
    }

    // =========================================================
    // Menu item kiểm tra compile lỗi
    // =========================================================
    [MenuItem("VU2/Bee/🔍 Validate LetItBee Scripts")]
    public static void ValidateScripts()
    {
        bool ok = true;
        void Check<T>(string name) where T : MonoBehaviour
        {
            var t = typeof(T);
            if (t == null) { Debug.LogError($"[LetItBee] ❌ Missing: {name}"); ok = false; }
            else Debug.Log($"[LetItBee] ✅ {name}");
        }

        Check<BeeController>("BeeController");
        Check<BeeFlower>("BeeFlower");
        Check<BeePollinationSystem>("BeePollinationSystem");
        Check<BeeSustainabilityManager>("BeeSustainabilityManager");
        Check<BeeEcosystemZone>("BeeEcosystemZone");
        Check<BeeHive>("BeeHive");
        Check<BeeEnemyAI>("BeeEnemyAI");
        Check<BeeQuestManager>("BeeQuestManager");
        Check<BeeFlowerSpawner>("BeeFlowerSpawner");
        Check<BeeGameOverScreen>("BeeGameOverScreen");
        Check<BeeAudioManager>("BeeAudioManager");
        Check<BeeSustainabilityHUD>("BeeSustainabilityHUD");
        Check<BeeHud>("BeeHud");

        if (ok) Debug.Log("[LetItBee] 🎉 Tất cả scripts compile OK!");
        else    Debug.LogError("[LetItBee] ⚠️ Có script bị lỗi, kiểm tra Console.");
    }

    // =========================================================
    // Menu item tạo FlowerPrefab mẫu
    // =========================================================
    [MenuItem("VU2/Bee/🌸 Create Sample Flower Prefab")]
    public static void CreateSampleFlowerPrefab()
    {
        string path = "Assets/Prefabs/LetItBee/SampleFlower.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs/LetItBee");

        // Tạo flower GO
        var flowerGO = new GameObject("SampleFlower");
        var flower   = flowerGO.AddComponent<BeeFlower>();

        // Mesh đơn giản: trụ làm thân + sphere làm hoa
        var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stem.name = "Stem";
        stem.transform.SetParent(flowerGO.transform, false);
        stem.transform.localScale  = new Vector3(0.1f, 0.3f, 0.1f);
        stem.transform.localPosition = new Vector3(0f, 0.3f, 0f);

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "FlowerHead";
        head.transform.SetParent(flowerGO.transform, false);
        head.transform.localScale    = new Vector3(0.4f, 0.4f, 0.4f);
        head.transform.localPosition = new Vector3(0f, 0.7f, 0f);

        // Gán renderer cho BeeFlower
        flower.flowerRenderer = head.GetComponent<Renderer>();

        // Thêm Collider cho interaction
        var col = flowerGO.AddComponent<SphereCollider>();
        col.center = new Vector3(0f, 0.7f, 0f);
        col.radius = flower.interactionRadius;
        col.isTrigger = true;

        // Lưu prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(flowerGO, path);
        Object.DestroyImmediate(flowerGO);

        AssetDatabase.Refresh();
        Debug.Log($"[LetItBee] 🌸 Sample Flower Prefab đã tạo tại: {path}");
        EditorGUIUtility.PingObject(prefab);
        Selection.activeObject = prefab;
    }

    // =========================================================
    // Helpers
    // =========================================================
    private static GameObject FindOrCreate(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return existing;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go;
    }

    private static GameObject FindOrCreateChild(string name, Transform parent)
    {
        if (parent != null)
        {
            var child = parent.Find(name);
            if (child != null) return child.gameObject;
        }

        var existing = GameObject.Find(name);
        if (existing != null) return existing;

        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        return go;
    }

    private static T EnsureComp<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    private static void CreateZone(string goName, Vector3 position, string zoneName,
        BeeFlower.FlowerSpecies species, BeeFlowerSpawner.SpawnPattern pattern,
        int count, float spacing, float ringRadius)
    {
        var zoneGO = FindOrCreate(goName);
        zoneGO.transform.position = position;

        var zone = EnsureComp<BeeEcosystemZone>(zoneGO);
        zone.zoneName       = zoneName;
        zone.dominantSpecies = species;

        var spawner = EnsureComp<BeeFlowerSpawner>(zoneGO);
        spawner.species      = species;
        spawner.pattern      = pattern;
        spawner.initialCount = count;
        spawner.spacing      = spacing;
        spawner.ringRadius   = ringRadius;
        spawner.zone         = zone;
    }

    private static void CreatePlaceholderFlowers()
    {
        // Nếu đã có SampleFlower prefab thì gán vào spawners
        string prefabPath = "Assets/Prefabs/LetItBee/SampleFlower.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[LetItBeeSetup] Chưa có flower prefab. Chạy 'Create Sample Flower Prefab' trước.");
            return;
        }

        foreach (var spawner in Object.FindObjectsOfType<BeeFlowerSpawner>())
        {
            if (spawner.flowerPrefabs == null)
                spawner.flowerPrefabs = new System.Collections.Generic.List<GameObject>();

            if (!spawner.flowerPrefabs.Contains(prefab))
                spawner.flowerPrefabs.Add(prefab);
        }
        Debug.Log("[LetItBeeSetup] ✅ Đã gán SampleFlower prefab vào tất cả BeeFlowerSpawner.");
    }
}
#endif
