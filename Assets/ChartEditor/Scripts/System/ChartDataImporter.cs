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
        IChartEditorDataGetter editorDataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter editorDataGetter)
        {
            this.editorDataGetter = editorDataGetter;
        }

        public void Import()
        {
            // ダイアログから選択
            if(!JsonLoader.TryLoadFromJsonFileDialog(out ChartDataOrigin chartDataOrigin)) { return; }

            ChartImporterForChartEditor chartImporter = new ChartImporterForChartEditor();

            chartImporter.Import(chartDataOrigin);
        }
    }

}
