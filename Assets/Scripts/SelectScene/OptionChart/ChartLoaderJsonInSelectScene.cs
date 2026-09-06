using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JsonUtil;
using ChartConvert;
using VContainer;
using System;

public class ChartLoaderJsonInSelectScene : MonoBehaviour, IChartLoader
{
    [SerializeField] NoteJudgementSettingsCatalog judgementSettingsCatalog;
    [SerializeField] Difficulty previewDifficulty = Difficulty.Normal;

    [Inject] INoteSpawnDataOptionGetter optionGetter;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {

    }

    void IChartLoader.LoadChart(Action callback)
    {
        callback.Invoke();
    }

    void IChartLoader.LoadChart(TextAsset jsonFile, Action callback)
    {
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
        ChartImporterForRhythmGame chartImporter = new ChartImporterForRhythmGame();
        ChartData chart = chartImporter.Import(chartDataOrigin, optionGetter);

        // 判定枠の調整
        JudgementWindowAdjuster judgementWindowAdjuster = new JudgementWindowAdjuster();
        AdjustJudgementWindow(judgementWindowAdjuster, chart);


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
        AdjustJudgementWindow(judgementWindowAdjuster, chart);


        return chart;
    }

    private void AdjustJudgementWindow(JudgementWindowAdjuster judgementWindowAdjuster, ChartData chart)
    {
        if (judgementSettingsCatalog == null)
        {
            Debug.LogWarning("[System] Judgement settings catalog is not assigned.");
            return;
        }

        judgementWindowAdjuster.AdjustJudgementWindow(chart, judgementSettingsCatalog, previewDifficulty);
    }
}
