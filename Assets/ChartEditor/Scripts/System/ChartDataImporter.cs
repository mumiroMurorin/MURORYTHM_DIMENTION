using UnityEngine;
using VContainer;
using JsonUtil;
using ChartConvert;
using static UndoRedo.History;

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
            // Load from file dialog and keep the path for subsequent Ctrl+S overwrite.
            if (!JsonLoader.TryLoadFromJsonFileDialog(out ChartDataOrigin chartDataOrigin, out string loadedPath)) { return; }

            ChartImporterForChartEditor chartImporter = new ChartImporterForChartEditor();

            ChartData chartData = new ChartData(0);
            editorDataSetter.SetChartData(chartData);
            chartImporter.Import(chartDataOrigin, ref chartData, editorDataSetter);
            ChartFilePathCache.CurrentChartFilePath = loadedPath;

            // Reset undo/redo history after import.
            ResetStates();
        }
    }
}