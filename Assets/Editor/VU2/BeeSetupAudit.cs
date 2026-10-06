// =============================================================================
// BeeSetupAudit - Kiem tra viec setup scene BeeBloom co dung khong.
//
// MENU: VU2/Bee/Audit BeeBloom Setup (Current Scene)
//
// Kiem tra:
//   1. Script ong: Controller / CameraRig / FlightInput / AltitudeHold / Flapping...
//   2. Gameplay: BeeHud, Quest, Sustainability, GameOverScreen, Audio
//   3. Moi truong: ENVIRONMENT / Scenery / VFX / AUDIO, Terrain
//   4. Camera + Lighting
//   5. Script bi mat (Missing script) / component trung
// Ket qua in ra Console theo dang checklist + huong dan fix.
// =============================================================================
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class BeeSetupAudit
{
    [MenuItem("VU2/Bee/Audit BeeBloom Setup (Current Scene)", false, 15)]
    public static void Audit()
    {
        StringBuilder sb = new StringBuilder();
        GameObject[] all = Object.FindObjectsOfType<GameObject>(true);

        sb.AppendLine("================ BEE SETUP AUDIT ================");
        sb.AppendLine("Scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);

        // --- 1. SCRIPT ONG ---
        sb.AppendLine("\n[1] SCRIPT LAI CONG -- bay / dieu khien");
        Check(sb, "BeeController", all, true, "Mat ong. Neu thieu, chay VU2/Bee/Setup Bee Flight Scene.");
        Check(sb, "BeeCameraRig", all, true, "Rig goc nhin VR. Desktop test se tu tat no.");
        Check(sb, "BeeFlightInput", all, false, "Ban phim WASD+Space (Editor). DesktopTestMode tu tao neu thieu.");
        Check(sb, "BeeDesktopTestMode", all, false, "CHE DO TEST PC - can co neu khong dung kinh VR.");
        Check(sb, "BeeAltitudeHold", all, false, "Giu ong sat mat nuoc. Tat khi test bay tu do.");
        Check(sb, "BeeFlapping", all, false, "Hien thi canh dap.");
        Check(sb, "BeePollinationSystem", all, false, "Di chuyen cham hoa.");
        Check(sb, "BeeGameStats", all, false, "Thong ke luot bay.");

        // --- 2. GAMEPLAY ---
        sb.AppendLine("\n[2] GAMEPLAY / HUD");
        Check(sb, "BeeSustainabilityManager", all, true, "Quan ly 3 chi so ben vung.");
        Check(sb, "BeeHud", all, true, "HUD binh mat / la bay / thuoc toc do.");
        Check(sb, "BeeSustainabilityHUD", all, false, "HUD 3 tru cot.");
        Check(sb, "BeeQuestManager", all, false, "Quest thu phan.");
        Check(sb, "BeeGameOverScreen", all, false, "Man hinh Win / Game Over.");
        Check(sb, "BeeAudioManager", all, false, "Am thanh bee.");
        Check(sb, "BeeHive", all, false, "To ong - giao mat.");

        // --- 3. MOI TRUONG ---
        sb.AppendLine("\n[3] MOI TRUONG / CANH QUAN");
        foreach (string root in new[] { "ENVIRONMENT", "Scenery", "VFX", "AUDIO" })
        {
            GameObject g = FindRootByName(all, root);
            if (g == null) { sb.AppendLine("  [ ] " + root + " - CHUA CO (chay VU2/Transfer/Copy Scenery To Active Scene)"); }
            else { sb.AppendLine("  [x] " + root + " - " + CountAll(g.transform) + " object" + (g.activeSelf ? "" : "  (bi TAT)")); }
        }
        int terrains = Object.FindObjectsOfType<Terrain>(true).Length;
        sb.AppendLine("  [" + (terrains > 0 ? "x" : " ") + "] Terrain: " + terrains + " (nen dat)");

        // --- 4. CAMERA / LIGHT ---
        sb.AppendLine("\n[4] CAMERA / LIGHTING");
        Camera cam = Camera.main;
        sb.AppendLine("  [" + (cam != null ? "x" : " ") + "] Camera.main: " + (cam != null ? cam.name : "THIEU - se duoc tu tao khi Play"));
        sb.AppendLine("  [i] XROrigin co: " + (FindComponentByName(all, "XROrigin") ? "Co" : "Khong (OK neu dung desktop test)"));
        Light dir = null;
        foreach (Light l in Object.FindObjectsOfType<Light>(true)) if (l.type == LightType.Directional) { dir = l; break; }
        sb.AppendLine("  [" + (dir != null && dir.enabled ? "x" : " ") + "] Den chinh: " +
            (dir != null ? dir.name + " (enabled=" + dir.enabled + ", cullingMask=" + dir.cullingMask + ")" : "THIEU"));
        sb.AppendLine("  [" + (RenderSettings.skybox != null ? "x" : " ") + "] Skybox: " +
            (RenderSettings.skybox != null ? RenderSettings.skybox.name : "CHUA GAN (nen chay menu Copy Scenery + Lighting)"));

        // --- 5. SCRIPT MAT ---
        sb.AppendLine("\n[5] SCRIPT BI MAT (Missing script)");
        int missing = 0;
        List<string> missingNames = new List<string>();
        foreach (GameObject go in all)
            foreach (Component c in go.GetComponents<Component>())
                if (c == null) { missing++; if (missingNames.Count < 12) missingNames.Add(go.name); }
        sb.AppendLine("  [" + (missing == 0 ? "x" : "!") + "] So component mat: " + missing);
        if (missing > 0) sb.AppendLine("       Vi du: " + string.Join(", ", missingNames.ToArray()));

        sb.AppendLine("\n=> Tong object trong scene: " + all.Length);
        Debug.Log(sb.ToString());
    }
    // ===================== HELPERS =====================
    private static void Check(StringBuilder sb, string typeName, GameObject[] all, bool required, string hint)
    {
        int n = CountComponents(all, typeName);
        if (n > 0) { sb.AppendLine("  [x] " + typeName + " x" + n); return; }
        sb.AppendLine("  [ ] " + typeName + " - THIEU" + (required ? " (BAT BUOC)" : "") + " | " + hint);
    }

    private static int CountComponents(GameObject[] all, string typeName)
    {
        int n = 0;
        foreach (GameObject go in all)
            foreach (Component c in go.GetComponents<Component>())
                if (c != null && c.GetType().Name == typeName) n++;
        return n;
    }

    private static bool FindComponentByName(GameObject[] all, string typeName)
    {
        return CountComponents(all, typeName) > 0;
    }

    private static GameObject FindRootByName(GameObject[] all, string name)
    {
        foreach (GameObject go in all)
            if (go.name == name && go.transform.parent == null) return go;
        return null;
    }

    private static int CountAll(Transform t)
    {
        int n = 1;
        for (int i = 0; i < t.childCount; i++) n += CountAll(t.GetChild(i));
        return n;
    }
}