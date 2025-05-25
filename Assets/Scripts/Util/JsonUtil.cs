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
                string json = JsonConvert.SerializeObject(
                    data,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
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
                string json = JsonConvert.SerializeObject(
                    data,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

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

    public static class JsonLoader
    {
        public static bool TryLoadFromJsonFileDialog<T>(out T result)
        {
            result = default;

            try
            {
                using (VistaOpenFileDialog dialog = new VistaOpenFileDialog())
                {
                    dialog.Title = "読み込むJSONファイルを選択";
                    dialog.Filter = "JSONファイル (*.json)|*.json";
                    dialog.Multiselect = false;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        string json = File.ReadAllText(dialog.FileName);
                        result = JsonConvert.DeserializeObject<T>(json);

                        UnityEngine.Debug.Log($"【JsonImporter】読み込み完了: {dialog.FileName}");
                        return true;
                    }
                    else
                    {
                        UnityEngine.Debug.Log("【JsonImporter】読み込みがキャンセルされました。");
                        return false;
                    }
                }
            }
            catch (IOException ioEx)
            {
                UnityEngine.Debug.LogError($"【JsonImporter】ファイル入出力エラー: {ioEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                UnityEngine.Debug.LogError($"【JsonImporter】JSON解析エラー: {jsonEx.Message}");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"【JsonImporter】予期しないエラー: {ex.Message}\n{ex.StackTrace}");
            }

            return false;
        }


        /// <summary>
        /// 指定されたパスのJSONファイルを読み込み、T型にデシリアライズします。
        /// </summary>
        /// <typeparam name="T">変換対象のクラス型</typeparam>
        /// <param name="filePath">JSONファイルのフルパス</param>
        /// <param name="result">読み込まれたT型のインスタンス。失敗時はdefault。</param>
        /// <returns>成功したらtrue、失敗したらfalse</returns>
        public static bool TryLoadFromJsonFile<T>(string filePath, out T result)
        {
            result = default;

            if (string.IsNullOrEmpty(filePath))
            {
                UnityEngine.Debug.LogError("【JsonLoader】ファイルパスが無効です。");
                return false;
            }

            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogError($"【JsonLoader】ファイルが見つかりません: {filePath}");
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                result = JsonConvert.DeserializeObject<T>(json);
                return true;
            }
            catch (JsonException ex)
            {
                UnityEngine.Debug.LogError($"【JsonLoader】JSONの解析に失敗しました: {ex.Message}");
            }
            catch (IOException ex)
            {
                UnityEngine.Debug.LogError($"【JsonLoader】ファイルの読み込みに失敗しました: {ex.Message}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"【JsonLoader】予期しないエラーが発生しました: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// TextAsset から JSON をデシリアライズして T 型のインスタンスを返します。
        /// </summary>
        /// <typeparam name="T">変換対象のクラス型</typeparam>
        /// <param name="jsonAsset">JSONデータが含まれる TextAsset</param>
        /// <param name="result">デシリアライズされた結果（成功時）</param>
        /// <returns>成功したら true、失敗したら false</returns>
        public static bool TryLoadFromTextAsset<T>(TextAsset jsonAsset, out T result)
        {
            result = default;

            if (jsonAsset == null)
            {
                Debug.LogError("【JsonLoader】TextAsset が null です。");
                return false;
            }

            try
            {
                result = JsonConvert.DeserializeObject<T>(jsonAsset.text);
                return true;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"【JsonLoader】JSONの解析に失敗しました: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"【JsonLoader】予期しないエラーが発生しました: {ex.Message}");
            }

            return false;
        }
    }
}
#endif