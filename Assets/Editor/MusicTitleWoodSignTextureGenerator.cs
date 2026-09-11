using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Procedurally paints a square wood-plank tile (grain + a crack + two nail/screw
// fasteners) and assigns it to the MusicTitle sign material's _FaceTex. TMP uses
// "Character" UV mapping on the MusicTitle text, so this one tile is applied fresh
// to every glyph individually -- each character reads as its own small board held
// together by screws, rather than one image stretched across the whole title. This
// also means the look does not depend on the title's actual string content/length
// (it will hold up when the text changes, e.g. to "9605年の花園").
public static class MusicTitleWoodSignTextureGenerator
{
    private const string OutputPath = "Assets/Prefabs/Stages/Creation_HIll/Textures/MusicTitleWoodSign.png";
    private const string MaterialPath = "Assets/Prefabs/Stages/Creation_HIll/MPLUS1p-ExtraBold SDF_Hill.mat";

    private const int Width = 512;
    private const int Height = 512;
    private const int Seed = 12345;

    [MenuItem("Tools/Creation Hill/Generate MusicTitle Wood Sign Texture")]
    public static void Generate()
    {
        var rng = new System.Random(Seed);
        var pixels = new Color[Width * Height];

        Color darkGrain = new Color(0.26f, 0.14f, 0.06f);
        Color lightGrain = new Color(0.90f, 0.68f, 0.40f);
        Color crackColor = new Color(0.06f, 0.03f, 0.015f);

        for (int y = 0; y < Height; y++)
        {
            float v = (float)y / Height;
            for (int x = 0; x < Width; x++)
            {
                float u = (float)x / Width;

                float fineGrain = Mathf.PerlinNoise(u * 3f, v * 9f);
                float blotch = Mathf.PerlinNoise(u * 1.2f + 11f, v * 1.6f + 11f);
                float fiber = Mathf.PerlinNoise(u * 10f + 31f, v * 3f + 31f) * 0.15f;
                float grain = Mathf.Clamp01(fineGrain * 0.6f + blotch * 0.35f + fiber + 0.02f);

                Color c = Color.Lerp(darkGrain, lightGrain, grain);

                // darken toward the top/bottom edges, like a rounded plank edge catching less light
                float edge = Mathf.SmoothStep(0f, 0.14f, v) * Mathf.SmoothStep(0f, 0.14f, 1f - v);
                c *= Mathf.Lerp(0.6f, 1f, edge);

                pixels[y * Width + x] = c;
            }
        }

        // One tile == one character, so keep it simple and bold: a single crack running
        // through the glyph and two nails, positioned like the reference photo where two
        // overlapping boards are pinned together near the top and through the middle.
        DrawCrack(pixels, rng, crackColor);

        int nailRadius = Mathf.RoundToInt(Height * 0.15f);
        DrawNail(pixels, Mathf.RoundToInt(Width * 0.5f), Mathf.RoundToInt(Height * 0.26f), nailRadius);
        DrawNail(pixels, Mathf.RoundToInt(Width * 0.5f), Mathf.RoundToInt(Height * 0.68f), nailRadius);

        var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        tex.SetPixels(pixels);
        tex.Apply();

        string dir = Path.GetDirectoryName(OutputPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllBytes(OutputPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(OutputPath);
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();

        var woodTex = AssetDatabase.LoadAssetAtPath<Texture2D>(OutputPath);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            Debug.LogError($"[MusicTitleWoodSignTextureGenerator] Material not found at {MaterialPath}");
            return;
        }

        mat.SetTexture("_FaceTex", woodTex);
        mat.SetTextureScale("_FaceTex", Vector2.one);
        mat.SetTextureOffset("_FaceTex", Vector2.zero);
        // Pure white face color so the texture's own colors/detail show through
        // untinted. Bevel is disabled (its ambient/specular overlay was washing the
        // texture out to near-white) and Underlay uses the plain drop-shadow mode
        // (not "Inner", which punches a dark inset into the glyph and hid the grain).
        mat.SetColor("_FaceColor", Color.white);
        mat.DisableKeyword("BEVEL_ON");
        mat.DisableKeyword("UNDERLAY_INNER");
        mat.DisableKeyword("GLOW_ON");
        mat.EnableKeyword("UNDERLAY_ON");
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        Debug.Log("[MusicTitleWoodSignTextureGenerator] Wood sign texture generated and assigned to " + MaterialPath);
    }

    private static void DrawCrack(Color[] pixels, System.Random rng, Color crackColor)
    {
        float x = (float)rng.NextDouble() * Width;
        float y = (float)rng.NextDouble() * Height;
        float dir = (float)rng.NextDouble() * Mathf.PI * 2f;
        int segments = 14 + rng.Next(10);
        float width = 12f + (float)rng.NextDouble() * 8f;

        for (int s = 0; s < segments; s++)
        {
            dir += ((float)rng.NextDouble() - 0.5f) * 0.7f;
            float len = 16f + (float)rng.NextDouble() * 24f;
            float nx = x + Mathf.Cos(dir) * len;
            float ny = y + Mathf.Sin(dir) * len;
            float segWidth = Mathf.Max(0.6f, width * (1f - (float)s / segments));
            PlotLine(pixels, x, y, nx, ny, segWidth, crackColor);
            x = nx;
            y = ny;
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                break;
        }
    }

    private static void PlotLine(Color[] pixels, float x0, float y0, float x1, float y1, float width, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1) - width - 1));
        int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(Mathf.Max(x0, x1) + width + 1));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1) - width - 1));
        int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(Mathf.Max(y0, y1) + width + 1));

        Vector2 a = new Vector2(x0, y0);
        Vector2 b = new Vector2(x1, y1);
        Vector2 ab = b - a;
        float abLenSq = Mathf.Max(0.0001f, ab.sqrMagnitude);

        for (int py = minY; py <= maxY; py++)
        {
            for (int px = minX; px <= maxX; px++)
            {
                Vector2 p = new Vector2(px, py);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / abLenSq);
                Vector2 proj = a + ab * t;
                float dist = Vector2.Distance(p, proj);
                if (dist <= width)
                {
                    float falloff = 1f - Mathf.Clamp01(dist / width);
                    int idx = py * Width + px;
                    pixels[idx] = Color.Lerp(pixels[idx], color, falloff * 0.95f);
                }
            }
        }
    }

    private static void DrawNail(Color[] pixels, int cx, int cy, int radius)
    {
        Color metal = new Color(0.55f, 0.55f, 0.58f);
        Color metalDark = new Color(0.20f, 0.20f, 0.22f);
        Color metalHi = new Color(0.85f, 0.85f, 0.85f);
        Color socketShadow = new Color(0.05f, 0.03f, 0.015f);

        int outer = radius + 8;
        for (int py = -outer; py <= outer; py++)
        {
            int y = cy + py;
            if (y < 0 || y >= Height)
                continue;
            for (int px = -outer; px <= outer; px++)
            {
                int x = cx + px;
                if (x < 0 || x >= Width)
                    continue;

                float dist = Mathf.Sqrt(px * px + py * py);
                int idx = y * Width + x;

                if (dist <= radius)
                {
                    Vector2 n = (new Vector2(px, py)).normalized;
                    float light = Mathf.Clamp01(Vector2.Dot(n, new Vector2(-0.6f, -0.6f)) * 0.5f + 0.5f);
                    Color c = Color.Lerp(metalDark, Color.Lerp(metal, metalHi, light), 0.8f);

                    float slot = Mathf.Abs(px * 0.7f + py * 0.7f);
                    if (dist < radius * 0.75f && slot < radius * 0.12f)
                        c = metalDark * 0.6f;

                    pixels[idx] = c;
                }
                else if (dist <= outer)
                {
                    float t = (dist - radius) / (outer - radius);
                    pixels[idx] = Color.Lerp(socketShadow, pixels[idx], t);
                }
            }
        }
    }
}
