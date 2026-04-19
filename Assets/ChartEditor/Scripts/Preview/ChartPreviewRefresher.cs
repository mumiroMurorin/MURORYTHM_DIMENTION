using ChartConvert;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class ChartPreviewRefresher : MonoBehaviour, IChartPreviewRefreshable
    {
        [SerializeField] ChartPreviewGenerator previewGenerator;
        [SerializeField] GroundControllerPreview groundController;

        INoteSpawnDataOptionHolder optionHolder;

        [Inject]
        public void Constructor(INoteSpawnDataOptionHolder optionHolder)
        {
            this.optionHolder = optionHolder;
        }

        public void RefreshPreview(ChartDataOrigin savedChartData, string savedFilePath)
        {
            if (savedChartData == null)
            {
                Debug.LogWarning("[ChartPreviewRefresher] Saved chart data is null.");
                return;
            }

            if (previewGenerator == null)
            {
                Debug.LogWarning("[ChartPreviewRefresher] Preview generator is not assigned.");
                return;
            }

            if (optionHolder == null)
            {
                Debug.LogWarning("[ChartPreviewRefresher] Spawn option holder is not assigned.");
                return;
            }

            ChartImporterForRhythmGame importer = new ChartImporterForRhythmGame();
            global::ChartData chartData = importer.Import(savedChartData, optionHolder);

            previewGenerator.SetChartData(chartData);
            groundController.SetChartData(chartData);
            previewGenerator.DestroyChart();
            previewGenerator.Generate();
        }
    }
}
