using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 譜面全体の縮小図 (全体マップ) を描く
    ///
    /// 縦が時間 (下が譜面の先頭、上が末尾)、横がレーン。ノーツ1つを、種類ごとの色の短い横線で描く。
    /// ホールドは、つながるノーツの間を薄い色で塗って、帯に見えるようにする。
    /// 譜面のどのあたりにノーツが固まっているかを、ひと目で見るためのもの (1つ1つのノーツを見分けるためのものではない)。
    /// </summary>
    public static class ChartOverviewImage
    {
        // レーンの数
        const int LANE_COUNT = 16;
        // スペースレイヤーのノーツを表すレーン番号
        const float SPACE_LANE_INDEX = 100f;
        // スペースのノーツを描く横の範囲 (レーン番号。スペースのノーツにはレーンが無いので、真ん中に帯として描く)
        const float SPACE_LEFT = 4f;
        const float SPACE_RIGHT = 12f;
        // ノーツ1つを描く高さ (ピクセル)
        const int NOTE_HEIGHT = 2;
        // 何小節ごとに、目盛りの線を引くか
        const int BAR_TICK_INTERVAL = 10;

        // ホールドの種類 (帯を塗るときに、グラウンドとスペースを分けるための番号)
        const int KIND_HOLD = 0;
        const int KIND_SPACE_HOLD = 1;

        static readonly Color32 clearColor = new Color32(0, 0, 0, 0);
        static readonly Color32 barTickColor = new Color32(255, 255, 255, 45);
        static readonly Color32 touchColor = new Color32(255, 200, 40, 255);
        static readonly Color32 dynamicUpColor = new Color32(255, 70, 70, 255);
        static readonly Color32 dynamicDownColor = new Color32(70, 130, 255, 255);
        static readonly Color32 dynamicSideColor = new Color32(255, 70, 220, 255);
        static readonly Color32 holdColor = new Color32(70, 120, 255, 255);
        static readonly Color32 holdBandColor = new Color32(70, 120, 255, 120);
        static readonly Color32 spaceHoldColor = new Color32(160, 140, 255, 255);
        static readonly Color32 spaceHoldBandColor = new Color32(160, 140, 255, 90);
        static readonly Color32 spaceBreakColor = new Color32(70, 220, 130, 255);

        /// <summary>
        /// つながるノーツ (ホールドの節) の位置
        /// </summary>
        private struct ChainNode
        {
            public int Kind;
            public int ChainIndex;
            public int Row;
            public float Left;
            public float Right;
        }

        // 作業用 (描くたびに確保しないよう使い回す)
        static readonly List<ChainNode> chainNodes = new List<ChainNode>();
        static readonly Comparison<ChainNode> compareChainNodes = CompareChainNodes;

        /// <summary>
        /// 譜面全体を描く
        /// </summary>
        /// <param name="pixels">描き込み先 (左下から右へ、1行ずつ上へ並ぶ。width × height 個以上)</param>
        /// <param name="width">横のピクセル数</param>
        /// <param name="height">縦のピクセル数</param>
        public static void Render(ChartData chartData, Color32[] pixels, int width, int height)
        {
            if (pixels == null || width <= 0 || height <= 0 || pixels.Length < width * height) { return; }

            for (int i = 0; i < width * height; i++) { pixels[i] = clearColor; }

            if (chartData == null) { return; }

            float totalSeconds = GetTotalSeconds(chartData);
            if (!(totalSeconds > 0f)) { return; }

            // 1周目: 小節の目盛りを描き、ホールドの節の位置を集める
            chainNodes.Clear();
            ForEachNote(chartData, totalSeconds, pixels, width, height, false);

            // ホールドの帯 (ノーツより先に描いて、ノーツの線が帯の上に乗るようにする)
            DrawChainBands(pixels, width, height);

            // 2周目: ノーツを描く
            ForEachNote(chartData, totalSeconds, pixels, width, height, true);

            chainNodes.Clear();
        }

        /// <summary>
        /// 譜面の先頭から順にたどり、小節の目盛りとノーツを処理する
        /// </summary>
        /// <param name="isDrawingNotes">true: ノーツを描く。false: 目盛りを描き、ホールドの節の位置を集める</param>
        private static void ForEachNote(ChartData chartData, float totalSeconds, Color32[] pixels, int width, int height, bool isDrawingNotes)
        {
            float seconds = 0f;
            var bars = chartData.BarDatas;

            for (int barIndex = 0; barIndex < bars.Count; barIndex++)
            {
                var bar = bars[barIndex];

                // 10小節目、20小節目…の先頭に、目盛りの線を引く
                if (!isDrawingNotes && barIndex > 0 && (barIndex + 1) % BAR_TICK_INTERVAL == 0)
                {
                    FillRow(pixels, width, height, GetRow(seconds, totalSeconds, height), 0, width - 1, barTickColor);
                }

                var subDivisions = bar.SubDivisionDatas;
                for (int subIndex = 0; subIndex < subDivisions.Count; subIndex++)
                {
                    var subDivision = subDivisions[subIndex];
                    int row = GetRow(seconds, totalSeconds, height);

                    var notes = subDivision.NoteDatas;
                    for (int noteIndex = 0; noteIndex < notes.Count; noteIndex++)
                    {
                        var note = notes[noteIndex];
                        var lanes = note.Address.RangeRP;
                        if (lanes.Count == 0) { continue; }

                        // 横の範囲 (レーン番号。右の端は、右端のレーンの次の番号)
                        bool isSpaceNote = lanes[0] == SPACE_LANE_INDEX;
                        float left = isSpaceNote ? SPACE_LEFT : lanes[0];
                        float right = isSpaceNote ? SPACE_RIGHT : lanes[lanes.Count - 1] + 1f;

                        if (isDrawingNotes)
                        {
                            DrawNote(pixels, width, height, row, left, right, GetNoteColor(note.NoteType));
                        }
                        else if (note is IChainNoteData chainNote && chainNote.ChainIndex.Value >= 0)
                        {
                            chainNodes.Add(new ChainNode
                            {
                                Kind = isSpaceNote ? KIND_SPACE_HOLD : KIND_HOLD,
                                ChainIndex = chainNote.ChainIndex.Value,
                                Row = row,
                                Left = left,
                                Right = right,
                            });
                        }
                    }

                    seconds += GetSubDivisionSeconds(bar, subDivision);
                }
            }
        }

        /// <summary>
        /// つながるノーツの間を、薄い色で塗る
        /// </summary>
        private static void DrawChainBands(Color32[] pixels, int width, int height)
        {
            // 種類・接続番号ごとに、時刻の順に並べる
            chainNodes.Sort(compareChainNodes);

            for (int i = 0; i + 1 < chainNodes.Count; i++)
            {
                ChainNode from = chainNodes[i];
                ChainNode to = chainNodes[i + 1];
                if (from.Kind != to.Kind || from.ChainIndex != to.ChainIndex) { continue; }

                Color32 color = from.Kind == KIND_SPACE_HOLD ? spaceHoldBandColor : holdBandColor;
                int rowCount = to.Row - from.Row;

                for (int row = from.Row; row <= to.Row; row++)
                {
                    // 節から節へ、横の範囲を少しずつ変える
                    float ratio = rowCount > 0 ? (float)(row - from.Row) / rowCount : 0f;
                    float left = Mathf.Lerp(from.Left, to.Left, ratio);
                    float right = Mathf.Lerp(from.Right, to.Right, ratio);

                    FillRow(pixels, width, height, row, GetLeftColumn(left, width), GetRightColumn(right, width), color);
                }
            }
        }

        private static int CompareChainNodes(ChainNode a, ChainNode b)
        {
            if (a.Kind != b.Kind) { return a.Kind.CompareTo(b.Kind); }
            if (a.ChainIndex != b.ChainIndex) { return a.ChainIndex.CompareTo(b.ChainIndex); }

            return a.Row.CompareTo(b.Row);
        }

        /// <summary>
        /// ノーツ1つを、短い横線で描く
        /// </summary>
        private static void DrawNote(Color32[] pixels, int width, int height, int row, float left, float right, Color32 color)
        {
            int leftColumn = GetLeftColumn(left, width);
            int rightColumn = GetRightColumn(right, width);

            for (int i = 0; i < NOTE_HEIGHT; i++)
            {
                FillRow(pixels, width, height, row + i, leftColumn, rightColumn, color);
            }
        }

        /// <summary>
        /// 1行のうち、指定した範囲の列を塗る (半透明の色は、下の色に重ねる)
        /// </summary>
        private static void FillRow(Color32[] pixels, int width, int height, int row, int leftColumn, int rightColumn, Color32 color)
        {
            if (row < 0 || row >= height) { return; }

            leftColumn = Mathf.Max(leftColumn, 0);
            rightColumn = Mathf.Min(rightColumn, width - 1);

            int offset = row * width;
            for (int column = leftColumn; column <= rightColumn; column++)
            {
                pixels[offset + column] = Blend(pixels[offset + column], color);
            }
        }

        /// <summary>
        /// 下の色に、上の色を重ねる
        /// </summary>
        private static Color32 Blend(Color32 under, Color32 over)
        {
            if (over.a == 255 || under.a == 0) { return over; }

            int alpha = over.a;
            int inverse = 255 - alpha;
            return new Color32(
                (byte)((over.r * alpha + under.r * inverse) / 255),
                (byte)((over.g * alpha + under.g * inverse) / 255),
                (byte)((over.b * alpha + under.b * inverse) / 255),
                (byte)Mathf.Max(under.a, over.a));
        }

        private static Color32 GetNoteColor(DeploymentNoteType noteType)
        {
            switch (noteType)
            {
                case DeploymentNoteType.DynamicGroundUpward:
                    return dynamicUpColor;
                case DeploymentNoteType.DynamicGroundDownward:
                    return dynamicDownColor;
                case DeploymentNoteType.DynamicGroundRightward:
                case DeploymentNoteType.DynamicGroundLeftward:
                    return dynamicSideColor;

                case DeploymentNoteType.HoldStart:
                case DeploymentNoteType.DivineHoldStart:
                case DeploymentNoteType.HoldRelay:
                case DeploymentNoteType.HoldMeshRelay:
                case DeploymentNoteType.HoldEnd:
                case DeploymentNoteType.HoldEndUnjudge:
                    return holdColor;

                case DeploymentNoteType.SpaceHoldStart:
                case DeploymentNoteType.SpaceHoldRelay:
                case DeploymentNoteType.SpaceHoldMeshRelay:
                case DeploymentNoteType.SpaceHoldEnd:
                    return spaceHoldColor;

                case DeploymentNoteType.SpaceBreak:
                    return spaceBreakColor;

                default:
                    return touchColor;
            }
        }

        /// <summary>
        /// 時刻が、下から何行目に当たるか
        /// </summary>
        private static int GetRow(float seconds, float totalSeconds, int height)
        {
            return Mathf.Clamp(Mathf.FloorToInt(seconds / totalSeconds * height), 0, height - 1);
        }

        /// <summary>
        /// レーン番号(左の端)が、左から何列目に当たるか
        /// </summary>
        private static int GetLeftColumn(float lane, int width)
        {
            return Mathf.Clamp(Mathf.FloorToInt(lane * width / LANE_COUNT), 0, width - 1);
        }

        /// <summary>
        /// レーン番号(右の端)が、左から何列目に当たるか (その列までを塗る)
        /// </summary>
        private static int GetRightColumn(float lane, int width)
        {
            return Mathf.Clamp(Mathf.CeilToInt(lane * width / LANE_COUNT) - 1, 0, width - 1);
        }

        #region 時刻

        /// <summary>
        /// 譜面全体の長さ(秒)
        /// </summary>
        public static float GetTotalSeconds(ChartData chartData)
        {
            return GetBarStartSeconds(chartData, null);
        }

        /// <summary>
        /// 小節ごとの、先頭の時刻(秒)を求める
        /// </summary>
        /// <param name="barStartSeconds">小節の先頭の時刻が、譜面の先頭の小節から順に入る。要らないときは null</param>
        /// <returns>譜面全体の長さ(秒)</returns>
        public static float GetBarStartSeconds(ChartData chartData, List<float> barStartSeconds)
        {
            if (barStartSeconds != null) { barStartSeconds.Clear(); }
            if (chartData == null) { return 0f; }

            float seconds = 0f;
            var bars = chartData.BarDatas;
            for (int barIndex = 0; barIndex < bars.Count; barIndex++)
            {
                if (barStartSeconds != null) { barStartSeconds.Add(seconds); }

                var bar = bars[barIndex];
                var subDivisions = bar.SubDivisionDatas;
                for (int subIndex = 0; subIndex < subDivisions.Count; subIndex++)
                {
                    seconds += GetSubDivisionSeconds(bar, subDivisions[subIndex]);
                }
            }

            return seconds;
        }

        /// <summary>
        /// 指定した時刻が、何番目の小節の中にあるか (0 始まり)
        /// </summary>
        /// <param name="barStartSeconds">小節ごとの先頭の時刻 (GetBarStartSeconds で求めたもの)</param>
        /// <returns>小節の番号。小節が1つも無ければ -1</returns>
        public static int FindBarIndex(List<float> barStartSeconds, float seconds)
        {
            if (barStartSeconds == null || barStartSeconds.Count == 0) { return -1; }

            // 先頭の時刻が seconds 以下の小節のうち、いちばん後ろのもの
            int low = 0;
            int high = barStartSeconds.Count - 1;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (barStartSeconds[middle] <= seconds) { low = middle; }
                else { high = middle - 1; }
            }

            return low;
        }

        /// <summary>
        /// 分線1本ぶんの長さ(秒)。編集画面のレーンで、線の間隔を決めているのと同じ式
        /// </summary>
        private static float GetSubDivisionSeconds(BarDataInChart bar, SubDivisionDataInBeat subDivision)
        {
            float bpm = subDivision.Bpm.Value;
            float beatUnit = bar.BeatUnit.Value;
            int divisionNum = bar.DivisionNum.Value;
            if (!(bpm > 0f) || !(beatUnit > 0f) || divisionNum <= 0) { return 0f; }

            return (60f / bpm) * (4f / beatUnit) / divisionNum;
        }

        #endregion
    }
}
