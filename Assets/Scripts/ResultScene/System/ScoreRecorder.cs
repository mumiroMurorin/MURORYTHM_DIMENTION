using UnityEngine;
using VContainer;
using UIInResultScene;

public class ScoreRecorder : MonoBehaviour
{
    [SerializeField] ResultRankingListView rankingListView;
    [SerializeField] ResultUIPresenter resultUIPresenter;

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
        var newRecordState = GetNewRecordState(musicData, difficulty, score, scoreRank);

        musicData.SetMusicRecord(difficulty, record);
        MusicRecordPersistence.SaveIfBetter(musicData, difficulty, record);
        resultUIPresenter?.SetNewRecordActive(newRecordState.IsScoreNewRecord, newRecordState.IsScoreRankNewRecord);
        rankingListView?.ShowRanking(musicData, difficulty, record);
    }

    NewRecordState GetNewRecordState(MusicData musicData, Difficulty difficulty, int score, ScoreRank scoreRank)
    {
        if (musicData == null || string.IsNullOrWhiteSpace(musicData.MusicName))
        {
            return NewRecordState.NotNewRecord;
        }

        string chartKey = MusicRecordPersistence.MakeChartKey(musicData.MusicName, difficulty);
        if (!MusicRecordPersistence.TryGetSavedRecord(chartKey, out var savedRecord))
        {
            return NewRecordState.NewRecord;
        }

        var savedScoreRank = ScoreRankUtility.GetRankFromScore(savedRecord.score);
        return new NewRecordState(
            score > savedRecord.score,
            score != savedRecord.score && scoreRank > savedScoreRank
        );
    }

    readonly struct NewRecordState
    {
        public NewRecordState(bool isScoreNewRecord, bool isScoreRankNewRecord)
        {
            IsScoreNewRecord = isScoreNewRecord;
            IsScoreRankNewRecord = isScoreRankNewRecord;
        }

        public bool IsScoreNewRecord { get; }
        public bool IsScoreRankNewRecord { get; }

        public static NewRecordState NewRecord { get; } = new NewRecordState(true, true);
        public static NewRecordState NotNewRecord { get; } = new NewRecordState(false, false);
    }
}
