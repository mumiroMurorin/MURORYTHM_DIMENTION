using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;

namespace Refactoring
{
    public class ChartLoader : MonoBehaviour, IChartLoader
    {
        [SerializeField] TextAsset csvData;

        public async UniTask<ChartData> LoadChartData(TextAsset textAsset, Action callback = null)
        {
            if (csvData == null || textAsset == null)
            {
                Debug.LogError("�ySystem�zCSV�t�@�C�����Q�Ƃ���Ă��܂���B");
                return null;
            }

            List<string[]> data = await CSVReader.ParseCsvAsync(textAsset != null ? textAsset : csvData);

            callback?.Invoke();
            return null;
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

    public class JsonReader
    {
        /// <summary>
        /// JSON��ǂݍ��݁AList<string[]>�ɕϊ����郁�\�b�h
        /// </summary>
        /// <param name="jsonFile">�ǂݍ���JSON�t�@�C��</param>
        /// <returns>JSON�f�[�^��List<string[]>�ɕϊ���������</returns>
        public async UniTask<List<string[]>> ParseJsonAsync(TextAsset jsonFile)
        {
            // JSON�t�@�C���̓��e��ǂݍ���
            string jsonContent = jsonFile.text;

            // JSON��List<string[]>�ɕϊ�
            List<string[]> result = await UniTask.RunOnThreadPool(() =>
            {
                // JSON�̃f�V���A���C�Y
                var data = JsonConvert.DeserializeObject<List<List<string>>>(jsonContent);

                // List<List<string>> �� List<string[]> �ɕϊ�
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
