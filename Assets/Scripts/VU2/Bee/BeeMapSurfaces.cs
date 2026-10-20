using UnityEngine;
using UnityEngine.SceneManagement;

// Repair only the known Bee Bloom environment meshes at load time. Never save or
// rewrite the user's scene, existing colliders, transforms or Inspector values.
public static class BeeMapSurfaces
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "SCN_LetItBee" || scene.name == "SCN_BeeBloom") Install(scene);
    }

    public static bool IsWater(string name) => name.StartsWith("SF_Water_Sea");
    public static bool IsGround(string name) => name == "dike"
        || name.StartsWith("SM_Terrain_Island_01") || name.StartsWith("SM_dat_trong_trot_")
        || name.StartsWith("nen_dat_low:");

    public static bool IsLegacyWalkingProxy(Transform target)
    {
        if (target.gameObject.scene.name != "SCN_LetItBee" && target.gameObject.scene.name != "SCN_BeeBloom") return false;
        for (var t = target; t != null; t = t.parent)
            if (t.name == "InfinityWall" || t.name.StartsWith("Teleport Area") || t.name.StartsWith("Teleport Cau")) return true;
        return false;
    }

    public static int Install(Scene scene)
    {
        int added = 0;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var mesh in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!IsGround(mesh.name) && !IsWater(mesh.name) && !mesh.name.StartsWith("SM_House_01")) continue;
            if (mesh.sharedMesh == null || mesh.GetComponent<Collider>() != null) continue;
            var renderer = mesh.GetComponent<Renderer>();
            if (renderer != null && renderer.isPartOfStaticBatch)
            {
                Debug.LogWarning("Bee Bloom: missing baked surface collider on " + mesh.name + ". Rebuild the scene before static batching.", mesh);
                continue;
            }
            // Keep original geometry and all existing collision proxies intact.
            var collider = mesh.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh.sharedMesh;
            added++;
        }
        Physics.SyncTransforms();
        return added;
    }
}
