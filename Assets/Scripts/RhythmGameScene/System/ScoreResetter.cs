using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class ScoreResetter : MonoBehaviour, IScoreResetter
{
    IScoreSetter scoreSetter;

    [Inject]
    public void Constructor(IScoreSetter scoreSetter)
    {
        this.scoreSetter = scoreSetter;
    }

    public void ResetScore()
    {
        scoreSetter.ResetScore();
    }
}

public interface IScoreResetter
{
    void ResetScore();
}
