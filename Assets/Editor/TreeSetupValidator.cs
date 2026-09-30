using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// =============================================================================
// TreeSetupValidator
// Editor tool: kiểm tra và sửa cấu hình cây trong scene.
//
// Menu:
//   Tools → Tree Setup → Validate Trees In Scene
//   Tools → Tree Setup → Fix All Trees (Tag + Layer + Active)
//   Tools → Tree Setup → Show Tree Report
//
// Vấn đề phát hiện trong AM_Tree_Gr2 prefab:
// 1. Tất cả child tree đều m_IsActive: 0 (inactive) → OverlapSphere không tìm thấy
// 2. Tag = "Untagged" thay vì "Tree" → FindGameObjectsWithTag("Tree") = 0
// 3. Layer có thể không khớp với targetLayerMask của PlayerResourcesManager
// =============================================================================
public class TreeSetupValidator : EditorWindow
{
    // Layer 8 is commonly the "Tree" layer in this project.
    // Change if your project uses a different layer number.
    private static int treeLayer = 8;
    private static string treeTag = "Tree";

    [MenuItem("Tools/Tree Setup/1. Show Tree Report")]
    public static void ShowReport()
    {
        EnsureTagExists(treeTag);

        // Find all Tree components (trees using Tree.cs script)
        Tree[] treeScripts = Object.FindObjectsOfType<Tree>(true); // includeInactive

        // Find by name pattern (AM_Tree, tree planting, coconut, banana...)
        GameObject[] allGOs = Resources.FindObjectsOfTypeAll<GameObject>();
        List<GameObject> treeObjects = new List<GameObject>();
        foreach (var go in allGOs)
        {
            if (go.hideFlags != HideFlags.None) continue;
            if (go.scene.name == null) continue; // skip prefabs not in scene
            
            string nameLower = go.name.ToLower();
            if (nameLower.Contains("tree") || nameLower.Contains("coconut") || 
                nameLower.Contains("banana") || nameLower.Contains("sm_coconut") ||
                nameLower.Contains("sn_banana"))
            {
                treeObjects.Add(go);
            }
        }

        // Also find by tag
        GameObject[] taggedTrees = new GameObject[0];
        try { taggedTrees = GameObject.FindGameObjectsWithTag(treeTag); }
        catch { /* tag doesn't exist yet */ }

        // Build report
        string report = "=== TREE SETUP REPORT ===\n\n";
        
        report += $"[Tree.cs Scripts] Found: {treeScripts.Length}\n";
        foreach (var ts in treeScripts)
        {
            report += $"  • {ts.gameObject.name} | Tag={ts.gameObject.tag} | Layer={ts.gameObject.layer} ({LayerMask.LayerToName(ts.gameObject.layer)}) | Active={ts.gameObject.activeSelf} | ActiveInHierarchy={ts.gameObject.activeInHierarchy}\n";
        }
        
        report += $"\n[Tagged \"{treeTag}\"] Found: {taggedTrees.Length}\n";
        foreach (var go in taggedTrees)
        {
            report += $"  • {go.name} | Layer={go.layer} ({LayerMask.LayerToName(go.layer)}) | Active={go.activeSelf}\n";
        }

        report += $"\n[Name contains 'tree/coconut/banana'] Found: {treeObjects.Count}\n";
        int inactive = 0, wrongTag = 0, wrongLayer = 0;
        foreach (var go in treeObjects)
        {
            bool hasCollider = go.GetComponent<Collider>() != null;
            if (!go.activeInHierarchy) inactive++;
            if (go.tag != treeTag) wrongTag++;
            if (go.layer != treeLayer) wrongLayer++;
            
            // Only show first 30
            if (treeObjects.IndexOf(go) < 30)
            {
                report += $"  • {go.name} | Tag={go.tag} | Layer={go.layer} ({LayerMask.LayerToName(go.layer)}) | Active={go.activeSelf} | Collider={hasCollider}\n";
            }
        }
        if (treeObjects.Count > 30)
            report += $"  ... and {treeObjects.Count - 30} more\n";

        report += $"\n=== ISSUES SUMMARY ===\n";
        report += $"  Inactive tree objects: {inactive}\n";
        report += $"  Wrong tag (not \"{treeTag}\"): {wrongTag}\n";
        report += $"  Wrong layer (not {treeLayer}={LayerMask.LayerToName(treeLayer)}): {wrongLayer}\n";

        // Check PlayerResourcesManager
        PlayerResourcesManager prm = Object.FindObjectOfType<PlayerResourcesManager>(true);
        if (prm != null)
        {
            // Use SerializedObject to read private fields
            SerializedObject so = new SerializedObject(prm);
            SerializedProperty layerProp = so.FindProperty("targetLayerMask");
            SerializedProperty radiusProp = so.FindProperty("workRadius");
            
            int mask = layerProp != null ? layerProp.intValue : -1;
            float radius = radiusProp != null ? radiusProp.floatValue : -1;
            
            report += $"\n=== PlayerResourcesManager ===\n";
            report += $"  Position: {prm.transform.position}\n";
            report += $"  WorkRadius: {radius}\n";
            report += $"  TargetLayerMask value: {mask}\n";
            report += $"  Layer {treeLayer} included in mask: {((mask & (1 << treeLayer)) != 0)}\n";
            report += $"  GameObject active: {prm.gameObject.activeSelf}\n";
        }
        else
        {
            report += "\n[WARNING] PlayerResourcesManager NOT FOUND in scene!\n";
        }

        Debug.Log(report);
        EditorUtility.DisplayDialog("Tree Report", 
            $"Tree.cs scripts: {treeScripts.Length}\n" +
            $"Tagged \"{treeTag}\": {taggedTrees.Length}\n" +
            $"Name-based: {treeObjects.Count}\n\n" +
            $"Issues:\n" +
            $"  Inactive: {inactive}\n" +
            $"  Wrong tag: {wrongTag}\n" +
            $"  Wrong layer: {wrongLayer}\n\n" +
            "Full report printed to Console.",
            "OK");
    }

