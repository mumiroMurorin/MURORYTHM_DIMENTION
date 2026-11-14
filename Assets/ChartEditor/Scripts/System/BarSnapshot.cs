using System.Collections.Generic;

namespace ChartEditor
{
    /// <summary>
    /// 小節1つぶんの状態の写し (拍子・分割数、分線ごとのBPM・スピード倍率、乗っているノーツとその位置)
    ///
    /// 小節を消したり、小節の拍子・分割数を変えたりする操作を、元に戻す・やり直すために使う。
    /// 戻すときは「何番目の小節か」で相手を決める。小節のデータそのものを覚えておくと、その小節が
    /// 一度消されて作り直された後 (短縮して元に戻した後など) に、もう無い小節を相手にしてしまう。
    /// </summary>
    public class BarSnapshot
    {
        readonly BarConfig barConfig;
        readonly List<SubdivisionConfig> subConfigs = new List<SubdivisionConfig>();
        // ノーツと、写しを取ったときの位置 (ノーツは後から動かされることがあるので、位置は別に写しておく)
        readonly List<NoteDataToAddress> notes = new List<NoteDataToAddress>();

        public BarSnapshot(BarDataInChart barData)
        {
            barConfig = barData.BarConfig;

            foreach (var sub in barData.SubDivisionDatas)
            {
                subConfigs.Add(sub.SubConfig);

                foreach (var note in sub.NoteDatas)
                {
                    notes.Add(new NoteDataToAddress(note, note.Address));
                }
            }
        }

        /// <summary>
        /// 指定した番号の小節を、写しを取ったときの状態にする
        /// </summary>
        public void Restore(ChartData chartData, int barIndex)
        {
            if (chartData == null || barIndex < 0 || barIndex >= chartData.BarDatas.Count) { return; }

            var barData = chartData.BarDatas[barIndex];

            // 今その小節に乗っているノーツを、いったん譜面から外す
            // (この後で分線を作り直すので、乗せたままにすると行き場が無くなる)
            for (int subIndex = 0; subIndex < barData.SubDivisionDatas.Count; subIndex++)
            {
                var noteDatas = barData.SubDivisionDatas[subIndex].NoteDatas;
                for (int i = noteDatas.Count - 1; i >= 0; i--)
                {
                    chartData.RemoveNote(noteDatas[i], false);
                }
            }

            // 拍子・分割数 (分線が作り直される)
            barData.SetBeatCount(barConfig.BeatCount);
            barData.SetBeatUnit(barConfig.BeatUnit);
            barData.SetDivisionNum(barConfig.DivisionNum);

            // 分線ごとのBPM・スピード倍率
            for (int i = 0; i < subConfigs.Count && i < barData.SubDivisionDatas.Count; i++)
            {
                barData.SubDivisionDatas[i].SetBpm(subConfigs[i].Bpm);
                barData.SubDivisionDatas[i].SetSpeedRatio(subConfigs[i].SpeedRatio);
            }

            // BPMを直接書き換えたので、線の位置を更新させる
            chartData.NotifyBpmChanged(barIndex, 0);

            // ノーツを、写しを取ったときの位置に戻す
            foreach (var pair in notes)
            {
                pair.NoteData.SetAddress(pair.Address);
                chartData.AddNote(pair.NoteData);
            }
        }
    }
}
