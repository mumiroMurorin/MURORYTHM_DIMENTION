using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class ScoreCalculaterSetter : MonoBehaviour, IScoreCalculaterSetter
{
    IScoreSetter scoreSetter;
    IChartDataGetter chartDataGetter;

    [Inject]
    public void Constructor(IScoreSetter scoreSetter, IChartDataGetter chartDataGetter)
    {
        this.scoreSetter = scoreSetter;
        this.chartDataGetter = chartDataGetter;
    }

    public void SetScoreCalculater()
    {
        scoreSetter?.SetScoreCalculater(new ScoreCalculater(chartDataGetter.Chart.MaxCombo));
    }
}

public interface IScoreCalculaterSetter
{
    void SetScoreCalculater();
}
