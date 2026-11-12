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
            if (dataGetter.ChartData.Value.BarDatas.Count <= 0) { return; }

            // 消すノーツを記録
            int count = dataGetter.ChartData.Value.BarDatas.Count;
            var destroyNoteList = new List<IDeployableNoteData>();

            for (int i = 0; i < length; i++)
            {
                foreach (var sub in dataGetter.ChartData.Value.BarDatas[count - i - 1].SubDivisionDatas)
                {
                    foreach(var note in sub.NoteDatas)
                    {
                        destroyNoteList.Add(note);
                    }
                }
            }

            // 短縮
            Record(() => {
                dataSetter.ChangeChartLength(-length);
            },
            // 延長
            () => {
                dataSetter.ChangeChartLength(length);

                // ノーツを追加
                foreach(var note in destroyNoteList)
                {
                    dataGetter.ChartData.Value.AddNote(note);
                }
            });
        }
    }

}