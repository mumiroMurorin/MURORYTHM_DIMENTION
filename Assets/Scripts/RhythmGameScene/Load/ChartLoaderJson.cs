using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JsonUtil;
using ChartConvert;
using VContainer;
using System;

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
        Difficulty difficulty = musicDataGetter.Difficulty.Value;
        ChartData chartData = LoadChartData(musicDataGetter.Music.Value.GetChart(difficulty));

        chartDataSetter.SetChartData(chartData);
        callback.Invoke();
    }

    /// <summary>
    /// データを読み込む
    /// </summary>
    /// <param name="textAsset"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public ChartData LoadChartData(TextAsset textAsset)
    {
        if (jsonData == null || textAsset == null)
        {
            Debug.LogError("【System】Jsonファイルが参照されていません。");
            return null;
        }

        // データの変換
        if(!JsonLoader.TryLoadFromTextAsset(textAsset != null ? textAsset : jsonData, out ChartDataOrigin chartDataOrigin))
        {
            // 失敗
            return null;
        }



        return null;
    }
}
