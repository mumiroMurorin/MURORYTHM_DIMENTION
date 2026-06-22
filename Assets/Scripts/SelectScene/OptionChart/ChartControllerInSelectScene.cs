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

    IOptionGetter optionGetter;
    INoteSpawnDataOptionSetter spawnDataSetter;

    [SerializeField] ScoreSetterInSelectScene scoreSetter;
    [SerializeField] GroundControllerLinear groundController; 
    [SerializeField] SerializeInterface<TransitionerInSelectScene.IPhaseStatusGetterInSelectScene> statusGetter;
    [SerializeField] SerializeInterface<ITimeController> timeController;
    [SerializeField] SerializeInterface<IChartGenerator> chartGenerator;
    [SerializeField] SerializeInterface<IChartEnder> chartEnder;

    public ChartData ChartData { get; private set; }

    [Inject]
    public void Construct(IOptionGetter optionGetter, INoteSpawnDataOptionSetter spawnDataSetter)
    {
        this.optionGetter = optionGetter;
        this.spawnDataSetter = spawnDataSetter;
    }

    void Start()
    {
        spawnDataSetter?.SetAutoMode(true);
        groundController.Initialize();

        Bind();
    }

    private void Bind()
    {
        // ステータスがオプションのときのみ譜面を動かす
        statusGetter?.Value?.PhaseStatus
            .Where(status => status == PhaseStatusInSelectScene.MusicOption)
            .Subscribe(_ => { timeController?.Value?.StartTimer(); })
            .AddTo(this.gameObject);

        statusGetter?.Value?.PhaseStatus
            .Where(status => status != PhaseStatusInSelectScene.MusicOption)
            .Subscribe(_ => { timeController?.Value?.StopTimer(); })
            .AddTo(this.gameObject);

        // オフセットが変わった際はリセット
        optionGetter?.OffsetMs
            .Subscribe(_ => ReloadChart())
            .AddTo(this.gameObject);

        // ノートスピードが変わった際はリセット
        optionGetter?.NoteSpeed
            .Subscribe(_ => RestartChart())
            .AddTo(this.gameObject);

        // 【ノーツ軌道】半径を変更した場合は円弧メッシュを作り直す
        optionGetter?.NoteCurveRadius
            .Skip(1)
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
        spawnDataSetter?.SetAutoMode(false);
    }
}
