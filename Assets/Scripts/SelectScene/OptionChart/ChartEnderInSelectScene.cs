using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;
using System;
using TransitionerInRhythmGameScene;

public class ChartEnderInSelectScene : MonoBehaviour, IChartEnder
{
    [SerializeField] float delaySeconds = 1f;
    [SerializeField] ScoreSetterInSelectScene scoreSetter;
    [Inject] IScoreGetter scoreGetter;

    void IChartEnder.BindOnEndChart(Action callback)
    {
        // •ˆ–ÊI—¹ˆ—‚ðw“Ç
        scoreGetter.NoteJudgementDatas
            .ObserveCountChanged()
            .Where(count => count >= scoreSetter.AddNoteCount)
            .Delay(System.TimeSpan.FromSeconds(delaySeconds))
            .Subscribe(count =>
            {
                callback?.Invoke();
            })
            .AddTo(this.gameObject);
    }
}
