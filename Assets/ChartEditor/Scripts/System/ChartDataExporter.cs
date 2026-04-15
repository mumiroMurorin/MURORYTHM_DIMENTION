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

            // Overwrite the current chart file when a save target is already known.
            if (!string.IsNullOrWhiteSpace(ChartFilePathCache.CurrentChartFilePath))
            {
                bool isSaved = TrySaveToJsonPath(chartDataOrigin, ChartFilePathCache.CurrentChartFilePath);
                if (isSaved) { return; }
            }

            // Fall back to Save As dialog when no target path exists or overwrite failed.
            if (TrySaveToJsonFileDialog(chartDataOrigin, out string savedPath))
            {
                ChartFilePathCache.CurrentChartFilePath = savedPath;
            }
        }

        public void ExportNewFile()
        {
            ChartExporter chartExporter = new ChartExporter();
            ChartDataOrigin chartDataOrigin = chartExporter.Export(editorDataGetter.ChartData.Value, editorDataGetter.Offset.Value);

            if (TrySaveToJsonFileDialog(chartDataOrigin, out string savedPath))
            {
                ChartFilePathCache.CurrentChartFilePath = savedPath;
            }
        }
    }
}