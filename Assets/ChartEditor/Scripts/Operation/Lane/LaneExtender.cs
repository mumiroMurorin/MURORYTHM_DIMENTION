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
            // ‰„’·
            if(delta > 0)
            {
                ExtendChart(delta);
            }
            // ’Zk
            else
            {
                ShortenChart(Mathf.Abs(delta));
            }
        }

        /// <summary>
        /// •ˆ–Ê‚Ì‰„’·
        /// </summary>
        /// <param name="length"></param>
        private void ExtendChart(int length)
        {
            // ‰„’·
            Record(() => {
                dataSetter.ChangeChartLength(length);
            },
            // ’Zk
            () => {
                dataSetter.ChangeChartLength(-length);
            });
        }

        /// <summary>
        /// •ˆ–Ê‚Ì’Zk
        /// </summary>
        /// <param name="length"></param>
        private void ShortenChart(int length)
        {
            // Á‚·ƒm[ƒc‚ğ‹L˜^
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

            // ’Zk
            Record(() => {
                dataSetter.ChangeChartLength(-length);
            },
            // ‰„’·
            () => {
                dataSetter.ChangeChartLength(length);

                // ƒm[ƒc‚ğ’Ç‰Á
                foreach(var note in destroyNoteList)
                {
                    dataGetter.ChartData.Value.AddNote(note);
                }
            });
        }
    }

}