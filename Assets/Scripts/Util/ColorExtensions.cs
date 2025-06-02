using UnityEngine;

public static class ColorExtensions
{
    public static string ToHexString(this Color color, bool includeAlpha = false)
    {
        Color32 c = color; // Color => Color32‚É•ÏŠ·
        if (includeAlpha)
            return $"#{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}";
        else
            return $"#{c.r:X2}{c.g:X2}{c.b:X2}";
    }
}
