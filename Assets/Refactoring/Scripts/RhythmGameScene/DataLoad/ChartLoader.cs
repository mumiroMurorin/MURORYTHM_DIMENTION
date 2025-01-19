using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

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
}
