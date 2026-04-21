using UnityEngine;
using UnityEngine.Events;

namespace ChartEditor
{
    public class ChartPreviewUpdaterOnSave : MonoBehaviour
    {
        [SerializeField] bool refreshOnSave;
        [SerializeField] ChartDataExporter chartDataExporter;
        [SerializeField] MonoBehaviour previewRefreshTarget;

        IChartPreviewRefreshable previewRefreshable;

        private void Awake()
        {
            previewRefreshable = previewRefreshTarget as IChartPreviewRefreshable;
            if (previewRefreshTarget != null && previewRefreshable == null)
            {
                Debug.LogWarning($"[{nameof(ChartPreviewUpdaterOnSave)}] previewRefreshTarget must implement IChartPreviewRefreshable.");
            }
        }

        private void OnEnable()
        {
            if (!refreshOnSave) { return; }

            if (chartDataExporter != null)
            {
                chartDataExporter.OnChartSaved += OnChartSaved;
            }
        }

        private void OnDisable()
        {
            if (!refreshOnSave) { return; }

            if (chartDataExporter != null)
            {
                chartDataExporter.OnChartSaved -= OnChartSaved;
            }
        }

        private void OnChartSaved(ChartConvert.ChartDataOrigin savedChartData, string savedFilePath)
        {
            previewRefreshable?.RefreshPreview(savedChartData, savedFilePath);
        }
    }
}
