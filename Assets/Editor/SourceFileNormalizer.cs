using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ソースコード (.cs / .shader など) の文字コードを UTF-8 (BOM 付き)、改行コードを CRLF に統一する。
/// <para>
/// Unity が新規作成するスクリプトは BOM なしで保存され、日本語環境の Visual Studio は
/// それを Shift-JIS とみなして日本語を書き込んでしまう。インポート時に BOM を付け、
/// Shift-JIS で保存されたファイルは UTF-8 に変換することで、どのエディターで編集しても
/// UTF-8 のまま保たれるようにする。改行コードが混在していれば CRLF にそろえる。
/// ルールはリポジトリ直下の .editorconfig / .gitattributes と同じ。
/// </para>
/// </summary>
public sealed class SourceFileNormalizer : AssetPostprocessor
{
    const string MENU_PATH = "Tools/Source Files/Normalize Encoding And Line Endings";
    const string CRLF = "\r\n";

    static readonly string[] TargetExtensions = { ".cs", ".shader", ".hlsl", ".cginc", ".compute" };

    // サードパーティ製アセットは対象外 (アセット更新時に余計な差分を出さないため)
    static readonly string[] ExcludedFolders =
    {
        "Assets/Imports/",
        "Assets/Plugins/",
        "Assets/NaughtyAttributes/",
        "Assets/TextMesh Pro/",
        "Assets/Samples/",
        "Assets/StandaloneFileBrowser/",
        "Assets/YughuesFreeGroundMaterials/",
        "Assets/Deform/",
    };

    static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };
    static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    enum SourceEncoding
    {
        Utf8WithBom,
        Utf8WithoutBom,
        ShiftJis,
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        List<string> converted = NormalizeFiles(importedAssets.Concat(movedAssets).Distinct());
        if (converted.Count == 0) { return; }

        // インポート処理の最中には再インポートせず、処理が終わってから反映させる
        EditorApplication.delayCall += () =>
        {
            foreach (string path in converted) { AssetDatabase.ImportAsset(path); }
        };
    }

    [MenuItem(MENU_PATH)]
    static void NormalizeAllFiles()
    {
        IEnumerable<string> paths = Directory.EnumerateFiles("Assets", "*", SearchOption.AllDirectories)
            .Select(path => path.Replace('\\', '/'));

        List<string> converted = NormalizeFiles(paths);
        if (converted.Count > 0) { AssetDatabase.Refresh(); }

        Debug.Log($"[SourceFile] {converted.Count} 件のファイルを UTF-8 (BOM 付き)・CRLF に統一しました");
    }

    static List<string> NormalizeFiles(IEnumerable<string> paths)
    {
        var converted = new List<string>();

        foreach (string path in paths)
        {
            if (!IsTarget(path)) { continue; }

            try
            {
                if (!TryNormalize(path, out SourceEncoding source)) { continue; }

                converted.Add(path);

                // BOM の付与や改行コードの統一だけなら (新規スクリプトなど) ログを出さない
                if (source == SourceEncoding.ShiftJis)
                {
                    Debug.Log($"[SourceFile] Shift-JIS のファイルを UTF-8 (BOM 付き) に変換しました: {path}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SourceFile] 文字コード・改行コードを統一できませんでした: {path}\n{e.Message}");
            }
        }

        return converted;
    }

    static bool IsTarget(string path)
    {
        if (!path.StartsWith("Assets/", StringComparison.Ordinal)) { return false; }

        string extension = Path.GetExtension(path);
        if (!TargetExtensions.Any(target => string.Equals(target, extension, StringComparison.OrdinalIgnoreCase))) { return false; }

        if (ExcludedFolders.Any(folder => path.StartsWith(folder, StringComparison.Ordinal))) { return false; }

        return File.Exists(path);
    }

    /// <summary>
    /// UTF-8 (BOM 付き)・CRLF でなければ変換して上書きする。上書きした場合は true を返す。
    /// 改行コード以外の文字の内容は一切変えない。
    /// </summary>
    static bool TryNormalize(string path, out SourceEncoding source)
    {
        byte[] bytes = File.ReadAllBytes(path);

        string text;
        if (HasUtf8Bom(bytes))
        {
            text = StrictUtf8.GetString(bytes, Utf8Bom.Length, bytes.Length - Utf8Bom.Length);
            source = SourceEncoding.Utf8WithBom;
        }
        else if (TryDecodeUtf8(bytes, out text))
        {
            source = SourceEncoding.Utf8WithoutBom;
        }
        else if (TryDecodeShiftJis(bytes, out text))
        {
            source = SourceEncoding.ShiftJis;
        }
        else
        {
            throw new InvalidDataException("UTF-8 と Shift-JIS のどちらとしても読み込めません。手動で確認してください。");
        }

        byte[] body = StrictUtf8.GetBytes(NormalizeLineEndings(text));
        byte[] result = new byte[Utf8Bom.Length + body.Length];
        Buffer.BlockCopy(Utf8Bom, 0, result, 0, Utf8Bom.Length);
        Buffer.BlockCopy(body, 0, result, Utf8Bom.Length, body.Length);

        if (result.SequenceEqual(bytes)) { return false; }

        File.WriteAllBytes(path, result);
        return true;
    }

    static string NormalizeLineEndings(string text)
    {
        return text.Replace(CRLF, "\n").Replace('\r', '\n').Replace("\n", CRLF);
    }

    static bool HasUtf8Bom(byte[] bytes)
    {
        return bytes.Length >= 3 && bytes[0] == Utf8Bom[0] && bytes[1] == Utf8Bom[1] && bytes[2] == Utf8Bom[2];
    }

    static bool TryDecodeUtf8(byte[] bytes, out string text)
    {
        try
        {
            text = StrictUtf8.GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            text = null;
            return false;
        }
    }

    static bool TryDecodeShiftJis(byte[] bytes, out string text)
    {
        text = null;

        Encoding shiftJis;
        try
        {
            shiftJis = Encoding.GetEncoding(932);
        }
        catch (Exception)
        {
            return false;
        }

        string decoded = shiftJis.GetString(bytes);

        // 元のバイト列に正確に戻せる場合だけ Shift-JIS とみなす (読み違いによる文字化けを防ぐ)
        if (!shiftJis.GetBytes(decoded).SequenceEqual(bytes)) { return false; }

        text = decoded;
        return true;
    }
}
