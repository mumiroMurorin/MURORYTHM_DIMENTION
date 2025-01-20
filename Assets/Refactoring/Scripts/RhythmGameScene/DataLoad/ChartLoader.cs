using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Refactoring
{
    public class ChartLoader : MonoBehaviour
    {
        [SerializeField] TextAsset chartData;

        private async void LoadChart()
        {
            if(chartData == null)
            {
                Debug.LogError("【System】CSVファイルが参照されていません。");
                return;
            }

            List<string[]> data = await CSVReader.ParseCsvAsync(chartData);
            
        }
    }

    public class CSVReader
    {
        /// <summary>
        /// CSVデータからList<string[]>に変換
        /// </summary>
        /// <param name="csvFile"></param>
        /// <returns></returns>
        public static async UniTask<List<string[]>> ParseCsvAsync(TextAsset csvFile)
        {
            if (csvFile == null)
            {
                Debug.LogError("【System】CSVファイルがnullです。");
                return null;
            }

            string csvContent = csvFile.text;

            return await UniTask.RunOnThreadPool(() =>
            {
                var result = new List<string[]>();

                using (var reader = new StringReader(csvContent))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // 行をカンマで分割して配列に変換
                        string[] row = line.Split(',');
                        result.Add(row);
                    }
                }

                return result;
            });
        }
    }

    public class JsonReader
    {
        /// <summary>
        /// JSONを読み込み、List<string[]>に変換するメソッド
        /// </summary>
        /// <param name="jsonFile">読み込むJSONファイル</param>
        /// <returns>JSONデータをList<string[]>に変換した結果</returns>
        public async UniTask<List<string[]>> ParseJsonAsync(TextAsset jsonFile)
        {
            // JSONファイルの内容を読み込み
            string jsonContent = jsonFile.text;

            // JSONをList<string[]>に変換
            List<string[]> result = await UniTask.RunOnThreadPool(() =>
            {
                // JSONのデシリアライズ
                var data = JsonConvert.DeserializeObject<List<List<string>>>(jsonContent);

                // List<List<string>> を List<string[]> に変換
                List<string[]> list = new List<string[]>();
                foreach (var sublist in data)
                {
                    list.Add(sublist.ToArray());
                }

                return list;
            });

            return result;
        }
    }
}
