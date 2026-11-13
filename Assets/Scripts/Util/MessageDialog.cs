using UnityEngine;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
using System.Windows.Forms;
#endif

/// <summary>
/// OS標準のメッセージボックスを表示する
/// (ファイルダイアログと同じく Windows 専用。それ以外の環境ではログのみ)
/// </summary>
public static class MessageDialog
{
    /// <summary>
    /// はい/いいえ の確認。「はい」が選ばれたとき true
    /// </summary>
    public static bool Confirm(string title, string message)
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        try
        {
            var result = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            return result == DialogResult.Yes;
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
    /// エラーの通知
    /// </summary>
    public static void ShowError(string title, string message)
    {
        Debug.LogWarning($"[MessageDialog] {title}: {message}");

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        try
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MessageDialog] エラーダイアログを表示できませんでした: {ex.Message}");
        }
#endif
    }
}
