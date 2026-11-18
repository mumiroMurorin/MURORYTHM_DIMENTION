using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JsonUtil;
using ChartConvert;
using VContainer;
using System;

public class ChartLoaderJson : MonoBehaviour, IChartLoader
{
    [SerializeField] NoteJudgementSettingsCatalog judgementSettingsCatalog;
    Difficulty currentDifficulty = Difficulty.Normal;

    [Inject] IMusicDataGetter musicDataGetter;
    [Inject] INoteSpawnDataOptionGetter optionGetter;
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
        currentDifficulty = difficulty;

        MusicData musicData = musicDataGetter.Music.Value;
        if (musicData == null)
        {
            GameFlowError.Raise("楽曲が選ばれていないため、譜面を読み込めません");
            return;
        }

        string path = musicData.GetChartPath(difficulty);

        // 読み込めなかったときはエラーを通知し、次へは進まない (選曲画面へ戻る)
        if (!TryLoadValidChart(() => LoadChartData(path), path, out ChartData chartData)) { return; }

        chartDataSetter.SetChartData(chartData);
        callback?.Invoke();
    }

    void IChartLoader.LoadChart(TextAsset jsonFile, Action callback)
    {
        currentDifficulty = musicDataGetter != null ? musicDataGetter.Difficulty.Value : Difficulty.Normal;

        string source = jsonFile != null ? jsonFile.name : "(譜面ファイル未設定)";

        // 読み込めなかったときはエラーを通知し、次へは進まない
        if (!TryLoadValidChart(() => LoadChartData(jsonFile), source, out ChartData chartData)) { return; }

        chartDataSetter.SetChartData(chartData);
        callback?.Invoke();
    }

    /// <summary>
    /// 譜面を読み込み、そのまま遊べる内容か確かめる。問題があればエラーを通知して false を返す。
    /// </summary>
    private static bool TryLoadValidChart(Func<ChartData> load, string source, out ChartData chartData)
    {
        chartData = null;

        try
        {
            chartData = load();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            GameFlowError.Raise($"譜面の変換に失敗しました: {source}");
            return false;
        }

        if (chartData == null)
        {
            GameFlowError.Raise($"譜面を読み込めませんでした: {source}");
            return false;
        }

        if (!TryValidateChart(chartData, out string reason))
        {
            GameFlowError.Raise($"譜面の内容に問題があります ({reason}): {source}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 最後まで遊べる譜面か確かめる (曲が終わらなくなる譜面を弾く)
    /// </summary>
    private static bool TryValidateChart(ChartData chartData, out string reason)
    {
        // 判定のあるノーツが無いと、判定数が MaxCombo に届かず曲が終わらない
        if (chartData.MaxCombo <= 0)
        {
            reason = "判定のあるノーツが1つもありません";
            return false;
        }

        // BPM が 0 などでタイミングが計算できていないノーツがあると、そのノーツが判定されず曲が終わらない
        foreach (IEnumerable<INoteData> noteDataList in chartData.AllNoteDataLists)
        {
            foreach (INoteData noteData in noteDataList)
            {
                if (float.IsNaN(noteData.Timing) || float.IsInfinity(noteData.Timing))
                {
                    reason = $"{noteData.NoteType} のタイミングを計算できません (BPM が 0 の可能性があります)";
                    return false;
                }
            }
        }

        reason = null;
        return true;
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

        judgementWindowAdjuster.AdjustJudgementWindow(chart, judgementSettingsCatalog, currentDifficulty);
    }
}
