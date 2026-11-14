using System;

namespace ChartEditor
{
    /// <summary>
    /// 譜面に置かれているノーツの数などを数えた結果
    /// </summary>
    public struct ChartStatisticsData : IEquatable<ChartStatisticsData>
    {
        // --- ノーツの種類ごとの数 (配置ノーツの一覧と同じ分け方) ---
        public int Touch;
        public int DynamicUpward;
        public int DynamicDownward;
        public int DynamicRightward;
        public int DynamicLeftward;
        /// <summary>
        /// ホールドの本数 (始点の数。どこにもつながっていないホールドも1本と数える)
        /// </summary>
        public int Hold;
        /// <summary>
        /// スペースホールドの本数 (始点の数)
        /// </summary>
        public int SpaceHold;
        public int SpaceBreak;

        // --- 譜面そのものの情報 ---
        public int BarCount;
        public float MinBpm;
        public float MaxBpm;

        public bool Equals(ChartStatisticsData other)
        {
            return Touch == other.Touch &&
                   DynamicUpward == other.DynamicUpward &&
                   DynamicDownward == other.DynamicDownward &&
                   DynamicRightward == other.DynamicRightward &&
                   DynamicLeftward == other.DynamicLeftward &&
                   Hold == other.Hold &&
                   SpaceHold == other.SpaceHold &&
                   SpaceBreak == other.SpaceBreak &&
                   BarCount == other.BarCount &&
                   MinBpm == other.MinBpm &&
                   MaxBpm == other.MaxBpm;
        }

        public override bool Equals(object obj)
        {
            return obj is ChartStatisticsData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                HashCode.Combine(Touch, DynamicUpward, DynamicDownward, DynamicRightward, DynamicLeftward),
                HashCode.Combine(Hold, SpaceHold, SpaceBreak),
                HashCode.Combine(BarCount, MinBpm, MaxBpm));
        }
    }

    /// <summary>
    /// 編集中の譜面から、ノーツの種類ごとの数などを数える
    /// </summary>
    public static class ChartStatistics
    {
        /// <summary>
        /// 譜面全体を数える
        /// (何度も呼ばれるので、数えるたびにメモリを確保しないよう、番号で1つずつ取り出して数えている)
        /// </summary>
        public static ChartStatisticsData Count(ChartData chartData)
        {
            var result = new ChartStatisticsData();
            if (chartData == null) { return result; }

            var bars = chartData.BarDatas;
            result.BarCount = bars.Count;

            bool hasBpm = false;

            for (int barIndex = 0; barIndex < bars.Count; barIndex++)
            {
                var subDivisions = bars[barIndex].SubDivisionDatas;
                for (int subIndex = 0; subIndex < subDivisions.Count; subIndex++)
                {
                    var subDivision = subDivisions[subIndex];

                    // BPM のいちばん遅いところと、いちばん速いところ
                    float bpm = subDivision.Bpm.Value;
                    if (!hasBpm)
                    {
                        hasBpm = true;
                        result.MinBpm = bpm;
                        result.MaxBpm = bpm;
                    }
                    else
                    {
                        if (bpm < result.MinBpm) { result.MinBpm = bpm; }
                        if (bpm > result.MaxBpm) { result.MaxBpm = bpm; }
                    }

                    var notes = subDivision.NoteDatas;
                    for (int noteIndex = 0; noteIndex < notes.Count; noteIndex++)
                    {
                        AddNote(ref result, notes[noteIndex].NoteType);
                    }
                }
            }

            return result;
        }

        private static void AddNote(ref ChartStatisticsData result, DeploymentNoteType noteType)
        {
            switch (noteType)
            {
                case DeploymentNoteType.Touch:
                case DeploymentNoteType.DivineTouch:
                    result.Touch++;
                    break;

                case DeploymentNoteType.DynamicGroundUpward:
                    result.DynamicUpward++;
                    break;
                case DeploymentNoteType.DynamicGroundDownward:
                    result.DynamicDownward++;
                    break;
                case DeploymentNoteType.DynamicGroundRightward:
                    result.DynamicRightward++;
                    break;
                case DeploymentNoteType.DynamicGroundLeftward:
                    result.DynamicLeftward++;
                    break;

                // ホールドは、中継点や終点ではなく「本数」を数える。1本につき始点は1つ
                case DeploymentNoteType.HoldStart:
                case DeploymentNoteType.DivineHoldStart:
                    result.Hold++;
                    break;

                case DeploymentNoteType.SpaceHoldStart:
                    result.SpaceHold++;
                    break;

                case DeploymentNoteType.SpaceBreak:
                    result.SpaceBreak++;
                    break;
            }
        }
    }
}
