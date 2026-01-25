using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JsonUtil;
using ChartConvert;
using VContainer;
using System;

public class ChartLoaderJson : MonoBehaviour, IChartLoader
{
    [SerializeField] List<NoteTypeToJudgementWindow> judgementWindows;

    [Inject] IMusicDataGetter musicDataGetter;
    [Inject] INoteSpawnDataOptionHolder optionGetter;
    [Inject] IChartDataSetter chartDataSetter;

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
        ChartData chartData = LoadChartData(musicDataGetter.Music.Value.GetChartPath(difficulty));

        chartDataSetter.SetChartData(chartData);
        callback.Invoke();
    }

    void IChartLoader.LoadChart(TextAsset jsonFile, Action callback)
    {
        ChartData chartData = LoadChartData(jsonFile);

        chartDataSetter.SetChartData(chartData);
        callback.Invoke();
    }

    public ChartData LoadChartData(string path)
    {
        if (path == null || path == "")
        {
            Debug.LogError("【System】Jsonファイルパスが参照されていません");
            return null;
        }

        // Jsonデータの変換
        if (!JsonLoader.TryLoadFromJsonFile(path, out ChartDataOrigin chartDataOrigin)) 
        {
            // 失敗
            return null;
        }

        // 譜面データの変換
        var chartImporter = new ChartImporterForRhythmGame();
        var chart = chartImporter.Import(chartDataOrigin, optionGetter);

        // 判定枠の調整
        JudgementWindowAdjuster judgementWindowAdjuster = new JudgementWindowAdjuster();
        judgementWindowAdjuster.AdjustJudgementWindow(chart, judgementWindows);


        return chart;
    }

    public ChartData LoadChartData(TextAsset jsonFile)
    {
        // Jsonデータの変換
        if (!JsonLoader.TryLoadFromTextAsset(jsonFile, out ChartDataOrigin chartDataOrigin))
        {
            // 失敗
            return null;
        }

        // 譜面データの変換
        ChartImporterForRhythmGame chartImporter = new ChartImporterForRhythmGame();
        ChartData chart = chartImporter.Import(chartDataOrigin, optionGetter);

        // 判定枠の調整
        JudgementWindowAdjuster judgementWindowAdjuster = new JudgementWindowAdjuster();
        judgementWindowAdjuster.AdjustJudgementWindow(chart, judgementWindows);


        return chart;
    }
}
