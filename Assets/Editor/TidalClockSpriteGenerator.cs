using UnityEngine;
using UnityEditor;
using System.IO;

// =============================================================================
// TidalClockSpriteGenerator
// Editor tool: tự động tạo tất cả sprite PNG cho Tidal Clock UI.
//
// Menu: Tools → Tidal Clock → Generate Sprites
// Output: Assets/UI/Sources/Sprites/TidalClock/
// =============================================================================
public class TidalClockSpriteGenerator : EditorWindow
{
    private static readonly string OutputFolder = "Assets/UI/Sources/Sprites/TidalClock";

    [MenuItem("Tools/Tidal Clock/Generate All Sprites")]
    public static void GenerateAll()
    {
        if (!Directory.Exists(OutputFolder))
            Directory.CreateDirectory(OutputFolder);

        GenerateClockBackground();
        GenerateEarthIcon();
        GenerateMoonPhaseSprites();
        GeneratePositionMarker();
        GenerateTickMark();
        GenerateIntensityBar();
        GenerateWarningIcon();

        AssetDatabase.Refresh();

        // Import all as Sprite
        ImportAllAsSprites();

        Debug.Log($"[TidalClockSpriteGenerator] ✓ All sprites generated in {OutputFolder}/");
        EditorUtility.DisplayDialog("Tidal Clock Sprites",
            $"All sprites generated successfully!\n\nFolder: {OutputFolder}/\n\n" +
            "All textures imported as Sprite (2D and UI).",
            "OK");
    }

