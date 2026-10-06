// =============================================================================
// OldGameAudit - Tool quet va don "game cu" con sot trong scene VU2.
// Menu: VU2/Audit/Log | Disable | Delete Old-Game Objects
// =============================================================================
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class OldGameAudit
{
    // Script game cu (GAMA tower-defense demo + legacy VU1).
    private static readonly string[] OldScriptNames =
    {
        "GameManager", "LevelManager", "InputManager", "PlayerResourcesManager",
        "BuildSystemManager", "GhostConstruction", "Connector", "EnemySpawner",
        "TutorialManager", "BaseUI", "GameUI", "PDFMaker", "DayToNightSky",
        "rotateTaget", "waterSound", "BoatController", "changeScene"
    };

    // Script ma code VU2 hien van goi toi -> can than khi xoa.
    private static readonly string[] KeepScriptNames =
    {
        "StatisticsManager", "SubsidenceManager"
    };

    // Ten object thuoc game cu (da kiem chung trong SCN_VU2_Level1_New).
    private static readonly string[] OldObjectNames =
    {
        "EnemySpawners", "SpawnEnemy", "SpawnEnemys", "Waypoints",
        "TutorialManager", "TutorialConstruction", "BuildSystemManager",
        "PlayerResourcesManager", "InputManager", "PDFMaker",
        "LakeBuild", "SluiceGateBuild", "WaterPumpBuild",
        "TutorialConstructionUI", "TutorialFinishUI", "TutorialMovementUI",
        "TutorialRemoveConstructionUI", "TutorialTeleporationUI",
        "GameUI_StartMenu", "SM_Boat2"
    };

    // ===================== 1. LOG =====================
    [MenuItem("VU2/Audit/Log Old-Game Objects", false, 300)]
    public static void LogOldGame()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== OLD-GAME AUDIT: " + SceneName());

        int n1 = 0, n2 = 0;
        sb.AppendLine("--- A. OBJECT theo ten ---");
        foreach (GameObject go in FindByNames(OldObjectNames))
        {
            n1++;
            sb.AppendLine(string.Format("  [OLD] {0}  (active={1})", PathOf(go), go.activeSelf));
        }

        sb.AppendLine("--- B. OBJECT chua script GAMA/VU1 ---");
        foreach (GameObject go in FindByScripts(OldScriptNames, false))
        {
            n2++;
            sb.AppendLine(string.Format("  [OLD-SCRIPT] {0}  -> {1}",
                PathOf(go), string.Join(", ", ScriptNamesOf(go).ToArray())));
        }

        sb.AppendLine("--- C. Script GAMA ma VU2 van goi (can than) ---");
        foreach (GameObject go in FindByScripts(KeepScriptNames, false))
        {
            sb.AppendLine(string.Format("  [KEEP?] {0}  -> {1}",
                PathOf(go), string.Join(", ", ScriptNamesOf(go).ToArray())));
        }

        sb.AppendLine("Tong: OLD=" + n1 + "  OLD-SCRIPT=" + n2);
        Debug.Log(sb.ToString());
    }

    // ===================== 2. DISABLE =====================
    [MenuItem("VU2/Audit/Disable Old-Game Objects", false, 301)]
    public static void DisableOldGame()
    {
        List<GameObject> targets = Collect();
        if (targets.Count == 0) { Debug.Log("[OldGameAudit] Khong tim thay object game cu."); return; }

        string preview = string.Join("\n  ", targets.ConvertAll(PathOf).ToArray());
        if (!EditorUtility.DisplayDialog("Disable Old-Game Objects",
            "SetActive(false) " + targets.Count + " object:\n\n  " + preview + "\n\nTien tuc?", "Disable", "Huy"))
            return;

        int n = 0;
        foreach (GameObject go in targets)
        {
            if (go == null || !go.activeSelf) continue;
            Undo.RecordObject(go, "Disable Old-Game Object");
            go.SetActive(false);
            EditorUtility.SetDirty(go);
            n++;
        }
        Debug.Log("[OldGameAudit] Da disable " + n + " object. Ctrl+Z de undo, roi Save Scene.");
    }

    // ===================== 3. DELETE =====================
    [MenuItem("VU2/Audit/Delete Old-Game Objects", false, 302)]
    public static void DeleteOldGame()
    {
        List<GameObject> targets = Collect();
        if (targets.Count == 0) { Debug.Log("[OldGameAudit] Khong tim thay object game cu."); return; }

        string preview = string.Join("\n  ", targets.ConvertAll(PathOf).ToArray());
        if (!EditorUtility.DisplayDialog("DELETE Old-Game Objects",
            "XOA HAN " + targets.Count + " object:\n\n  " + preview + "\n\nChac chan? (nen Save scene truoc)", "Xoa", "Huy"))
            return;

        HashSet<GameObject> set = new HashSet<GameObject>(targets);
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (GameObject go in targets)
        {
            Transform p = go.transform.parent;
            bool parentInList = false;
            while (p != null)
            {
                if (set.Contains(p.gameObject)) { parentInList = true; break; }
                p = p.parent;
            }
            if (!parentInList) toDestroy.Add(go);
        }

        foreach (GameObject go in toDestroy) Undo.DestroyObjectImmediate(go);
        Debug.Log("[OldGameAudit] Da xoa " + toDestroy.Count + " object goc. Save Scene de luu.");
    }

// ===================== HELPERS =====================
    private static List<GameObject> Collect()
    {
        List<GameObject> all = FindByNames(OldObjectNames);
        foreach (GameObject g in FindByScripts(OldScriptNames, false))
            if (!all.Contains(g)) all.Add(g);
        return all;
    }

    private static string SceneName()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
    }

    private static List<GameObject> FindByNames(string[] names)
    {
        List<GameObject> result = new List<GameObject>();
        foreach (GameObject go in UnityEngine.Object.FindObjectsOfType<GameObject>(true))
        {
            foreach (string n in names)
            {
                if (go.name == n || (n == "SpawnEnemy" && go.name.StartsWith("SpawnEnemy")))
                {
                    if (!result.Contains(go)) result.Add(go);
                    break;
                }
            }
        }
        return result;
    }

    private static List<GameObject> FindByScripts(string[] names, bool skipRoot)
    {
        List<GameObject> result = new List<GameObject>();
        foreach (MonoBehaviour mb in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            string typeName = mb.GetType().Name;
            bool hit = false;
            foreach (string n in names) { if (typeName == n) { hit = true; break; } }
            if (!hit) continue;
            GameObject go = mb.gameObject;
            if (skipRoot && go.transform.parent == null) continue;
            if (!result.Contains(go)) result.Add(go);
        }
        return result;
    }

    private static List<string> ScriptNamesOf(GameObject go)
    {
        List<string> list = new List<string>();
        foreach (Component c in go.GetComponents<Component>())
            list.Add(c == null ? "<MISSING SCRIPT>" : c.GetType().Name);
        return list;
    }

    private static string PathOf(GameObject go)
    {
        List<string> parts = new List<string>();
        Transform t = go.transform;
        while (t != null) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join(" / ", parts.ToArray());
    }
}