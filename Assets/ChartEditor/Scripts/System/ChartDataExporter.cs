using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using ChartConvert;
using static JsonUtil.JsonWriter;

namespace ChartEditor
{
    public class ChartDataExporter : MonoBehaviour
    {
        IChartEditorDataGetter editorDataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter editorDataGetter)
        {
            this.editorDataGetter = editorDataGetter;
        }

        public void Export()
        {
            ChartExporter chartExporter = new ChartExporter();

            ChartDataOrigin chartDataOrigin = chartExporter.Export(editorDataGetter.ChartData.Value, editorDataGetter.Offset.Value);

            TrySaveToJsonFileDialog(chartDataOrigin);
        }
    }

}
