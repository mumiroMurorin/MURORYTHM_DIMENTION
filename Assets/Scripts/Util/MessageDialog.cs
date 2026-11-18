using UnityEngine;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.Runtime.InteropServices;
#endif

/// <summary>
/// OS標準のメッセージボックスを表示する
///
/// Windows の機能を直接呼び出す。
/// (.NET の MessageBox は Unity 上だと古い見た目になり、ボタンも英語の Yes / No になるため使わない)
/// Windows 以外の環境ではログのみ。
/// </summary>
public static class MessageDialog
{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    // ボタンの種類
    const uint MB_OK = 0x00000000;
    const uint MB_YESNO = 0x00000004;
    // アイコン
    const uint MB_ICONERROR = 0x00000010;
    const uint MB_ICONQUESTION = 0x00000020;
    const uint MB_ICONWARNING = 0x00000030;
    // 最初から選ばれているボタン (2番目 = いいえ)
    const uint MB_DEFBUTTON2 = 0x00000100;
    // 閉じるまで、このアプリのほかのウィンドウを操作できないようにする
    const uint MB_TASKMODAL = 0x00002000;
    // 「はい」が押されたときの戻り値
    const int IDYES = 6;

    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
    static extern int NativeMessageBox(IntPtr hWnd, string text, string caption, uint type);

    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();
#endif

    /// <summary>
    /// はい/いいえ の確認。「はい」が選ばれたとき true
    /// </summary>
    public static bool Confirm(string title, string message)
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        try
        {
            int result = NativeMessageBox(GetActiveWindow(), message, title, MB_YESNO | MB_ICONWARNING | MB_DEFBUTTON2 | MB_TASKMODAL);
            return result == IDYES;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MessageDialog] 確認ダイアログを表示できませんでした: {ex.Message}");
            return true;
        }
#else
        Debug.LogWarning($"[MessageDialog] {title}: {message}");
        return true;
#endif
    }

    /// <summary>
    /// はい/いいえ のたずね (? の印。最初から「はい」が選ばれている)。「はい」が選ばれたとき true
    /// (Confirm は、取り返しのつかない操作の確認なので、注意の印で「いいえ」が最初に選ばれている)
    /// </summary>
    public static bool Ask(string title, string message)
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        try
        {
            int result = NativeMessageBox(GetActiveWindow(), message, title, MB_YESNO | MB_ICONQUESTION | MB_TASKMODAL);
            return result == IDYES;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MessageDialog] たずねるダイアログを表示できませんでした: {ex.Message}");
            return false;
        }
#else
        Debug.Log($"[MessageDialog] {title}: {message}");
        return false;
#endif
    }

    /// <summary>
    /// エラーの通知
    /// </summary>
    public static void ShowError(string title, string message)
    {
        Debug.LogWarning($"[MessageDialog] {title}: {message}");

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        try
        {
            NativeMessageBox(GetActiveWindow(), message, title, MB_OK | MB_ICONERROR | MB_TASKMODAL);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MessageDialog] エラーダイアログを表示できませんでした: {ex.Message}");
        }
#endif
    }
}
