#if UNITY_EDITOR || UNITY_STANDALONE_WIN
using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using Ookii.Dialogs.WinForms;
using UnityEngine;

namespace JsonUtil
{
    public static class JsonWriter
    {
        private static bool TrySerializeJson<T>(T data, out string json)
        {
            json = default;
            try
            {
                json = JsonConvert.SerializeObject(
                    data,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonWriter] Serialize failed: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 書き込み途中で失敗しても元のファイルが壊れないよう、一時ファイルに書いてから置き換える
        /// </summary>
        /// <param name="keepBackup">true のとき、上書き前のファイルを「元のファイル名.bak」として残す</param>
        private static void WriteTextSafely(string filePath, string text, bool keepBackup)
        {
            string tempPath = filePath + ".tmp";
            File.WriteAllText(tempPath, text);

            if (!File.Exists(filePath))
            {
                File.Move(tempPath, filePath);
                return;
            }

            string backupPath = filePath + ".bak";
            try
            {
                File.Replace(tempPath, filePath, keepBackup ? backupPath : null);
            }
            catch (Exception)
            {
                // 同期フォルダなど File.Replace が使えない場所ではコピーで代替する
                if (keepBackup) { File.Copy(filePath, backupPath, true); }
                File.Copy(tempPath, filePath, true);
                File.Delete(tempPath);
            }
        }

        public static bool TrySaveToJsonPath<T>(T data, string filePath, bool keepBackup = false)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Debug.LogError("[JsonWriter] Invalid save path.");
                return false;
            }

            if (!TrySerializeJson(data, out string json)) { return false; }

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                WriteTextSafely(filePath, json, keepBackup);
                Debug.Log($"[JsonWriter] Saved: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonWriter] Save failed: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        public static bool TrySaveToJsonFile<T>(T data, string fileName)
        {
            try
            {
                string path = Path.Combine(UnityEngine.Application.persistentDataPath, fileName);
                return TrySaveToJsonPath(data, path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonWriter] Save failed: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        public static bool TrySaveToJsonFileDialog<T>(T data)
        {
            return TrySaveToJsonFileDialog(data, out _);
        }

        public static bool TrySaveToJsonFileDialog<T>(T data, out string savedPath)
        {
            savedPath = string.Empty;

            if (!TrySerializeJson(data, out string json)) { return false; }

            try
            {
                using (VistaSaveFileDialog dialog = new VistaSaveFileDialog())
                {
                    dialog.Title = "Select save destination";
                    dialog.Filter = "JSON file (*.json)|*.json";
                    dialog.FileName = "chart.json";

                    if (dialog.ShowDialog() != DialogResult.OK)
                    {
                        Debug.Log("[JsonWriter] Save canceled.");
                        return false;
                    }

                    WriteTextSafely(dialog.FileName, json, false);
                    savedPath = dialog.FileName;
                    Debug.Log($"[JsonWriter] Saved: {dialog.FileName}");
                    return true;
                }
            }
            catch (IOException ioEx)
            {
                Debug.LogError($"[JsonWriter] IO error: {ioEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonWriter] Unexpected error: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
    }

    public static class JsonLoader
    {
        public static bool TryLoadFromJsonFileDialog<T>(out T result)
        {
            return TryLoadFromJsonFileDialog(out result, out _);
        }

        public static bool TryLoadFromJsonFileDialog<T>(out T result, out string loadedPath)
        {
            return TryLoadFromJsonFileDialog(out result, out loadedPath, out _);
        }

        /// <param name="errorMessage">読み込みに失敗したときの理由。キャンセルされたときは空</param>
        public static bool TryLoadFromJsonFileDialog<T>(out T result, out string loadedPath, out string errorMessage)
        {
            result = default;
            loadedPath = string.Empty;
            errorMessage = string.Empty;

            try
            {
                using (VistaOpenFileDialog dialog = new VistaOpenFileDialog())
                {
                    dialog.Title = "Select JSON file to load";
                    dialog.Filter = "JSON file (*.json)|*.json";
                    dialog.Multiselect = false;

                    if (dialog.ShowDialog() != DialogResult.OK)
                    {
                        Debug.Log("[JsonLoader] Load canceled.");
                        return false;
                    }

                    loadedPath = dialog.FileName;
                    string json = File.ReadAllText(dialog.FileName);
                    result = JsonConvert.DeserializeObject<T>(json);
                    Debug.Log($"[JsonLoader] Loaded: {dialog.FileName}");
                    return true;
                }
            }
            catch (IOException ioEx)
            {
                errorMessage = $"ファイルを読み込めませんでした: {ioEx.Message}";
                Debug.LogError($"[JsonLoader] IO error: {ioEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                errorMessage = $"JSONとして解釈できませんでした: {jsonEx.Message}";
                Debug.LogError($"[JsonLoader] JSON parse error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                errorMessage = $"予期しないエラーが発生しました: {ex.Message}";
                Debug.LogError($"[JsonLoader] Unexpected error: {ex.Message}\n{ex.StackTrace}");
            }

            return false;
        }

        public static bool TryLoadFromJsonFile<T>(string filePath, out T result)
        {
            result = default;

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("[JsonLoader] Invalid file path.");
                return false;
            }

            if (!File.Exists(filePath))
            {
                Debug.LogError($"[JsonLoader] File not found: {filePath}");
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
                Debug.LogError($"[JsonLoader] JSON parse error: {ex.Message}");
            }
            catch (IOException ex)
            {
                Debug.LogError($"[JsonLoader] Read failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonLoader] Unexpected error: {ex.Message}");
            }

            return false;
        }

        public static bool TryLoadFromTextAsset<T>(TextAsset jsonAsset, out T result)
        {
            result = default;

            if (jsonAsset == null)
            {
                Debug.LogError("[JsonLoader] TextAsset is null.");
                return false;
            }

            try
            {
                result = JsonConvert.DeserializeObject<T>(jsonAsset.text);
                return true;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[JsonLoader] JSON parse error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonLoader] Unexpected error: {ex.Message}");
            }

            return false;
        }
    }
}
#endif