// =============================================================================
// BeeHudSprites - Sinh sprite cho HUD ong BANG CODE (khong can file art).
// BeeHudSprites - Sinh sprite cho HUD ong bằng code (không cần file art).
//
// VI SAO
//   HUD trong anh mau (binh mat, la ban, thanh mat hoa, tay cam...) can vai
//   hinh co ban: chu nhat bo goc, hinh tron, vanh tron, luc giac, tam giac.
//   Thay vi doi art asset, ta ve bang Texture2D luc chay -> vai texture
//   96-160px, chi vai KB VRAM, khong anh huong hieu nang Quest.
//
// CACHE / LƯU ĐỆM
//   Sprite duoc cache theo key nen moi hinh chi tao MOT lan cho ca scene.
//   Goi ClearCache() khi can giai phong (thuong khong can).
// =============================================================================
using System.Collections.Generic;
using UnityEngine;

public static class BeeHudSprites
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(16);

    // =========================================================================
    // SHAPES / HINH CO BAN
    // =========================================================================

    /// <summary>
    /// Chu nhat bo goc. Dung voi Image.type = Sliced nen kho bo goc khi keo
    /// gian thanh panel dai (neu de Simple thi 4 goc bi meo).
    /// </summary>
    public static Sprite RoundedRect(int size = 96, int radius = 24)
    {
        string key = "rr_" + size + "_" + radius;
        if (Cache.TryGetValue(key, out Sprite cached)) return cached;

        float r = Mathf.Clamp(radius, 1f, size * 0.5f - 1f);
        Texture2D tex = NewTexture(size);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float ax = Mathf.Min(x + 0.5f, size - 0.5f - x);
                float ay = Mathf.Min(y + 0.5f, size - 0.5f - y);
                float dx = Mathf.Max(0f, r - ax);
                float dy = Mathf.Max(0f, r - ay);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, AlphaToPixel(Mathf.Clamp01(r - dist + 0.5f)));
            }
        }

        tex.Apply(false, false);
        var border = new Vector4(r, r, r, r);
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, border);
        Cache[key] = sprite;
        return sprite;
    }

    /// <summary>Hinh tron dac (dung cho nut "!", icon nho).</summary>
    public static Sprite Circle(int size = 96)
    {
        string key = "c_" + size;
        if (Cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = NewTexture(size);
        float radius = size * 0.5f - 1f;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                tex.SetPixel(x, y, AlphaToPixel(Mathf.Clamp01(radius - dist + 0.5f)));
            }
        }

        tex.Apply(false, false);
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), size);
        Cache[key] = sprite;
        return sprite;
    }

    /// <summary>Vanh tron rong (la ban trong anh mau).</summary>
    public static Sprite Ring(int size = 160, int thickness = 6)
    {
        string key = "ring_" + size + "_" + thickness;
        if (Cache.TryGetValue(key, out Sprite cached)) return cached;

        Texture2D tex = NewTexture(size);
        float radius = size * 0.5f - thickness * 0.5f - 1f;
        float half = thickness * 0.5f;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                tex.SetPixel(x, y, AlphaToPixel(Mathf.Clamp01(half - Mathf.Abs(dist - radius) + 0.5f)));
            }
        }

        tex.Apply(false, false);
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), size);
        Cache[key] = sprite;
        return sprite;
    }

    /// <summary>Luc giac deu (icon mat ong / mat hoa trong anh mau).</summary>
    public static Sprite Hexagon(int size = 96)
    {
        string key = "hex_" + size;
        if (Cache.TryGetValue(key, out Sprite cached)) return cached;

        // Luc giac "pointy-top": 6 dinh, ban kinh ngoai = radius.
        float radius = size * 0.5f - 1f;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        var points = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (90f + i * 60f);
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        Texture2D tex = NewTexture(size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = ConvexAlpha(points, new Vector2(x + 0.5f, y + 0.5f) - center);
                tex.SetPixel(x, y, AlphaToPixel(alpha));
            }
        }

        tex.Apply(false, false);
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), size);
        Cache[key] = sprite;
        return sprite;
    }

    /// <summary>Tam giac day ngang, dinh huong LEN (mui ten / chevron).</summary>
    public static Sprite TriangleUp(int size = 96)
    {
        string key = "tri_" + size;
        if (Cache.TryGetValue(key, out Sprite cached)) return cached;

        float half = size * 0.5f - 1f;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        var points = new[]
        {
            new Vector2(0f, half),       // dinh
            new Vector2(-half, -half),   // goc trai
            new Vector2(half, -half)     // goc phai
        };

        Texture2D tex = NewTexture(size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = ConvexAlpha(points, new Vector2(x + 0.5f, y + 0.5f) - center);
                tex.SetPixel(x, y, AlphaToPixel(alpha));
            }
        }

        tex.Apply(false, false);
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), size);
        Cache[key] = sprite;
        return sprite;
    }

    // =========================================================================
    // HELPERS / HAM PHU
    // =========================================================================

    /// <summary>
    /// Alpha cua da giac LOI (thu tu dinh nguoc chieu kim dong ho).
    /// Nguyen ly: khoang cach lon nhat toi cac canh, lam muot 1 pixel.
    /// </summary>
    private static float ConvexAlpha(Vector2[] points, Vector2 p)
    {
        float maxDistance = float.NegativeInfinity;

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % points.Length];
            Vector2 dir = (b - a).normalized;
            // Phap tuyen ngoai cua canh a->b voi da giac CCW.
            Vector2 normal = new Vector2(dir.y, -dir.x);
            float d = Vector2.Dot(p - a, normal);
            if (d > maxDistance) maxDistance = d;
        }

        return Mathf.Clamp01(0.5f - maxDistance);
    }

    private static Texture2D NewTexture(int size)
    {
        return new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    private static Color AlphaToPixel(float alpha)
    {
        return new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
    }

    /// <summary>Giai phong toan bo texture da sinh (dung khi doi scene/game).</summary>
    public static void ClearCache()
    {
        foreach (Sprite sprite in Cache.Values)
        {
            if (sprite == null) continue;

            DestroyObject(sprite.texture);
            DestroyObject(sprite);
        }
        Cache.Clear();
    }

    /// <summary>Destroy dung cach: Play mode dung Destroy, Edit mode dung DestroyImmediate.</summary>
    private static void DestroyObject(Object target)
    {
        if (target == null) return;

        if (Application.isPlaying) Object.Destroy(target);
        else Object.DestroyImmediate(target);
    }
}
