using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;
using System;
using TransitionerInRhythmGameScene;

public class ChartEnder : MonoBehaviour, IChartEnder
{
    [SerializeField] float delaySeconds = 0f;
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
            .Delay(System.TimeSpan.FromSeconds(delaySeconds))
            .Where(count => count >= chartDataGetter.Chart.MaxCombo)
            .Take(1)    // 譜面の終了は1回だけ (二重に終了処理へ進まないようにする)
            .Subscribe(count =>
            {
                callback?.Invoke();
            })
            .AddTo(this.gameObject);
    }
}
