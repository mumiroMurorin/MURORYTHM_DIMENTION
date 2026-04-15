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

        public static bool TrySaveToJsonPath<T>(T data, string filePath)
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

                File.WriteAllText(filePath, json);
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

                    File.WriteAllText(dialog.FileName, json);
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
            result = default;
            loadedPath = string.Empty;

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

                    string json = File.ReadAllText(dialog.FileName);
                    result = JsonConvert.DeserializeObject<T>(json);
                    loadedPath = dialog.FileName;
                    Debug.Log($"[JsonLoader] Loaded: {dialog.FileName}");
                    return true;
                }
            }
            catch (IOException ioEx)
            {
                Debug.LogError($"[JsonLoader] IO error: {ioEx.Message}");
            }
            catch (JsonException jsonEx)
            {
                Debug.LogError($"[JsonLoader] JSON parse error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
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