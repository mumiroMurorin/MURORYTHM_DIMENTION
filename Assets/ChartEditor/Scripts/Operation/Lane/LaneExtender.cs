using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;
using static UndoRedo.History;

namespace ChartEditor
{
    public class LaneExtender : MonoBehaviour
    {
        IChartEditorDataSetter dataSetter;
        IChartEditorDataGetter dataGetter;

        [Inject]
        public void Construct(IChartEditorDataSetter dataSetter, IChartEditorDataGetter dataGetter)
        {
            this.dataSetter = dataSetter;
            this.dataGetter = dataGetter;
        }

        public void ChangeChartLength(int delta)
        {
            // 譜面の読み込み中は受け付けない
            if (dataGetter.IsLoadingChart.Value) { return; }

            // 延長
            if(delta > 0)
            {
                ExtendChart(delta);
            }
            // 短縮
            else
            {
                ShortenChart(Mathf.Abs(delta));
            }
        }

        /// <summary>
        /// 譜面の延長
        /// </summary>
        /// <param name="length"></param>
        private void ExtendChart(int length)
        {
            // 延長
            Record(() => {
                dataSetter.ChangeChartLength(length);
            },
            // 短縮
            () => {
                dataSetter.ChangeChartLength(-length);
            });
        }

        /// <summary>
        /// 譜面の短縮
        /// </summary>
        /// <param name="length"></param>
        private void ShortenChart(int length)
        {
            // 譜面が未作成、または小節が無いときは何もしない
            var chartData = dataGetter.ChartData.Value;
            if (chartData == null || chartData.BarDatas.Count <= 0) { return; }

            int count = chartData.BarDatas.Count;
            length = Mathf.Min(length, count);
            if (length <= 0) { return; }

            // 消す小節の設定とノーツを記録 (小節番号の若い順)
            var removedBars = new List<BarSnapshot>();
            for (int i = count - length; i < count; i++)
            {
                removedBars.Add(new BarSnapshot(chartData.BarDatas[i]));
            }

            // 短縮
            Record(() => {
                dataSetter.ChangeChartLength(-length);
            },
            // 延長して、消した小節の設定とノーツを元に戻す
            () => {
                dataSetter.ChangeChartLength(length);

                var chart = dataGetter.ChartData.Value;
                int firstIndex = chart.BarDatas.Count - removedBars.Count;
                for (int i = 0; i < removedBars.Count; i++)
                {
                    removedBars[i].Restore(chart, firstIndex + i);
                }
            });
        }

        /// <summary>
        /// 削除される小節の状態 (拍子・分割数、分線ごとのBPM・速度倍率、ノーツ)
        /// </summary>
        private class BarSnapshot
        {
            readonly BarConfig barConfig;
            readonly List<SubdivisionConfig> subConfigs = new List<SubdivisionConfig>();
            readonly List<IDeployableNoteData> notes = new List<IDeployableNoteData>();

            public BarSnapshot(BarDataInChart barData)
            {
                barConfig = barData.BarConfig;

                foreach (var sub in barData.SubDivisionDatas)
                {
                    subConfigs.Add(sub.SubConfig);

                    foreach (var note in sub.NoteDatas)
                    {
                        notes.Add(note);
                    }
                }
            }

            public void Restore(ChartData chartData, int barIndex)
            {
                if (barIndex < 0 || barIndex >= chartData.BarDatas.Count) { return; }

                // 拍子・分割数 (分線が作り直される)
                var barData = chartData.BarDatas[barIndex];
                barData.SetBeatCount(barConfig.BeatCount);
                barData.SetBeatUnit(barConfig.BeatUnit);
                barData.SetDivisionNum(barConfig.DivisionNum);

                // 分線ごとのBPM・速度倍率
                for (int i = 0; i < subConfigs.Count && i < barData.SubDivisionDatas.Count; i++)
                {
                    barData.SubDivisionDatas[i].SetBpm(subConfigs[i].Bpm);
                    barData.SubDivisionDatas[i].SetSpeedRatio(subConfigs[i].SpeedRatio);
                }

                // BPMを直接書き換えたので、線の位置を更新させる
                chartData.NotifyBpmChanged(barIndex, 0);

                // ノーツ
                foreach (var note in notes)
                {
                    chartData.AddNote(note);
                }
            }
        }
    }

}
