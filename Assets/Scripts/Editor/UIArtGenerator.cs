using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Draws the glossy buttons, panels and shape icons used by the menu screens and saves them as PNG sprites
// in Assets/UI/Generated. Glossy sprites are drawn in their final colour (tinting a grey sprite makes it muddy),
// so each shape + colour combination becomes its own small PNG, e.g. "Pill_6BF23F.png".
public static class UIArtGenerator
{
    public const string Folder = "Assets/UI/Generated";

    // Glossy shapes. Get them with UIArtGenerator.Glossy(shape, colour).
    public const string Pill = "Pill";
    public const string Panel = "Panel";
    public const string Disc = "Disc";
    public const string Star = "Star";
    public const string Heart = "Heart";
    public const string Triangle = "Triangle";
    public const string Square = "Square";
    public const string Ball = "Ball";
    public const string Diamond = "Diamond";
    public const string Crown = "Crown";
    public const string Arrow = "Arrow";
    public const string Trophy = "Trophy";

    // Plain sprites. Get them with UIArtGenerator.Plain(name).
    public const string Card = "Card";
    public const string Board = "Board";
    public const string Cell = "Cell";
    public const string SelectRing = "SelectRing";
    public const string Circle = "Circle";
    public const string Cursor = "Cursor";

    const int IconSize = 256;

    static readonly Dictionary<string, Func<Vector2, float>> IconShapes = new()
    {
        [Disc] = p => Sdf.Circle(p, 0.94f),
        [Star] = p => Sdf.Polygon(p - new Vector2(0, -0.04f), StarPoints(0.9f, 0.42f)) - 0.05f,
        [Heart] = p => Sdf.Heart((p + new Vector2(0, 0.86f)) / 1.55f) * 1.55f,
        [Triangle] = p => Sdf.Polygon(p, new[] { new Vector2(0, 0.7f), new Vector2(0.78f, -0.62f), new Vector2(-0.78f, -0.62f) }) - 0.12f,
        [Square] = p => Sdf.RoundBox(p, new Vector2(0.8f, 0.8f), 0.24f),
        [Ball] = p => Sdf.Circle(p, 0.86f),
        [Diamond] = p => Sdf.Polygon(p, new[] { new Vector2(0, 0.82f), new Vector2(0.72f, 0), new Vector2(0, -0.82f), new Vector2(-0.72f, 0) }) - 0.08f,
        [Crown] = p => Sdf.Polygon(p, new[]
        {
            new Vector2(-0.82f, -0.62f), new Vector2(0.82f, -0.62f), new Vector2(0.88f, 0.5f), new Vector2(0.42f, 0.02f),
            new Vector2(0, 0.72f), new Vector2(-0.42f, 0.02f), new Vector2(-0.88f, 0.5f)
        }) - 0.06f,
        [Arrow] = p => Sdf.Polygon(p, new[]
        {
            new Vector2(-0.82f, 0), new Vector2(-0.08f, 0.68f), new Vector2(-0.08f, 0.28f), new Vector2(0.78f, 0.28f),
            new Vector2(0.78f, -0.28f), new Vector2(-0.08f, -0.28f), new Vector2(-0.08f, -0.68f)
        }) - 0.08f,
        [Trophy] = TrophySdf,
    };

    // Sprites drawn during this build. Files are overwritten in place so their .meta GUIDs stay stable.
    static readonly HashSet<string> DrawnThisBuild = new();

    // Starts a build: redraws the plain sprites. Glossy ones are drawn on first request.
    public static void Reset()
    {
        DrawnThisBuild.Clear();
        Directory.CreateDirectory(Folder);
        AssetDatabase.ImportAsset(Folder);

        SaveCard();
        SaveBoard();
        SaveCell();
        SaveSelectRing();
        SaveCursor();
        Save(Circle, 256, 256, Vector4.zero, p => WithAlpha(Color.white, Coverage(Sdf.Circle(p - new Vector2(128, 128), 124))));
    }

    public static Sprite Plain(string name) =>
        Borders.ContainsKey(name) ? Import(name) : AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");

    public static Sprite Glossy(string shape, Color color)
    {
        var name = $"{shape}_{ColorUtility.ToHtmlStringRGB(color)}";
        if (DrawnThisBuild.Contains(name))
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");

        // Buttons and panels are 9-sliced so they stretch without distorting their rounded corners.
        if (shape == Pill)
            SaveGlossyBox(name, color, 256, 128, 64, 7, 9, 64);
        else if (shape == Panel)
            SaveGlossyBox(name, color, 256, 256, 56, 8, 11, 64);
        else
            SaveGlossyIcon(name, color, IconShapes[shape]);
        return Import(name);
    }

