using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// Thuan_23127_TotalBoard - Builds an Excel-style summary table (3 phases).
// Thuan_23127_TotalBoard - Xây d?ng b?ng t?ng k?t d?ng Excel (3 giai do?n).
//
// Layout (Excel-like grid):
// +-------------------------------------------------------+
// ¦          ¦   T11–T1     ¦    T2–T3     ¦     T4       ¦
// +----------+--------------+--------------+--------------¦
// ¦ [Icon]   ¦ DT: X        ¦ DT: X        ¦ DT: X        ¦
// ¦ Tôm      ¦ SL: Y        ¦ SL: Y        ¦ SL: Y        ¦
// +----------+--------------+--------------+--------------¦
// ¦ [Icon]   ¦ DT: X        ¦ DT: X        ¦ DT: X        ¦
// ¦ Lúa      ¦ SL: Y        ¦ SL: Y        ¦ SL: Y        ¦
// +-------------------------------------------------------+
//
// DT = Di?n tích (Area) = harvest count × 10
// SL = S?n lu?ng (Production/Score)
// =============================================================================
public class Thuan_23127_TotalBoard : MonoBehaviour
{
    // =========================================================================
    // UI REFERENCES
    // THAM CHI?U UI
    // =========================================================================
    [Header("Scroll content parent / Parent ch?a b?ng")]
    public Transform content;

    [Header("Row prefab (legacy, optional) / Prefab hàng (cu, tùy ch?n)")]
    public UI_ProductRow rowPrefab;

    // =========================================================================
    // TABLE STYLE SETTINGS
    // CÀI Ð?T KI?U B?NG
    // =========================================================================
    [Header("Table Style / Ki?u b?ng")]
    [Tooltip("Font for table text. Leave null for default Arial.")]
    public Font tableFont;

    [Tooltip("Font size for table body text.")]
    public int bodyFontSize = 22;

    [Tooltip("Font size for header text.")]
    public int headerFontSize = 22;

    [Tooltip("Header background color.")]
    public Color headerBgColor = new Color(0.15f, 0.35f, 0.65f, 1f);

    [Tooltip("Header text color.")]
    public Color headerTextColor = Color.white;

    [Tooltip("Body text color.")]
    public Color bodyTextColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    [Tooltip("Even row background.")]
    public Color evenRowColor = new Color(0.92f, 0.95f, 1f, 1f);

    [Tooltip("Odd row background.")]
    public Color oddRowColor = new Color(1f, 1f, 1f, 1f);

