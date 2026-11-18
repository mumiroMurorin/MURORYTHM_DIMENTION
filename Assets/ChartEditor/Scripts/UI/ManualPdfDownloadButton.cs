using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
using System.Windows.Forms;
using Ookii.Dialogs.WinForms;
#endif

namespace ChartEditor
{
    /// <summary>
    /// 説明書の画面の「PDFでダウンロード」ボタン (2026-11-19)
    ///
    /// 押すと保存先を聞いて、説明書の PDF をそこに書き出す。書き出したら、すぐ開くかを聞く。
    /// PDF は TextAsset (.pdf.bytes) としてシーンから参照しているので、譜面エディタのビルドに入る
    /// (作り方: Docs/ChartEditorManual/make_manual.py --install。説明書の画像と一緒に、この PDF も書き直す)。
    /// ボタンはシーンに置いてある (説明書の「戻る」の右)。
    /// </summary>
    public class ManualPdfDownloadButton : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Button button;
        [Tooltip("説明書の PDF (拡張子を .pdf.bytes にしたもの)")]
        [SerializeField] TextAsset pdf;
        [Tooltip("保存するときに、はじめに出すファイル名")]
        [SerializeField] string fileName = "譜面エディタ説明書.pdf";

        void Awake()
        {
            if (button != null) { button.onClick.AddListener(Download); }
        }

        void OnDestroy()
        {
            if (button != null) { button.onClick.RemoveListener(Download); }
        }

        /// <summary>
        /// 保存先を聞いて、PDF を書き出す
        /// </summary>
        public void Download()
        {
            // (TextAsset.bytes は、読むたびに写しを作るので、1回だけ読む)
            byte[] data = pdf != null ? pdf.bytes : null;
            if (data == null || data.Length == 0)
            {
                MessageDialog.ShowError("PDF を保存できません", "説明書の PDF が見つかりません。");
                return;
            }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            string path;
            try
            {
                using (var dialog = new VistaSaveFileDialog())
                {
                    dialog.Title = "説明書 (PDF) の保存先";
                    dialog.Filter = "PDF ファイル (*.pdf)|*.pdf";
                    dialog.DefaultExt = "pdf";
                    dialog.AddExtension = true;
                    dialog.OverwritePrompt = true;
                    // はじめはデスクトップに、決まったファイル名で出す
                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    dialog.FileName = string.IsNullOrEmpty(desktop) ? fileName : Path.Combine(desktop, fileName);

                    if (dialog.ShowDialog() != DialogResult.OK) { return; }
                    path = dialog.FileName;
                }

                File.WriteAllBytes(path, data);
            }
            catch (Exception ex)
            {
                MessageDialog.ShowError("PDF を保存できませんでした", ex.Message);
                return;
            }

            Debug.Log($"【説明書】PDF を保存しました: {path}");
            if (MessageDialog.Ask("PDF を保存しました", $"説明書の PDF を保存しました。\n{path}\n\n今すぐ開きますか？"))
            {
                // PDF を開くアプリ (Windows で決めてあるもの) で開く
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"【説明書】保存した PDF を開けませんでした: {ex.Message}");
                    UnityEngine.Application.OpenURL(new Uri(path).AbsoluteUri);
                }
            }
#else
            Debug.LogWarning("【説明書】この環境では、PDF の保存先を選べません");
#endif
        }
    }
}
