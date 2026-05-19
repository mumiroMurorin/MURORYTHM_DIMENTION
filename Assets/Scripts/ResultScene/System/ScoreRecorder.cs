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
        var judgementCount = new JudgementToCount(
                scoreGetter.PerfectNum.Value,
                scoreGetter.GreatNum.Value,
                scoreGetter.GoodNum.Value,
                scoreGetter.MissNum.Value
            );

        var score = (int)scoreGetter.Score.Value;
        var comboRank = scoreGetter.CurrentComboRank.Value;
        var scoreRank = scoreGetter.CurrentScoreRank.Value;
        var record = new MusicRecord(score, scoreRank,comboRank, judgementCount);

        var musicData = musicDataGetter.Music.Value;
        var difficulty = musicDataGetter.Difficulty.Value;

        musicData.SetMusicRecord(difficulty, record);
        MusicRecordPersistence.SaveIfBetter(musicData, difficulty, record);
    }
}
