using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FixTerrainTreePrototypes
{
    static FixTerrainTreePrototypes()
    {
        EditorApplication.delayCall += Execute;
    }

    [MenuItem("Tools/Fix Missing Tree Prototypes")]
    public static void Execute()
    {
        string[] terrainGuids = AssetDatabase.FindAssets("t:TerrainData");
        int fixedCount = 0;

        foreach (string guid in terrainGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (terrainData == null || terrainData.treePrototypes == null)
                continue;

            TreePrototype[] prototypes = terrainData.treePrototypes;
            bool hasMissing = prototypes.Any(p => p == null || p.prefab == null);

            if (hasMissing)
            {
                Debug.Log($"[FixTerrainTreePrototypes] Found missing tree prototype in {path}. Original count: {prototypes.Length}");
                TreePrototype[] validPrototypes = prototypes.Where(p => p != null && p.prefab != null).ToArray();
                terrainData.treePrototypes = validPrototypes;
                EditorUtility.SetDirty(terrainData);
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[FixTerrainTreePrototypes] Fixed and saved {fixedCount} TerrainData asset(s).");
        }
        else
        {
            Debug.Log("[FixTerrainTreePrototypes] No missing tree prototypes found.");
        }
    }
}
