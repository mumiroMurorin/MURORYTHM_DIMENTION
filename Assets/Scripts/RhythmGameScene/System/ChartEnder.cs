using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;
using System;
using TransitionerInRhythmGameScene;

public class ChartEnder : MonoBehaviour, IChartEnder
{
    [Header("フェーズ遷移管理")]
    [SerializeField] SerializeInterface<IPhaseTransitionableInRhythmGameScene> phaseTransitionable;

    IScoreGetter scoreGetter;
    IChartDataGetter chartDataGetter;

    [Inject]
    public void Constructor(IScoreGetter scoreGetter, IChartDataGetter chartDataGetter)
    {
        this.scoreGetter = scoreGetter;
        this.chartDataGetter = chartDataGetter;
    }

    void IChartEnder.BindOnEndChart(Action callback)
    {
        // 譜面終了処理を購読
        scoreGetter.NoteJudgementDatas
            .ObserveCountChanged()
            .Where(count => count >= chartDataGetter.Chart.MaxCombo)
            .Subscribe(count =>
            {
                callback?.Invoke();
                OnEndChart();
            })
            .AddTo(this.gameObject);
    }

    private void OnEndChart()
    {
        phaseTransitionable.Value.TransitionPhase(PhaseStatusInRhythmGame.EndAnimation);
    }
}
