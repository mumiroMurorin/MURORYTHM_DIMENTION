using System.Collections.Generic;

public static class ScoreRankUtility
{
    static readonly List<RankToThreshold> RankThresholds = new List<RankToThreshold>
    {
        new RankToThreshold(ScoreRank.SSS, 1000000),
        new RankToThreshold(ScoreRank.SSPlus, 995000),
        new RankToThreshold(ScoreRank.SS, 990000),
        new RankToThreshold(ScoreRank.SPlus, 985000),
        new RankToThreshold(ScoreRank.S, 980000),
        new RankToThreshold(ScoreRank.APlus, 970000),
        new RankToThreshold(ScoreRank.A, 950000),
        new RankToThreshold(ScoreRank.B, 900000),
        new RankToThreshold(ScoreRank.C, 750000),
        new RankToThreshold(ScoreRank.D, 500000),
        new RankToThreshold(ScoreRank.E, 0),
    };

    public static ScoreRank GetRankFromScore(int score)
    {
        for (int i = 0; i < RankThresholds.Count; i++)
        {
            if (RankThresholds[i].Threshold > score) { continue; }
            return RankThresholds[i].Rank;
        }

        return ScoreRank.E;
    }

    readonly struct RankToThreshold
    {
        public RankToThreshold(ScoreRank rank, int threshold)
        {
            Rank = rank;
            Threshold = threshold;
        }

        public ScoreRank Rank { get; }
        public int Threshold { get; }
    }
}
