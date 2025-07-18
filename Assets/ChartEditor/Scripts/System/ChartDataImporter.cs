using System.Collections;
using System.Collections.Generic;
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
            // ダイアログから選択
            if(!JsonLoader.TryLoadFromJsonFileDialog(out ChartDataOrigin chartDataOrigin)) { return; }

            ChartImporterForChartEditor chartImporter = new ChartImporterForChartEditor();

            ChartData chartData = new ChartData(0);
            editorDataSetter.SetChartData(chartData);
            chartImporter.Import(chartDataOrigin, ref chartData, editorDataSetter);

            // RedoUndoリセット
            ResetStates();
        }
    }

}
