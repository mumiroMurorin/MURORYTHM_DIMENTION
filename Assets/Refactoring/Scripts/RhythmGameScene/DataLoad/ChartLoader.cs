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
                Debug.LogError("�ySystem�zCSV�t�@�C�����Q�Ƃ���Ă��܂���B");
                return;
            }

            List<string[]> data = await CSVReader.ParseCsvAsync(chartData);
            
        }
    }

    public class CSVReader
    {
        /// <summary>
        /// CSV�f�[�^����List<string[]>�ɕϊ�
        /// </summary>
        /// <param name="csvFile"></param>
        /// <returns></returns>
        public static async UniTask<List<string[]>> ParseCsvAsync(TextAsset csvFile)
        {
            if (csvFile == null)
            {
                Debug.LogError("�ySystem�zCSV�t�@�C����null�ł��B");
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
                        // �s���J���}�ŕ������Ĕz��ɕϊ�
                        string[] row = line.Split(',');
                        result.Add(row);
                    }
                }

                return result;
            });
        }
    }
}