    [Tooltip("Grid line color.")]
    public Color gridLineColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Tooltip("Row height for data rows.")]
    public float dataRowHeight = 80f;

    [Tooltip("Row height for header.")]
    public float headerRowHeight = 45f;

    // =========================================================================
    // AREA MULTIPLIER
    // H? S? DI?N TÍCH
    // =========================================================================
    private const int AREA_MULTIPLIER = 10;

    // Maximum total durian area (maxHarvest × AREA_MULTIPLIER = 15 × 10 = 150).
    // Di?n tích t?i da c?a s?u riêng (maxHarvest × AREA_MULTIPLIER = 15 × 10 = 150).
    private const int DURIAN_MAX_AREA = 150;

    // Maximum total rice area (maxHarvest × AREA_MULTIPLIER = 25 × 10 = 250).
    // Di?n tích t?i da c?a lúa (maxHarvest × AREA_MULTIPLIER = 25 × 10 = 250).
    private const int RICE_MAX_AREA = 250;

    // =========================================================================
    // INTERNAL STATE
    // TR?NG THÁI N?I B?
    // =========================================================================
    private readonly List<GameObject> _createdObjects = new();

    // =========================================================================
    // PHASE LABELS
    // NHÃN GIAI ÐO?N
    // =========================================================================
    private static readonly string[] PhaseHeaders = { "T11–T1", "T2–T3", "T4" };

    // =========================================================================
    // OnEnable - Subscribe to data change events.
    // OnEnable - Ðang ký s? ki?n thay d?i d? li?u.
    // =========================================================================
    private void OnEnable()
    {
        var summary = Thuan_23127_SeasonalSummary.Instance;
        if (summary != null)
        {
            summary.OnChanged += Rebuild;
        }
        Rebuild();
    }

    // =========================================================================
    // Freeze - Prevents or allows UI rebuilding.
    // Freeze - Ngan c?n ho?c cho phép rebuild UI.
    // =========================================================================
    public void Freeze(bool v) { /* reserved for future use */ }

    // =========================================================================
    // OnDisable - Unsubscribe from events.
    // OnDisable - H?y dang ký kh?i events.
    // =========================================================================
    private void OnDisable()
    {
        var summary = Thuan_23127_SeasonalSummary.Instance;
        if (summary != null)
        {
            summary.OnChanged -= Rebuild;
        }
    }

    // =========================================================================
    // Rebuild - Clears and rebuilds the entire Excel-style table.
    // Rebuild - Xóa và xây d?ng l?i toàn b? b?ng d?ng Excel.
    // =========================================================================
    private void Rebuild()
    {
        // Allow rebuild when game ends (for final summary display).
        // Cho phép rebuild khi game k?t thúc (d? hi?n th? t?ng k?t).
        if (!content)
        {
            Debug.LogWarning("[TotalBoard] Rebuild() skipped: content is null!");
            return;
        }

        Debug.Log($"[TotalBoard] Rebuild() STARTED. content={content.name}, GameActive={GameRulesProvider.GameActive}");

        // Clear all previously created objects.
        // Xóa t?t c? objects dã t?o tru?c dó.
        foreach (var obj in _createdObjects)
            if (obj) Destroy(obj);
        _createdObjects.Clear();

        // Also clear any legacy UI_ProductRow children.
        // Xóa c? các UI_ProductRow con cu.
        var legacyRows = content.GetComponentsInChildren<UI_ProductRow>(true);
        for (int i = legacyRows.Length - 1; i >= 0; i--)
            if (legacyRows[i]) Destroy(legacyRows[i].gameObject);

        // Ensure font is available.
        // Ð?m b?o font kh? d?ng.
        if (tableFont == null)
            tableFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Ensure content has VerticalLayoutGroup for table layout.
        // Ð?m b?o content có VerticalLayoutGroup cho layout b?ng.
        EnsureVerticalLayout();

        // Get 3-phase data.
        // L?y d? li?u 3 giai do?n.
        var summary = Thuan_23127_SeasonalSummary.Instance;
        Debug.Log($"[TotalBoard] SeasonalSummary.Instance = {(summary != null ? "EXISTS" : "NULL")}");

        var data = summary
            ? summary.GetAllPhaseData()
            : new List<(string, Sprite, int[], int[])>();

        Debug.Log($"[TotalBoard] Data rows count = {data.Count}");

        // -- Build header row --
        // -- Xây d?ng hàng tiêu d? --
        BuildHeaderRow();

        // -- Build data rows --
        // -- Xây d?ng các hàng d? li?u --
        int rowIdx = 0;
        foreach (var (key, icon, scores, counts) in data)
        {
            Debug.Log($"[TotalBoard] Row {rowIdx}: key={key}, icon={(icon != null ? icon.name : "null")}, " +
                      $"scores=[{scores[0]},{scores[1]},{scores[2]}], " +
                      $"counts=[{counts[0]},{counts[1]},{counts[2]}]");
            BuildDataRow(key, icon, scores, counts, rowIdx);
            rowIdx++;
        }

        Debug.Log($"[TotalBoard] Rebuild() COMPLETED. Total created objects = {_createdObjects.Count}");
    }

    // =========================================================================
    // EnsureVerticalLayout - Adds VerticalLayoutGroup if missing.
    // EnsureVerticalLayout - Thêm VerticalLayoutGroup n?u chua có.
    // =========================================================================
    private void EnsureVerticalLayout()
    {
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.spacing = 0;
        vlg.padding = new RectOffset(5, 5, 5, 5);

        var csf = content.GetComponent<ContentSizeFitter>();
        if (csf == null)
        {
            csf = content.gameObject.AddComponent<ContentSizeFitter>();
        }
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // =========================================================================
    // BuildHeaderRow - Creates the header: [ ] | T11–T1 | T2–T3 | T4
    // BuildHeaderRow - T?o hàng tiêu d?: [ ] | T11–T1 | T2–T3 | T4
    // =========================================================================
    private void BuildHeaderRow()
    {
        var row = CreateRowContainer("Header", headerRowHeight, headerBgColor);

        // Empty first cell (icon column).
        // Ô tr?ng d?u tiên (c?t icon).
        CreateTextCell(row.transform, "", headerFontSize, headerTextColor, 0.15f);

        // Phase header cells.
        // Các ô tiêu d? giai do?n.
        for (int i = 0; i < 3; i++)
        {
            CreateTextCell(row.transform, PhaseHeaders[i], headerFontSize,
                           headerTextColor, 0.283f, FontStyle.Bold);
        }
    }

    // =========================================================================
    // BuildDataRow - Creates one product row with icon + 3 phase cells.
    // BuildDataRow - T?o m?t hàng s?n ph?m v?i icon + 3 ô giai do?n.
    //
    // Each phase cell shows:
    //   Di?n tích: {count × 10}
    //   S?n lu?ng: {score}
    //
    // SPECIAL: Durian Phase 3 (T4) shows:
    //   S?n lu?ng: 0
    //   Di?n tích m?t: 150 - t?ng di?n tích dã thu ho?ch (d?i di?n vùng m?t tr?ng)
    //
    // SPECIAL: Rice Phase 3 (T4) shows:
    //   S?n lu?ng: 0
    //   Di?n tích m?t: 250 - t?ng di?n tích dã thu ho?ch
    // =========================================================================
    private void BuildDataRow(string key, Sprite icon, int[] scores, int[] counts, int rowIdx)
    {
        Color bgColor = (rowIdx % 2 == 0) ? evenRowColor : oddRowColor;
        var row = CreateRowContainer($"Row_{rowIdx}", dataRowHeight, bgColor);

        // Icon cell (first column).
        // Ô icon (c?t d?u tiên).
        CreateIconCell(row.transform, icon, 0.15f);

        // Check if this row is Durian, Rice, or Shrimp.
        // Ki?m tra hàng này có ph?i S?u riêng, Lúa, ho?c Tôm không.
        bool isDurian = key.Contains("Durian") || key == "Plant:1";
        bool isRice   = key.Contains("Rice")   || key == "Plant:11";
        bool isShrimp = key.Contains("Shrimp");

        // Total harvested area across phase 1 + phase 2 (for phase 3 lost-area calc).
        // T?ng di?n tích thu ho?ch du?c qua giai do?n 1 + 2 (cho tính DT m?t tr?ng GÐ3).
        int totalHarvestedArea = (counts[0] + counts[1]) * AREA_MULTIPLIER;

        // 3 phase cells with area + score.
        // 3 ô giai do?n v?i di?n tích + s?n lu?ng.
        for (int i = 0; i < 3; i++)
        {
            string cellText;

            if (isDurian && i == 2)
            {
                // DURIAN PHASE 3 (T4): SL = 0, DTMT = 150 - harvested.
                // S?U RIÊNG GÐ3 (T4): SL = 0, DTMT (di?n tích m?t tr?ng).
                int lostArea = DURIAN_MAX_AREA - totalHarvestedArea;
                if (lostArea < 0) lostArea = 0;
                cellText = $"DTMT: {lostArea,4}\nSL:   {0,4}";
            }
            else if (isRice && i == 2)
            {
                // RICE PHASE 3 (T4): SL = 0, DTMT = 250 - harvested.
                // LÚA GÐ3 (T4): SL = 0, DTMT (di?n tích m?t tr?ng).
                int lostArea = RICE_MAX_AREA - totalHarvestedArea;
                if (lostArea < 0) lostArea = 0;
                cellText = $"DTMT: {lostArea,4}\nSL:   {0,4}";
            }
            else if (isShrimp && i == 2)
            {
                // SHRIMP PHASE 3 (T4): DTMT = 0, SL = accumulated score.
                // TÔM GÐ3 (T4): DTMT (di?n tích m?t tr?ng) = 0, SL = di?m tích luy.
                cellText = $"DTMT: {0,4}\nSL:   {scores[i],4}";
            }
            else if (i == 2)
            {
                // OTHER PRODUCTS PHASE 3: also show DTMT label.
                // S?N PH?M KHÁC GÐ3: cung hi?n th? DTMT.
                int area = counts[i] * AREA_MULTIPLIER;
                cellText = $"DTMT: {area,4}\nSL:   {scores[i],4}";
            }
            else
            {
                int area = counts[i] * AREA_MULTIPLIER;
                cellText = $"DT:   {area,4}\nSL:   {scores[i],4}";
            }

            CreateTextCell(row.transform, cellText, bodyFontSize,
                           bodyTextColor, 0.283f, FontStyle.Normal,
                           TextAnchor.MiddleLeft);
        }
    }

    // =========================================================================
    // CreateRowContainer - Creates a horizontal layout row with background.
    // CreateRowContainer - T?o hàng ngang v?i n?n.
    // =========================================================================
    private GameObject CreateRowContainer(string name, float height, Color bgColor)
    {
        var row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(content, false);
        _createdObjects.Add(row);

        // Background image.
        // Hình n?n.
        var bg = row.AddComponent<Image>();
        bg.color = bgColor;

        // Horizontal layout.
        // Layout ngang.
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.spacing = 2;
        hlg.padding = new RectOffset(4, 4, 2, 2);

        // Fixed height.
        // Chi?u cao c? d?nh.
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;

        return row;
    }

    // =========================================================================
    // CreateTextCell - Creates a text cell within a row.
    // CreateTextCell - T?o ô text trong m?t hàng.
    //
    // flexibleWidth: proportional width (e.g., 0.2 = 20% of row).
    // flexibleWidth: chi?u r?ng t? l? (ví d?: 0.2 = 20% hàng).
    // =========================================================================
    private GameObject CreateTextCell(Transform parent, string text, int size,
                                      Color color, float flexibleWidth,
                                      FontStyle style = FontStyle.Normal,
                                      TextAnchor align = TextAnchor.MiddleCenter)
    {
        var cell = new GameObject("Cell", typeof(RectTransform));
        cell.transform.SetParent(parent, false);

        // Cell background (for grid lines).
        // N?n ô (cho du?ng k? lu?i).
        var cellBg = cell.AddComponent<Image>();
        cellBg.color = new Color(0, 0, 0, 0); // transparent

        // Layout sizing.
        // Kích thu?c layout.
        var le = cell.AddComponent<LayoutElement>();
        le.flexibleWidth = flexibleWidth;

        // Add outline for readability (on cell Image).
        // Thêm outline d? d? d?c (trên Image ô).
        var outline = cell.AddComponent<Outline>();
        outline.effectColor = gridLineColor;
        outline.effectDistance = new Vector2(0.5f, 0.5f);

        // Text on a child object (Unity only allows one Graphic per GameObject).
        // Text trên object con (Unity ch? cho phép 1 Graphic m?i GameObject).
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(cell.transform, false);

        // Stretch text to fill entire cell.
        // Kéo giãn text d? l?p d?y toàn b? ô.
        var textRT = textGo.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // Text component.
        // Component text.
        var txt = textGo.AddComponent<Text>();
        txt.text = text;
        txt.font = tableFont;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = align;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.resizeTextForBestFit = false;

        return cell;
    }

    // =========================================================================
    // CreateIconCell - Creates an icon cell with the product sprite.
    // CreateIconCell - T?o ô icon v?i sprite s?n ph?m.
    // =========================================================================
    private GameObject CreateIconCell(Transform parent, Sprite sprite, float flexibleWidth)
    {
        var cell = new GameObject("IconCell", typeof(RectTransform));
        cell.transform.SetParent(parent, false);

        // Layout sizing — MUST set preferredWidth = 0 so the Image's native
        // sprite size does not influence HorizontalLayoutGroup column widths.
        // Without this, sprites with different native sizes (e.g., shrimp 200px
        // vs durian 64px) cause the icon cell to be wider/narrower per row,
        // shifting all data columns out of alignment.
        // Layout sizing — PH?I set preferredWidth = 0 d? kích thu?c g?c c?a
        // sprite không ?nh hu?ng d?n chi?u r?ng c?t trong HorizontalLayoutGroup.
        // N?u không, các sprite có kích thu?c g?c khác nhau (ví d? tôm 200px
        // vs s?u riêng 64px) s? làm ô icon r?ng/h?p khác nhau m?i hàng,
        // khi?n t?t c? c?t d? li?u b? l?ch.
        var le = cell.AddComponent<LayoutElement>();
        le.flexibleWidth = flexibleWidth;
        le.preferredWidth = 0;

        // Icon image.
        // Hình icon.
        var img = cell.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.preserveAspect = true;
        }
        else
        {
            img.color = new Color(0, 0, 0, 0); // transparent if no icon
        }

        return cell;
    }

    // =========================================================================
    // ClearAllRows - Removes all created objects from display.
    // ClearAllRows - Xóa t?t c? objects dã t?o kh?i hi?n th?.
    //
    // Called by: Game restart to clear the summary.
    // Ðu?c g?i b?i: Restart game d? xóa t?ng k?t.
    // =========================================================================
    public void ClearAllRows()
    {
        foreach (var obj in _createdObjects)
            if (obj) Destroy(obj);
        _createdObjects.Clear();

        // Also clear legacy rows.
        if (!content) return;
        var rows = content.GetComponentsInChildren<UI_ProductRow>(true);
        for (int i = rows.Length - 1; i >= 0; i--)
            if (rows[i]) Destroy(rows[i].gameObject);
    }
}