    // ---------- Sprites ----------

    // Rounded box with a darker rim, a deeper "lip" along the bottom for depth, and a shine across the top.
    static void SaveGlossyBox(string name, Color color, int w, int h, float radius, float rim, float lip, float border)
    {
        var half = new Vector2(w / 2f - 2, h / 2f - 2);
        var center = new Vector2(w / 2f, h / 2f);
        Save(name, w, h, new Vector4(border, border, border, border), p =>
        {
            var q = p - center;
            float d = Sdf.RoundBox(q, half, radius);
            float inner = Sdf.RoundBox(q - new Vector2(0, lip * 0.6f), half - new Vector2(rim, rim + lip * 0.6f), radius - rim);
            float v = Mathf.InverseLerp(-half.y, half.y, q.y);
            return GlossyShade(color, d, inner, v);
        });
    }

    static void SaveGlossyIcon(string name, Color color, Func<Vector2, float> unitSdf)
    {
        const float scale = IconSize / 2f - 4;
        Save(name, IconSize, IconSize, Vector4.zero, p =>
        {
            var u = (p - new Vector2(IconSize / 2f, IconSize / 2f)) / scale;
            float d = unitSdf(u) * scale;
            float inner = unitSdf(u - new Vector2(0, 0.035f)) * scale + 7;
            return GlossyShade(color, d, inner, (u.y + 1) / 2);
        });
    }

