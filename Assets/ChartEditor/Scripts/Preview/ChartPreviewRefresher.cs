using ChartConvert;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class ChartPreviewRefresher : MonoBehaviour, IChartPreviewRefreshable
    {
        [SerializeField] ChartPreviewGenerator previewGenerator;
        [SerializeField] GroundControllerPreview groundController;

        INoteSpawnDataOptionGetter optionHolder;
        IChartEditorDataGetter dataGetter;
        CancellationTokenSource refreshPreviewCts;

        [Inject]
        public void Constructor(INoteSpawnDataOptionGetter optionHolder, IChartEditorDataGetter dataGetter)
        {
            this.optionHolder = optionHolder;
            this.dataGetter = dataGetter;
        }

        public async UniTask RefreshPreviewFromEditorDataAsync()
        {
            if (dataGetter?.ChartData?.Value == null) { return; }

            refreshPreviewCts?.CancelAndDispose();
            refreshPreviewCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            CancellationToken token = refreshPreviewCts.Token;
            var chartData = dataGetter.ChartData.Value;
            float offset = dataGetter.Offset.Value;

            try
            {
                ChartDataOrigin chartDataOrigin = await UniTask.RunOnThreadPool(
                    () =>
                    {
                        token.ThrowIfCancellationRequested();
                        ChartExporter exporter = new ChartExporter();
                        return exporter.Export(chartData, offset);
                    },
                    cancellationToken: token);

                token.ThrowIfCancellationRequested();
                await UniTask.SwitchToMainThread(token);

                RefreshPreview(chartDataOrigin);
            }
            catch (OperationCanceledException)
            {
                // Ignore latest-only cancellation.
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void RefreshPreview(ChartDataOrigin savedChartData)
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

        private void OnDestroy()
        {
            refreshPreviewCts?.CancelAndDispose();
        }
    }
}
