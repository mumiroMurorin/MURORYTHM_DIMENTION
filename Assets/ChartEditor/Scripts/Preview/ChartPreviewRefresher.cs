using ChartConvert;
using UnityEngine;

namespace ChartEditor
{
    public class ChartPreviewRefresher : MonoBehaviour, IChartPreviewRefreshable
    {
        [SerializeField] ChartPreviewGenerator previewGenerator;
        [SerializeField] SerializeInterface<INoteSpawnDataOptionHolder> spawnOptionHolder;

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

            if (spawnOptionHolder == null || spawnOptionHolder.Value == null)
            {
                Debug.LogWarning("[ChartPreviewRefresher] Spawn option holder is not assigned.");
                return;
            }

            ChartImporterForRhythmGame importer = new ChartImporterForRhythmGame();
            global::ChartData chartData = importer.Import(savedChartData, spawnOptionHolder.Value);

            previewGenerator.SetChartData(chartData);
            previewGenerator.DestroyChart();
            previewGenerator.Generate();
        }
    }
}
