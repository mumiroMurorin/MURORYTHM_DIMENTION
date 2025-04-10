#if UNITY_EDITOR || UNITY_STANDALONE_WIN
using UnityEngine;
using Ookii.Dialogs.WinForms;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using System;

namespace JsonUtil
{
    public static class JsonConverter
    {
        /// <summary>
        /// クラスをJson形式で書き出す
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool TrySaveToJsonFile<T>(T data, string fileName)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                string path = Path.Combine(UnityEngine.Application.persistentDataPath, fileName);
                File.WriteAllText(path, json);
                Debug.Log($"【JsonConoverter】保存成功: {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"【JsonConoverter】保存失敗: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// クラスをJson形式でダイアログ付きで書き出す
        /// </summary>
        public static bool TrySaveToJsonFileDialog<T>(T data)
        {
            try
            {
                // JSONに変換
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);

                // ダイアログ表示
                using (VistaSaveFileDialog dialog = new VistaSaveFileDialog())
                {
                    dialog.Title = "保存先を選択";
                    dialog.Filter = "JSONファイル (*.json)|*.json";
                    dialog.FileName = "chart.json";

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(dialog.FileName, json);
                        UnityEngine.Debug.Log($"【JsonConoverter】保存完了: {dialog.FileName}");
                    }
                    else
                    {
                        UnityEngine.Debug.Log("【JsonConoverter】保存がキャンセルされました。");
                        return false;
                    }
                }
            }
            catch (IOException ioEx)
            {
                UnityEngine.Debug.LogError($"【JsonConoverter】ファイル入出力エラー: {ioEx.Message}");
                return false;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"【JsonConoverter】予期しないエラー: {ex.Message}\n{ex.StackTrace}");
                return false;
            }

            return true;
        }
    }
}
#endif