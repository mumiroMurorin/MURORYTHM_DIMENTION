using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using JsonUtil;
using ChartConvert;

namespace ChartEditor
{
    public class ChartDataImporter : MonoBehaviour
    {
        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter editorDataSetter;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter editorDataSetter)
        {
            this.dataGetter = dataGetter;
            this.editorDataSetter = editorDataSetter;
        }

        public void Import()
        {
            // ダイアログから選択
            if(!JsonLoader.TryLoadFromJsonFileDialog(out ChartDataOrigin chartDataOrigin)) { return; }

            ChartImporterForChartEditor chartImporter = new ChartImporterForChartEditor();

            ChartData chartData = new ChartData(0);
            editorDataSetter.SetChartData(chartData);
            chartImporter.Import(chartDataOrigin, ref chartData, editorDataSetter);

            // ノーツの配置
            DeployNote(chartData);
        }

        /// <summary>
        /// ノーツの配置をChartDataを参照して行う
        /// </summary>
        /// <param name="chartData"></param>
        private void DeployNote(ChartData chartData)
        {
            foreach(var barData in chartData.BarDatas)
            {
                foreach(var subData in barData.SubDivisionDatas)
                {
                    foreach(var noteData in subData.NoteDatas)
                    {
                        dataGetter.ChartData.Value.AddNote(noteData);
                    }
                }
            }
        }
    }

}