    [MenuItem("Tools/Tree Setup/2. Fix All Trees (Tag + Layer + Active)")]
    public static void FixAllTrees()
    {
        EnsureTagExists(treeTag);

        int fixedTag = 0, fixedLayer = 0, fixedActive = 0, fixedCollider = 0;

        // Strategy 1: Fix objects with Tree.cs component
        Tree[] treeScripts = Object.FindObjectsOfType<Tree>(true);
        foreach (var ts in treeScripts)
        {
            GameObject go = ts.gameObject;
            Undo.RecordObject(go, "Fix Tree Setup");

            if (go.tag != treeTag) { go.tag = treeTag; fixedTag++; }
            if (go.layer != treeLayer) { go.layer = treeLayer; fixedLayer++; }
            if (!go.activeSelf) { go.SetActive(true); fixedActive++; }
            
            // Ensure collider exists
            if (go.GetComponent<Collider>() == null)
            {
                Undo.AddComponent<BoxCollider>(go);
                fixedCollider++;
            }
            
            EditorUtility.SetDirty(go);
        }

        // Strategy 2: Fix child trees in AM_Tree_Gr2 groups (by name pattern)
        GameObject[] allGOs = Object.FindObjectsOfType<GameObject>(true);
        foreach (var go in allGOs)
        {
            string nameLower = go.name.ToLower();
            // Match individual tree meshes inside grove groups
            bool isTreeMesh = nameLower.Contains("sm_coconut") || 
                              nameLower.Contains("sn_banana") ||
                              nameLower.Contains("sm_tree");
            
            // Match tree planting objects
            bool isTreePlanting = nameLower.Contains("tree planting");
            
            if (!isTreeMesh && !isTreePlanting) continue;

            Undo.RecordObject(go, "Fix Tree Setup");

            // Check if it has Tree component or collider (likely a real tree)
            bool hasCollider = go.GetComponent<Collider>() != null;
            bool hasTree = go.GetComponent<Tree>() != null;

            if (isTreeMesh && hasCollider)
            {
                if (!go.activeSelf) { go.SetActive(true); fixedActive++; }
                if (go.tag != treeTag) { go.tag = treeTag; fixedTag++; }
                if (go.layer != treeLayer) { go.layer = treeLayer; fixedLayer++; }
                EditorUtility.SetDirty(go);
            }

            if (isTreePlanting)
            {
                if (go.tag != treeTag) { go.tag = treeTag; fixedTag++; }
                if (go.layer != treeLayer) { go.layer = treeLayer; fixedLayer++; }
                if (!go.activeSelf) { go.SetActive(true); fixedActive++; }
                EditorUtility.SetDirty(go);
            }
        }

        // Strategy 3: Fix PlayerResourcesManager layer mask
        PlayerResourcesManager prm = Object.FindObjectOfType<PlayerResourcesManager>(true);
        bool fixedMask = false;
        if (prm != null)
        {
            SerializedObject so = new SerializedObject(prm);
            SerializedProperty layerProp = so.FindProperty("targetLayerMask");
            if (layerProp != null)
            {
                int currentMask = layerProp.intValue;
                int treeBit = 1 << treeLayer;
                if ((currentMask & treeBit) == 0)
                {
                    layerProp.intValue = currentMask | treeBit;
                    so.ApplyModifiedProperties();
                    fixedMask = true;
                    Debug.Log($"[TreeSetup] Fixed PlayerResourcesManager.targetLayerMask: added layer {treeLayer}");
                }
            }
        }

        string summary = $"=== TREE FIX COMPLETE ===\n" +
                          $"  Tags fixed: {fixedTag}\n" +
                          $"  Layers fixed: {fixedLayer}\n" +
                          $"  Activated: {fixedActive}\n" +
                          $"  Colliders added: {fixedCollider}\n" +
                          $"  LayerMask fixed: {fixedMask}\n\n" +
                          "Remember to SAVE the scene!";
        
        Debug.Log(summary);
        EditorUtility.DisplayDialog("Fix Trees", summary, "OK");
    }

