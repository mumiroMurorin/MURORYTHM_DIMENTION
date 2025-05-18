using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class ScoreRecorder : MonoBehaviour
{
    IMusicDataGetter musicDataGetter;
    IScoreGetter scoreGetter;

    [Inject]
    public void Construct(IScoreGetter scoreGetter, IMusicDataGetter musicDataGetter)
    {
        this.musicDataGetter = musicDataGetter;
        this.scoreGetter = scoreGetter;
    }

    private void Start()
    {
        RecordScore();
    }

    private void RecordScore()
    {
        var judgementCount = new int[4] {
            scoreGetter.PerfectNum.Value,
            scoreGetter.GreatNum.Value,
            scoreGetter.GoodNum.Value,
            scoreGetter.MissNum.Value
        };

        var record = new MusicRecord
        {
             ComboRank = scoreGetter.CurrentComboRank.Value,
             Score = (int)scoreGetter.Score.Value,
             ScoreRank = scoreGetter.CurrentScoreRank.Value,
             JudgementCount = judgementCount
        };

        musicDataGetter.Music.Value.SetMusicRecord(musicDataGetter.Difficulty.Value, record);
    }
}
