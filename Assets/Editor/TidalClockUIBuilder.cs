using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

// =============================================================================
// TidalClockUIBuilder
// Editor tool: tự động dựng toàn bộ Canvas hierarchy cho Tidal Clock UI.
//
// Menu: Tools → Tidal Clock → Build UI Hierarchy
//
// PREREQUISITE: Chạy "Tools → Tidal Clock → Generate All Sprites" trước
// để tạo sprite trong Assets/UI/Sources/Sprites/TidalClock/
// =============================================================================
public class TidalClockUIBuilder : EditorWindow
{
    private static readonly string SpriteFolder = "Assets/UI/Sources/Sprites/TidalClock";

    [MenuItem("Tools/Tidal Clock/Build UI Hierarchy In Scene")]
    public static void BuildUIHierarchy()
    {
        // Verify sprites exist
        if (!Directory.Exists(SpriteFolder))
        {
            if (EditorUtility.DisplayDialog("Sprites Not Found",
                "Sprite folder not found. Generate sprites first?\n\n" +
                "Go to: Tools → Tidal Clock → Generate All Sprites",
                "Generate Now", "Cancel"))
            {
                TidalClockSpriteGenerator.GenerateAll();
            }
            else return;
        }

        // Find or create Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("No Canvas Found",
                "Please open a Scene that contains a Canvas (HUD).\n" +
                "The Tidal Clock panel will be added as a child of the existing Canvas.",
                "OK");
            return;
        }

        // Load sprites
        Sprite clockBG      = LoadSprite("ClockBackground");
        Sprite earthIcon    = LoadSprite("EarthIcon");
        Sprite moonNewMoon  = LoadSprite("MoonPhase_NewMoon");
        Sprite moonFQ       = LoadSprite("MoonPhase_FirstQuarter");
        Sprite moonFull     = LoadSprite("MoonPhase_FullMoon");
        Sprite moonLQ       = LoadSprite("MoonPhase_LastQuarter");
        Sprite posMarker    = LoadSprite("PositionMarker");
        Sprite tickMark     = LoadSprite("TickMark");
        Sprite intensityBar = LoadSprite("IntensityBar");
        Sprite warningIcon  = LoadSprite("WarningIcon");

        // =================================================================
        // Root: TidalClock_Panel
        // =================================================================
        GameObject panel = CreateUIObject("TidalClock_Panel", canvas.transform);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1, 1); // top-right
        panelRT.anchorMax = new Vector2(1, 1);
        panelRT.pivot = new Vector2(1, 1);
        panelRT.anchoredPosition = new Vector2(-20, -20);
        panelRT.sizeDelta = new Vector2(200, 260); // panel + text below

        // Optional panel background (semi-transparent)
        Image panelBG = panel.AddComponent<Image>();
        panelBG.color = new Color(0, 0, 0, 0.3f);
        panelBG.raycastTarget = false;

        // =================================================================
        // ClockBackground
        // =================================================================
        GameObject clockBGObj = CreateUIObject("ClockBackground", panel.transform);
        RectTransform clockBGRT = clockBGObj.GetComponent<RectTransform>();
        clockBGRT.anchorMin = new Vector2(0.5f, 1);
        clockBGRT.anchorMax = new Vector2(0.5f, 1);
        clockBGRT.pivot = new Vector2(0.5f, 1);
        clockBGRT.anchoredPosition = new Vector2(0, -5);
        clockBGRT.sizeDelta = new Vector2(150, 150);
        Image clockBGImg = clockBGObj.AddComponent<Image>();
        clockBGImg.sprite = clockBG;
        clockBGImg.raycastTarget = false;
        clockBGImg.preserveAspect = true;

        // =================================================================
        // EarthIcon (trung tâm đồng hồ)
        // =================================================================
        GameObject earthObj = CreateUIObject("EarthIcon", clockBGObj.transform);
        RectTransform earthRT = earthObj.GetComponent<RectTransform>();
        earthRT.anchorMin = new Vector2(0.5f, 0.5f);
        earthRT.anchorMax = new Vector2(0.5f, 0.5f);
        earthRT.anchoredPosition = Vector2.zero;
        earthRT.sizeDelta = new Vector2(30, 30);
        Image earthImg = earthObj.AddComponent<Image>();
        earthImg.sprite = earthIcon;
        earthImg.raycastTarget = false;
        earthImg.preserveAspect = true;

        // =================================================================
        // TickMarkParent (empty container cho 16 vạch)
        // =================================================================
        GameObject tickParent = CreateUIObject("TickMarkParent", clockBGObj.transform);
        RectTransform tickParentRT = tickParent.GetComponent<RectTransform>();
        tickParentRT.anchorMin = new Vector2(0.5f, 0.5f);
        tickParentRT.anchorMax = new Vector2(0.5f, 0.5f);
        tickParentRT.anchoredPosition = Vector2.zero;
        tickParentRT.sizeDelta = Vector2.zero;

        // Generate 16 tick marks around orbit
        float orbitRadius = 60f;
        for (int i = 0; i < 16; i++)
        {
            float angle = (i / 16f) * 360f * Mathf.Deg2Rad;
            // Convention: 0 = position 1 (left/9h), going clockwise
            // Adjust: start from top and go clockwise for UI
            float uiAngle = (90f - i * 22.5f) * Mathf.Deg2Rad;
            float px = Mathf.Cos(uiAngle) * orbitRadius;
            float py = Mathf.Sin(uiAngle) * orbitRadius;

            GameObject tick = CreateUIObject($"Tick_{i}", tickParent.transform);
            RectTransform tickRT = tick.GetComponent<RectTransform>();
            tickRT.anchoredPosition = new Vector2(px, py);
            tickRT.sizeDelta = new Vector2(5, 5);
            Image tickImg = tick.AddComponent<Image>();
            tickImg.sprite = tickMark;
            tickImg.raycastTarget = false;
        }

        // =================================================================
        // MoonOrbit → MoonIcon
        // =================================================================
        GameObject moonOrbit = CreateUIObject("MoonOrbit", clockBGObj.transform);
        RectTransform moonOrbitRT = moonOrbit.GetComponent<RectTransform>();
        moonOrbitRT.anchorMin = new Vector2(0.5f, 0.5f);
        moonOrbitRT.anchorMax = new Vector2(0.5f, 0.5f);
        moonOrbitRT.anchoredPosition = Vector2.zero;
        moonOrbitRT.sizeDelta = Vector2.zero;

        GameObject moonIcon = CreateUIObject("MoonIcon", moonOrbit.transform);
        RectTransform moonIconRT = moonIcon.GetComponent<RectTransform>();
        moonIconRT.anchoredPosition = new Vector2(-orbitRadius, 0); // start at position 1 (left/9h)
        moonIconRT.sizeDelta = new Vector2(20, 20);
        Image moonImg = moonIcon.AddComponent<Image>();
        moonImg.sprite = moonFull;
        moonImg.raycastTarget = false;
        moonImg.preserveAspect = true;

        // =================================================================
        // Position Markers (4 vị trí cố định trên quỹ đạo)
        // Vị trí 1 = trái (9h), Vị trí 2 = dưới (6h),
        // Vị trí 3 = phải (3h), Vị trí 4 = trên (12h)
        // =================================================================
        Vector2[] markerPositions = new Vector2[]
        {
            new Vector2(-orbitRadius, 0),          // Vị trí 1 - trái (9h)
            new Vector2(0, -orbitRadius),           // Vị trí 2 - dưới (6h)
            new Vector2(orbitRadius, 0),            // Vị trí 3 - phải (3h)
            new Vector2(0, orbitRadius),            // Vị trí 4 - trên (12h)
        };
        string[] markerLabels = { "1_Left", "2_Bottom", "3_Right", "4_Top" };

        for (int i = 0; i < 4; i++)
        {
            GameObject marker = CreateUIObject($"PositionMarker_{i + 1}", clockBGObj.transform);
            RectTransform markerRT = marker.GetComponent<RectTransform>();
            markerRT.anchorMin = new Vector2(0.5f, 0.5f);
            markerRT.anchorMax = new Vector2(0.5f, 0.5f);
            markerRT.anchoredPosition = markerPositions[i];
            markerRT.sizeDelta = new Vector2(10, 10);
            Image markerImg = marker.AddComponent<Image>();
            markerImg.sprite = posMarker;
            markerImg.raycastTarget = false;
        }

        // =================================================================
        // TideStateText (TextMeshPro, dưới đồng hồ)
        // =================================================================
        GameObject tideTextObj = CreateUIObject("TideStateText", panel.transform);
        RectTransform tideTextRT = tideTextObj.GetComponent<RectTransform>();
        tideTextRT.anchorMin = new Vector2(0.5f, 1);
        tideTextRT.anchorMax = new Vector2(0.5f, 1);
        tideTextRT.pivot = new Vector2(0.5f, 1);
        tideTextRT.anchoredPosition = new Vector2(0, -162);
        tideTextRT.sizeDelta = new Vector2(180, 30);
        TextMeshProUGUI tideText = tideTextObj.AddComponent<TextMeshProUGUI>();
        tideText.text = "TRIỀU CƯỜNG";
        tideText.fontSize = 18;
        tideText.fontStyle = FontStyles.Bold;
        tideText.alignment = TextAlignmentOptions.Center;
        tideText.color = new Color(0.9f, 0.3f, 0.2f, 1f);
        tideText.raycastTarget = false;

        // =================================================================
        // MoonPhaseText (TextMeshPro, nhỏ hơn)
        // =================================================================
        GameObject moonTextObj = CreateUIObject("MoonPhaseText", panel.transform);
        RectTransform moonTextRT = moonTextObj.GetComponent<RectTransform>();
        moonTextRT.anchorMin = new Vector2(0.5f, 1);
        moonTextRT.anchorMax = new Vector2(0.5f, 1);
        moonTextRT.pivot = new Vector2(0.5f, 1);
        moonTextRT.anchoredPosition = new Vector2(0, -190);
        moonTextRT.sizeDelta = new Vector2(180, 22);
        TextMeshProUGUI moonText = moonTextObj.AddComponent<TextMeshProUGUI>();
        moonText.text = "Trăng tròn";
        moonText.fontSize = 13;
        moonText.alignment = TextAlignmentOptions.Center;
        moonText.color = new Color(0.7f, 0.75f, 0.85f, 1f);
        moonText.raycastTarget = false;

        // =================================================================
        // IntensityBar (Image, type=Filled)
        // =================================================================
        GameObject barObj = CreateUIObject("IntensityBar", panel.transform);
        RectTransform barRT = barObj.GetComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0.5f, 1);
        barRT.anchorMax = new Vector2(0.5f, 1);
        barRT.pivot = new Vector2(0.5f, 1);
        barRT.anchoredPosition = new Vector2(0, -215);
        barRT.sizeDelta = new Vector2(160, 14);
        Image barImg = barObj.AddComponent<Image>();
        barImg.sprite = intensityBar;
        barImg.type = Image.Type.Filled;
        barImg.fillMethod = Image.FillMethod.Horizontal;
        barImg.fillAmount = 0.75f; // demo value
        barImg.raycastTarget = false;

        // =================================================================
        // WarningIcon (Image, icon đèn đỏ, góc trên phải đồng hồ)
        // =================================================================
        GameObject warnObj = CreateUIObject("WarningIcon", clockBGObj.transform);
        RectTransform warnRT = warnObj.GetComponent<RectTransform>();
        warnRT.anchorMin = new Vector2(1, 1);
        warnRT.anchorMax = new Vector2(1, 1);
        warnRT.pivot = new Vector2(1, 1);
        warnRT.anchoredPosition = new Vector2(5, 5);
        warnRT.sizeDelta = new Vector2(28, 28);
        Image warnImg = warnObj.AddComponent<Image>();
        warnImg.sprite = warningIcon;
        warnImg.raycastTarget = false;

        // =================================================================
        // Select panel in hierarchy
        // =================================================================
        Selection.activeGameObject = panel;
        Undo.RegisterCreatedObjectUndo(panel, "Create TidalClock UI");

        Debug.Log("[TidalClockUIBuilder] ✓ UI hierarchy created under Canvas.");
        EditorUtility.DisplayDialog("Tidal Clock UI",
            "UI hierarchy created successfully!\n\n" +
            "Structure:\n" +
            "Canvas\n" +
            "  └── TidalClock_Panel\n" +
            "      ├── ClockBackground\n" +
            "      │   ├── EarthIcon\n" +
            "      │   ├── TickMarkParent (16 ticks)\n" +
            "      │   ├── MoonOrbit → MoonIcon\n" +
            "      │   ├── PositionMarker_1..4\n" +
            "      │   └── WarningIcon\n" +
            "      ├── TideStateText\n" +
            "      ├── MoonPhaseText\n" +
            "      └── IntensityBar\n\n" +
            "Next: Add TidalClockUI component & assign references.",
            "OK");
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static Sprite LoadSprite(string name)
    {
        string path = $"{SpriteFolder}/{name}.png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"[TidalClockUIBuilder] Sprite not found: {path}");
        return sprite;
    }
}
