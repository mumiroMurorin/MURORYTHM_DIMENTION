using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;
using Newtonsoft.Json;
using System;

namespace Refactoring
{
    public class ChartLoader : MonoBehaviour, IChartLoader
    {
        [SerializeField] TextAsset csvData;

        IMusicDataGetter musicDataGetter;
        IChartDataSetter chartDataSetter;
        CancellationTokenSource cts;

        [Inject]
        public void Constructor(IMusicDataGetter musicDataGetter, IChartDataSetter chartDataSetter)
        {
            this.musicDataGetter = musicDataGetter;
            this.chartDataSetter = chartDataSetter;
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            cts = new CancellationTokenSource();
        }

        void IChartLoader.LoadChart(Action callback)
        {
            DifficulityName difficulty = musicDataGetter.Difficulty.Value;
            LoadChartData(musicDataGetter.Music.Value.GetChart(difficulty), cts.Token, callback).Forget();
        }

        /// <summary>
        /// �񓯊��Ńf�[�^��ǂݍ���
        /// ��������
        /// </summary>
        /// <param name="textAsset"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public async UniTask<ChartData> LoadChartData(TextAsset textAsset, CancellationToken token, Action callback = null)
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
}
