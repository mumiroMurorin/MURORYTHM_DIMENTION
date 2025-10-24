using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

public class ChartControllerInSelectScene : MonoBehaviour
{
    [Header("オプション用譜面")]
    [SerializeField] TextAsset chartJson;
    [SerializeField] SerializeInterface<IChartLoader> chartLoader;

    [Inject] IJudgementRecorder judgementRecorder;
    [Inject] IOptionGetter optionGetter;
    [Inject] IOptionSetter optionSetter;

    [SerializeField] ScoreSetterInSelectScene scoreSetter;
    [SerializeField] SerializeInterface<ITimeController> timeController;
    [SerializeField] SerializeInterface<IChartGenerator> chartGenerator;
    [SerializeField] SerializeInterface<IChartEnder> chartEnder;

    public ChartData ChartData { get; private set; }

    void Start()
    {
        optionSetter?.SetAutoMode(true);

        Bind();
    }

    private void Bind()
    {
        // オフセットが変わった際はリセット
        optionGetter?.OffsetMs
            .Subscribe(_ => ReloadChart())
            .AddTo(this.gameObject);

        // ノートスピードが変わった際はリセット
        optionGetter?.NoteSpeed
            .Subscribe(_ => RestartChart())
            .AddTo(this.gameObject);

        // 譜面終了時リセット
        chartEnder?.Value?.BindOnEndChart(() => RestartChart());
    }

    private void RestartChart()
    {
        scoreSetter?.Initialize();
        timeController?.Value?.ResetTimer();
        timeController?.Value?.StartTimer();
        chartGenerator?.Value?.Generate();
    }

    private void ReloadChart()
    {
        ChartData = chartLoader.Value.LoadChartData(chartJson);
        RestartChart();
    }

    private void OnDestroy()
    {
        optionSetter?.SetAutoMode(false);
    }
}
