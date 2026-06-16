using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Drawing.Text;
using Microsoft.Win32;
#endif

public static class InstalledFontProvider
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    static Dictionary<string, string> fontFilePathByName;
#endif

    public static IReadOnlyList<string> GetInstalledFontNames()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        return GetInstalledFontFilePathMap()
            .Keys
            .OrderBy(x => x)
            .ToList();
#else
        return new List<string>();
#endif
    }

    public static string GetFontFilePath(string fontName)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (string.IsNullOrWhiteSpace(fontName)) { return null; }

        return GetInstalledFontFilePathMap().TryGetValue(fontName, out var path) ? path : null;
#else
        return null;
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    static Dictionary<string, string> GetInstalledFontFilePathMap()
    {
        if (fontFilePathByName != null) { return fontFilePathByName; }

        fontFilePathByName = new Dictionary<string, string>();

        foreach (var fontPath in EnumerateWindowsFontFilePaths())
        {
            AddFontFamiliesFromFile(fontPath, fontFilePathByName);
        }

        if (fontFilePathByName.Count == 0)
        {
            var collection = new InstalledFontCollection();
            foreach (var family in collection.Families)
            {
                if (string.IsNullOrWhiteSpace(family.Name)) { continue; }
                if (!fontFilePathByName.ContainsKey(family.Name))
                {
                    fontFilePathByName.Add(family.Name, null);
                }
            }
        }

        return fontFilePathByName;
    }

    static IEnumerable<string> EnumerateWindowsFontFilePaths()
    {
        foreach (var fontPath in EnumerateFontFilePaths(Registry.LocalMachine))
        {
            yield return fontPath;
        }

        foreach (var fontPath in EnumerateFontFilePaths(Registry.CurrentUser))
        {
            yield return fontPath;
        }
    }

    static IEnumerable<string> EnumerateFontFilePaths(RegistryKey root)
    {
        using (var key = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"))
        {
            if (key == null) { yield break; }

            foreach (var valueName in key.GetValueNames())
            {
                var value = key.GetValue(valueName) as string;
                if (string.IsNullOrWhiteSpace(value)) { continue; }

                var fontPath = Path.IsPathRooted(value)
                    ? value
                    : Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Fonts), value);

                if (File.Exists(fontPath))
                {
                    yield return fontPath;
                }
            }
        }
    }

    static void AddFontFamiliesFromFile(string fontPath, Dictionary<string, string> map)
    {
        try
        {
            using (var privateFonts = new PrivateFontCollection())
            {
                privateFonts.AddFontFile(fontPath);

                foreach (var family in privateFonts.Families)
                {
                    if (string.IsNullOrWhiteSpace(family.Name)) { continue; }
                    if (!map.ContainsKey(family.Name))
                    {
                        map.Add(family.Name, fontPath);
                    }
                }
            }
        }
        catch
        {
            // Some installed fonts cannot be loaded through System.Drawing; skip them safely.
        }
    }
#endif
}
