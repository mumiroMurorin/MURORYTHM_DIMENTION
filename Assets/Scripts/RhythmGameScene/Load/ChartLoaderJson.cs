using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using VContainer;
using System;

namespace Refactoring
{
    public class ChartLoaderJson : MonoBehaviour, IChartLoader
    {
        [SerializeField] TextAsset jsonData;

        IMusicDataGetter musicDataGetter;
        IChartDataSetter chartDataSetter;

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
            
        }

        void IChartLoader.LoadChart(Action callback)
        {
            DifficulityName difficulty = musicDataGetter.Difficulty.Value;
            ChartData chartData = LoadChartData(musicDataGetter.Music.Value.GetChart(difficulty));

            chartDataSetter.SetChartData(chartData);
            callback.Invoke();
        }

        /// <summary>
        /// 非同期でデータを読み込む
        /// </summary>
        /// <param name="textAsset"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ChartData LoadChartData(TextAsset textAsset)
        {
            if (jsonData == null || textAsset == null)
            {
                Debug.LogError("【System】CSVファイルが参照されていません。");
                return null;
            }

            ChartData chartData = JsonUtility.FromJson<ChartData>(textAsset != null ? textAsset.text : jsonData.text);

            return chartData;
        }
    }
}