    [MenuItem("Tools/Tree Setup/3. Validate After Fix")]
    public static void ValidateAfterFix()
    {
        EnsureTagExists(treeTag);

        Tree[] treeScripts = Object.FindObjectsOfType<Tree>(true);
        GameObject[] taggedTrees = new GameObject[0];
        try { taggedTrees = GameObject.FindGameObjectsWithTag(treeTag); }
        catch { }

        PlayerResourcesManager prm = Object.FindObjectOfType<PlayerResourcesManager>(true);
        
        int issues = 0;
        string msg = "";

        if (taggedTrees.Length == 0)
        {
            msg += "✗ No objects tagged \"Tree\" found!\n";
            issues++;
        }
        else
        {
            msg += $"✓ {taggedTrees.Length} objects tagged \"Tree\"\n";
        }

        int activeWithCollider = 0;
        foreach (var go in taggedTrees)
        {
            if (go.activeInHierarchy && go.GetComponent<Collider>() != null)
                activeWithCollider++;
        }
        
        if (activeWithCollider == 0 && taggedTrees.Length > 0)
        {
            msg += "✗ Tagged trees have no active colliders!\n";
            issues++;
        }
        else
        {
            msg += $"✓ {activeWithCollider} active trees with colliders\n";
        }

        if (prm != null)
        {
            SerializedObject so = new SerializedObject(prm);
            SerializedProperty layerProp = so.FindProperty("targetLayerMask");
            int mask = layerProp != null ? layerProp.intValue : 0;
            bool layerOK = (mask & (1 << treeLayer)) != 0;
            
            if (!layerOK)
            {
                msg += $"✗ PlayerResourcesManager.targetLayerMask does NOT include layer {treeLayer}!\n";
                issues++;
            }
            else
            {
                msg += $"✓ LayerMask includes layer {treeLayer}\n";
            }

            msg += $"  WorkRadius: {prm.WorkRadius}\n";
            msg += $"  Position: {prm.transform.position}\n";
        }
        else
        {
            msg += "✗ PlayerResourcesManager not found!\n";
            issues++;
        }

        msg += $"\n{(issues == 0 ? "All checks PASSED!" : $"{issues} issue(s) found.")}";
        
        Debug.Log("[TreeSetup Validation]\n" + msg);
        EditorUtility.DisplayDialog("Validation", msg, "OK");
    }

    // =========================================================================
    // Ensure the "Tree" tag exists in TagManager
    // =========================================================================
    private static void EnsureTagExists(string tag)
    {
        // Check if tag exists
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[TreeSetup] Created missing tag: \"{tag}\"");
        }
    }
}