    // d: distance to the outer edge, inner: distance to the raised inner face, v: 0 at the bottom, 1 at the top.
    static Color GlossyShade(Color color, float d, float inner, float v)
    {
        float alpha = Coverage(d);
        if (alpha <= 0) return Color.clear;

        var c = Color.Lerp(Darken(color, 0.96f), Color.Lerp(color, Color.white, 0.28f), v);
        c = Color.Lerp(c, Darken(color, 0.74f), Mathf.Clamp01(inner + 0.5f));
        float shine = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.58f, 0.92f, v)) * Mathf.Clamp01(-inner / 6f);
        c = Color.Lerp(c, Color.white, shine * 0.55f);
        return WithAlpha(c, alpha);
    }

    // Darker and slightly more saturated, like a shadowed side of the same plastic.
    // Yellows and oranges shift toward red as they darken so they read as gold rather than olive.
    static Color Darken(Color c, float value)
    {
        Color.RGBToHSV(c, out var h, out var s, out var v);
        if (h > 0.06f && h < 0.2f)
            h -= 0.05f * (1 - value) / 0.26f;
        return Color.HSVToRGB(h, Mathf.Min(1, s * 1.12f), v * value);
    }

    static void SaveCard()
    {
        var fill = Hex("FFF7E3");
        var fillBottom = Hex("FCEBC4");
        var rimColor = Hex("F0AF3C");
        Save(Card, 256, 256, new Vector4(64, 64, 64, 64), p =>
        {
            var q = p - new Vector2(128, 128);
            float d = Sdf.RoundBox(q, new Vector2(126, 126), 44);
            var c = Color.Lerp(fillBottom, fill, Mathf.InverseLerp(-126, 126, q.y));
            c = Color.Lerp(c, rimColor, Mathf.Clamp01(d + 8.5f));
            return WithAlpha(c, Coverage(d));
        });
    }

    static void SaveBoard()
    {
        var felt = Hex("1C4630");
        var wood = Hex("A0612A");
        var woodDark = Hex("6E3D17");
        Save(Board, 192, 192, new Vector4(44, 44, 44, 44), p =>
        {
            var q = p - new Vector2(96, 96);
            float d = Sdf.RoundBox(q, new Vector2(94, 94), 28);
            var c = felt;
            c = Color.Lerp(c, woodDark, Mathf.Clamp01(d + 12.5f));
            c = Color.Lerp(c, wood, Mathf.Clamp01(d + 9.5f) * Mathf.Clamp01(-d - 2.5f));
            return WithAlpha(c, Coverage(d));
        });
    }

    static void SaveCell()
    {
        var top = Hex("2E6447");
        var bottom = Hex("24503A");
        Save(Cell, 96, 96, new Vector4(28, 28, 28, 28), p =>
        {
            var q = p - new Vector2(48, 48);
            float d = Sdf.RoundBox(q, new Vector2(46, 46), 16);
            var c = Color.Lerp(bottom, top, Mathf.InverseLerp(-46, 46, q.y));
            return WithAlpha(c, Coverage(d));
        });
    }

    // White outline with a soft glow, tinted to show a selected tile.
    static void SaveSelectRing()
    {
        Save(SelectRing, 128, 128, new Vector4(40, 40, 40, 40), p =>
        {
            var q = p - new Vector2(64, 64);
            float d = Sdf.RoundBox(q, new Vector2(52, 52), 20);
            float ring = Coverage(Mathf.Abs(d) - 4);
            float glow = Mathf.Clamp01(1 - Mathf.Abs(d) / 11f) * 0.55f;
            return WithAlpha(Color.white, Mathf.Max(ring, glow));
        });
    }

    // Classic mouse pointer: white fill, dark outline.
    static void SaveCursor()
    {
        var points = new[]
        {
            new Vector2(-0.55f, 0.92f), new Vector2(-0.55f, -0.5f), new Vector2(-0.22f, -0.2f), new Vector2(0.02f, -0.82f),
            new Vector2(0.24f, -0.72f), new Vector2(0.0f, -0.12f), new Vector2(0.42f, -0.12f)
        };
        var outline = Hex("1B2440");
        Save(Cursor, 128, 128, Vector4.zero, p =>
        {
            var u = (p - new Vector2(64, 64)) / 60f;
            float d = (Sdf.Polygon(u, points) - 0.02f) * 60f;
            var c = Color.Lerp(Color.white, outline, Mathf.Clamp01(d + 7.5f));
            return WithAlpha(c, Coverage(d));
        });
    }

    static float TrophySdf(Vector2 p)
    {
        float cup = Mathf.Min(
            Sdf.RoundBox(p - new Vector2(0, 0.5f), new Vector2(0.56f, 0.3f), 0.06f),
            Sdf.Circle(p - new Vector2(0, 0.24f), 0.56f) + (p.y > 0.4f ? 1 : 0));
        var handleP = new Vector2(Mathf.Abs(p.x), p.y);
        float handle = Mathf.Abs(Sdf.Circle(handleP - new Vector2(0.56f, 0.42f), 0.24f)) - 0.07f;
        handle = Mathf.Max(handle, 0.5f - handleP.x);
        float stem = Sdf.RoundBox(p - new Vector2(0, -0.42f), new Vector2(0.1f, 0.22f), 0.03f);
        float plinth = Sdf.RoundBox(p - new Vector2(0, -0.76f), new Vector2(0.46f, 0.14f), 0.06f);
        return Mathf.Min(Mathf.Min(cup, handle), Mathf.Min(stem, plinth));
    }

    static Vector2[] StarPoints(float outer, float inner)
    {
        var points = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float angle = Mathf.PI / 2 + i * Mathf.PI / 5;
            float r = i % 2 == 0 ? outer : inner;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
        }
        return points;
    }

    // ---------- Helpers ----------

    static readonly Dictionary<string, Vector4> Borders = new();

    static void Save(string name, int w, int h, Vector4 border, Func<Vector2, Color> pixel)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = pixel(new Vector2(x + 0.5f, y + 0.5f));
        tex.SetPixels(pixels);
        File.WriteAllBytes($"{Folder}/{name}.png", tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        Borders[name] = border;
    }

    // Imports a freshly drawn PNG as a sprite with its 9-slice border.
    static Sprite Import(string name)
    {
        var path = $"{Folder}/{name}.png";
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = Borders[name];
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Borders.Remove(name);
        DrawnThisBuild.Add(name);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Anti-aliased coverage for a signed distance in pixels (negative = inside).
    static float Coverage(float d) => Mathf.Clamp01(0.5f - d);

    static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }

    // Signed distance functions (negative inside). Formulas from Inigo Quilez.
    static class Sdf
    {
        public static float Circle(Vector2 p, float r) => p.magnitude - r;

        public static float RoundBox(Vector2 p, Vector2 half, float r)
        {
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + new Vector2(r, r);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
        }

        public static float Polygon(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                var e = v[j] - v[i];
                var w = p - v[i];
                var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        public static float Heart(Vector2 p)
        {
            p.x = Mathf.Abs(p.x);
            if (p.y + p.x > 1)
                return (p - new Vector2(0.25f, 0.75f)).magnitude - Mathf.Sqrt(2) / 4;
            float a = (p - new Vector2(0, 1)).sqrMagnitude;
            float b = (p - 0.5f * Mathf.Max(p.x + p.y, 0) * Vector2.one).sqrMagnitude;
            return Mathf.Sqrt(Mathf.Min(a, b)) * Mathf.Sign(p.x - p.y);
        }
    }
}