    // =========================================================================
    // 1. ClockBackground — hình tròn 256×256, viền sáng, nền tối bán trong suốt
    // =========================================================================
    private static void GenerateClockBackground()
    {
        int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float outerR = center - 2f;
        float innerR = outerR - 4f;     // viền dày 4px
        float bgR = innerR - 2f;

        Color bgColor = new Color(0.05f, 0.08f, 0.15f, 0.85f);   // xanh đậm bán mờ
        Color rimColor = new Color(0.4f, 0.6f, 0.9f, 1f);         // viền xanh sáng
        Color rimOuter = new Color(0.2f, 0.35f, 0.6f, 0.6f);      // viền ngoài mờ

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                if (dist > outerR)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (dist > innerR)
                {
                    float t = (dist - innerR) / (outerR - innerR);
                    tex.SetPixel(x, y, Color.Lerp(rimColor, rimOuter, t));
                }
                else if (dist > bgR)
                {
                    float t = (dist - bgR) / (innerR - bgR);
                    tex.SetPixel(x, y, Color.Lerp(bgColor, rimColor, t * 0.3f));
                }
                else
                {
                    // Subtle radial gradient
                    float t = dist / bgR;
                    Color c = Color.Lerp(
                        new Color(0.08f, 0.12f, 0.22f, 0.9f),
                        bgColor,
                        t
                    );
                    tex.SetPixel(x, y, c);
                }
            }
        }

        // Draw subtle grid lines (cross through center)
        DrawLineH(tex, size / 2, 0, size, new Color(0.3f, 0.5f, 0.7f, 0.15f), center, outerR - 6f);
        DrawLineV(tex, size / 2, 0, size, new Color(0.3f, 0.5f, 0.7f, 0.15f), center, outerR - 6f);

        tex.Apply();
        SavePNG(tex, "ClockBackground");
        Object.DestroyImmediate(tex);
    }

    // =========================================================================
    // 2. EarthIcon — hình tròn xanh lam + vệt xanh lá (Trái Đất cartoon)
    //    64×64
    // =========================================================================
    private static void GenerateEarthIcon()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float radius = center - 2f;

        Color ocean = new Color(0.15f, 0.35f, 0.75f, 1f);
        Color land = new Color(0.2f, 0.6f, 0.3f, 1f);
        Color ice = new Color(0.85f, 0.92f, 0.97f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist > radius)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float nx = (x - center) / radius;
                float ny = (y - center) / radius;

                // Simple continent shapes using noise-like patterns
                float pattern = Mathf.Sin(nx * 5f + 1.2f) * Mathf.Cos(ny * 4f + 0.8f) +
                                Mathf.Sin(nx * 3f - ny * 2f) * 0.5f;

                Color c;
                if (Mathf.Abs(ny) > 0.8f)
                {
                    c = ice; // polar caps
                }
                else if (pattern > 0.2f)
                {
                    c = land;
                }
                else
                {
                    c = ocean;
                }

                // Edge shading (pseudo 3D)
                float shade = 1f - dist / radius * 0.3f;
                float highlight = Mathf.Max(0, 1f - Vector2.Distance(
                    new Vector2(nx, ny), new Vector2(-0.3f, 0.3f)) * 1.5f) * 0.25f;
                c = new Color(
                    Mathf.Clamp01(c.r * shade + highlight),
                    Mathf.Clamp01(c.g * shade + highlight),
                    Mathf.Clamp01(c.b * shade + highlight),
                    1f
                );

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        SavePNG(tex, "EarthIcon");
        Object.DestroyImmediate(tex);
    }

    // =========================================================================
    // 3. Moon Phase Sprites (4 cái)
    //    - MoonPhase_NewMoon      (đen gần hoàn toàn, viền mờ)
    //    - MoonPhase_FirstQuarter  (nửa phải sáng)
    //    - MoonPhase_FullMoon      (tròn sáng)
    //    - MoonPhase_LastQuarter   (nửa trái sáng)
    //    48×48 mỗi cái
    // =========================================================================
    private static void GenerateMoonPhaseSprites()
    {
        GenerateMoonSprite("MoonPhase_NewMoon", MoonPhaseType.NewMoon);
        GenerateMoonSprite("MoonPhase_FirstQuarter", MoonPhaseType.FirstQuarter);
        GenerateMoonSprite("MoonPhase_FullMoon", MoonPhaseType.FullMoon);
        GenerateMoonSprite("MoonPhase_LastQuarter", MoonPhaseType.LastQuarter);
    }

    private enum MoonPhaseType { NewMoon, FirstQuarter, FullMoon, LastQuarter }

    private static void GenerateMoonSprite(string name, MoonPhaseType phase)
    {
        int size = 48;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float radius = center - 2f;

        Color bright = new Color(0.95f, 0.93f, 0.8f, 1f);   // ánh trăng vàng nhạt
        Color dark = new Color(0.12f, 0.12f, 0.15f, 1f);     // mặt tối
        Color rim = new Color(0.4f, 0.4f, 0.5f, 0.6f);       // viền mờ nhẹ

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist > radius + 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                if (dist > radius)
                {
                    tex.SetPixel(x, y, new Color(rim.r, rim.g, rim.b, 0.3f));
                    continue;
                }

                float nx = (x - center) / radius; // -1 to 1

                float illumination; // 0 = dark, 1 = bright
                switch (phase)
                {
                    case MoonPhaseType.NewMoon:
                        illumination = 0.08f; // almost all dark, faint edge glow
                        if (dist > radius - 3f) illumination = 0.2f;
                        break;
                    case MoonPhaseType.FirstQuarter:
                        illumination = nx > 0 ? 1f : Mathf.Clamp01(nx + 0.15f) * 0.3f;
                        break;
                    case MoonPhaseType.FullMoon:
                        illumination = 1f;
                        break;
                    case MoonPhaseType.LastQuarter:
                        illumination = nx < 0 ? 1f : Mathf.Clamp01(-nx + 0.15f) * 0.3f;
                        break;
                    default:
                        illumination = 0.5f;
                        break;
                }

                // Add crater-like texture
                float crater = Mathf.Sin(x * 0.8f) * Mathf.Cos(y * 0.9f) * 0.08f +
                               Mathf.Sin(x * 1.5f + y * 1.3f) * 0.04f;

                Color c = Color.Lerp(dark, bright, illumination);
                c = new Color(
                    Mathf.Clamp01(c.r + crater),
                    Mathf.Clamp01(c.g + crater),
                    Mathf.Clamp01(c.b + crater * 0.5f),
                    1f
                );

                // Soften edge
                if (dist > radius - 2f)
                {
                    float edgeT = (dist - (radius - 2f)) / 2f;
                    c.a = Mathf.Lerp(1f, 0.5f, edgeT);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        SavePNG(tex, name);
        Object.DestroyImmediate(tex);
    }

    // =========================================================================
    // 4. PositionMarker — chấm tròn nhỏ sáng 24×24, glow effect
    // =========================================================================
    private static void GeneratePositionMarker()
    {
        int size = 24;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float innerR = 4f;
        float outerR = center - 1f;

        Color core = new Color(0.9f, 0.95f, 1f, 1f);
        Color glow = new Color(0.4f, 0.6f, 0.9f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist > outerR)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else if (dist < innerR)
                {
                    tex.SetPixel(x, y, core);
                }
                else
                {
                    float t = (dist - innerR) / (outerR - innerR);
                    tex.SetPixel(x, y, Color.Lerp(core, glow, t));
                }
            }
        }

        tex.Apply();
        SavePNG(tex, "PositionMarker");
        Object.DestroyImmediate(tex);
    }

    // =========================================================================
    // 5. TickMark — chấm tròn rất nhỏ 12×12 trắng mờ
    // =========================================================================
    private static void GenerateTickMark()
    {
        int size = 12;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist > radius)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float t = dist / radius;
                    tex.SetPixel(x, y, new Color(0.8f, 0.85f, 0.9f, Mathf.Lerp(0.7f, 0f, t)));
                }
            }
        }

        tex.Apply();
        SavePNG(tex, "TickMark");
        Object.DestroyImmediate(tex);
    }

    // =========================================================================
    // 6. IntensityBar — thanh ngang gradient 256×32 (xanh→vàng→đỏ)
    //    Dùng làm fill image (Filled, Horizontal)
    // =========================================================================
    private static void GenerateIntensityBar()
    {
        int w = 256, h = 32;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        Color low = new Color(0.2f, 0.5f, 0.9f, 1f);     // xanh dương (triều kém)
        Color mid = new Color(0.9f, 0.8f, 0.2f, 1f);     // vàng
        Color high = new Color(0.9f, 0.2f, 0.15f, 1f);   // đỏ (triều cường)
        float cornerR = 8f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Rounded rectangle mask
                if (!InsideRoundedRect(x, y, w, h, cornerR))
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float t = (float)x / (w - 1);
                Color c;
                if (t < 0.5f)
                    c = Color.Lerp(low, mid, t * 2f);
                else
                    c = Color.Lerp(mid, high, (t - 0.5f) * 2f);

                // Subtle vertical gradient (bevel)
                float vy = (float)y / (h - 1);
                float bevel = 1f - Mathf.Abs(vy - 0.5f) * 0.3f;
                c *= bevel;
                c.a = 1f;

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        SavePNG(tex, "IntensityBar");
        Object.DestroyImmediate(tex);
    }

    // =========================================================================
    // 7. WarningIcon — tam giác cảnh báo đỏ 64×64 với dấu chấm than
    // =========================================================================
    private static void GenerateWarningIcon()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;

        // Clear
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.clear);

        // Draw circle background (red glow)
        float outerR = center - 2f;
        Color redGlow = new Color(0.9f, 0.15f, 0.1f, 0.85f);
        Color redCore = new Color(1f, 0.25f, 0.15f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist > outerR) continue;

                float t = dist / outerR;
                Color c = Color.Lerp(redCore, redGlow, t * t);

                // Pulse-like inner highlight
                if (dist < outerR * 0.4f)
                {
                    float ht = 1f - dist / (outerR * 0.4f);
                    c = Color.Lerp(c, new Color(1f, 0.6f, 0.4f, 1f), ht * 0.4f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        // Draw exclamation mark (!)
        Color white = new Color(1f, 1f, 1f, 1f);

        // Vertical bar of !
        int barLeft = (int)(center - 2f);
        int barRight = (int)(center + 2f);
        int barTop = (int)(center + 14f);
        int barBottom = (int)(center - 2f);
        for (int y = barBottom; y <= barTop; y++)
        {
            for (int x = barLeft; x <= barRight; x++)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                    tex.SetPixel(x, y, white);
            }
        }

        // Dot of !
        int dotCenterY = (int)(center - 7f);
        for (int y = dotCenterY - 2; y <= dotCenterY + 2; y++)
        {
            for (int x = barLeft; x <= barRight; x++)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, dotCenterY));
                    if (dist <= 3f) tex.SetPixel(x, y, white);
                }
            }
        }

        tex.Apply();
        SavePNG(tex, "WarningIcon");
        Object.DestroyImmediate(tex);
    }

    // =========================================================================
    // UTILITY
    // =========================================================================

    private static void SavePNG(Texture2D tex, string name)
    {
        byte[] bytes = tex.EncodeToPNG();
        string path = $"{OutputFolder}/{name}.png";
        File.WriteAllBytes(path, bytes);
        Debug.Log($"  → Saved: {path} ({tex.width}×{tex.height})");
    }

    private static void ImportAllAsSprites()
    {
        string[] pngs = Directory.GetFiles(OutputFolder, "*.png");
        foreach (string file in pngs)
        {
            string assetPath = file.Replace("\\", "/");
            // Ensure path starts from Assets/
            if (!assetPath.StartsWith("Assets/"))
            {
                int idx = assetPath.IndexOf("Assets/");
                if (idx >= 0) assetPath = assetPath.Substring(idx);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }
    }

    private static void DrawLineH(Texture2D tex, int y, int xStart, int xEnd, Color c, float center, float maxDist)
    {
        for (int x = xStart; x < xEnd; x++)
        {
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
            if (dist < maxDist)
            {
                Color existing = tex.GetPixel(x, y);
                tex.SetPixel(x, y, Color.Lerp(existing, c, c.a));
            }
        }
    }

    private static void DrawLineV(Texture2D tex, int x, int yStart, int yEnd, Color c, float center, float maxDist)
    {
        for (int y = yStart; y < yEnd; y++)
        {
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
            if (dist < maxDist)
            {
                Color existing = tex.GetPixel(x, y);
                tex.SetPixel(x, y, Color.Lerp(existing, c, c.a));
            }
        }
    }

    private static bool InsideRoundedRect(int x, int y, int w, int h, float r)
    {
        // Check four corners
        if (x < r && y < r)
            return Vector2.Distance(new Vector2(x, y), new Vector2(r, r)) <= r;
        if (x >= w - r && y < r)
            return Vector2.Distance(new Vector2(x, y), new Vector2(w - r - 1, r)) <= r;
        if (x < r && y >= h - r)
            return Vector2.Distance(new Vector2(x, y), new Vector2(r, h - r - 1)) <= r;
        if (x >= w - r && y >= h - r)
            return Vector2.Distance(new Vector2(x, y), new Vector2(w - r - 1, h - r - 1)) <= r;
        return true;
    }
}
