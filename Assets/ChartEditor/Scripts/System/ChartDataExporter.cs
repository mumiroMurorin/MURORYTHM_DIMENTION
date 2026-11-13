using System;
using UnityEngine;
using VContainer;
using ChartConvert;
using static JsonUtil.JsonWriter;

namespace ChartEditor
{
    public class ChartDataExporter : MonoBehaviour
    {
        IChartEditorDataGetter editorDataGetter;
        ChartSaveGuard saveGuard;
        public event Action<ChartDataOrigin, string> OnChartSaved;

        [Inject]
        public void Construct(IChartEditorDataGetter editorDataGetter, ChartSaveGuard saveGuard)
        {
            this.editorDataGetter = editorDataGetter;
            this.saveGuard = saveGuard;
        }

        public void Export()
        {
            if (!HasChart()) { return; }

            ChartExporter chartExporter = new ChartExporter();
            ChartDataOrigin chartDataOrigin = chartExporter.Export(editorDataGetter.ChartData.Value, editorDataGetter.Offset.Value);

            // Overwrite the current chart file when a save target is already known.
            if (!string.IsNullOrWhiteSpace(ChartFilePathCache.CurrentChartFilePath))
            {
                // 上書き前のファイルは「.bak」として残す
                bool isSaved = TrySaveToJsonPath(chartDataOrigin, ChartFilePathCache.CurrentChartFilePath, keepBackup: true);
                if (isSaved)
                {
                    NotifyChartSaved(chartDataOrigin, ChartFilePathCache.CurrentChartFilePath);
                    return;
                }
            }

            // Fall back to Save As dialog when no target path exists or overwrite failed.
            if (TrySaveToJsonFileDialog(chartDataOrigin, out string savedPath))
            {
                ChartFilePathCache.CurrentChartFilePath = savedPath;
                NotifyChartSaved(chartDataOrigin, savedPath);
            }
        }

        public void ExportNewFile()
        {
            if (!HasChart()) { return; }

            ChartExporter chartExporter = new ChartExporter();
            ChartDataOrigin chartDataOrigin = chartExporter.Export(editorDataGetter.ChartData.Value, editorDataGetter.Offset.Value);

            if (TrySaveToJsonFileDialog(chartDataOrigin, out string savedPath))
            {
                ChartFilePathCache.CurrentChartFilePath = savedPath;
                NotifyChartSaved(chartDataOrigin, savedPath);
            }
        }

        /// <summary>
        /// 保存する譜面があるか (起動直後など、譜面が未作成のときは保存しない)
        /// </summary>
        private bool HasChart()
        {
            var chartData = editorDataGetter.ChartData.Value;
            if (chartData != null && chartData.BarDatas.Count > 0) { return true; }

            Debug.Log("【Export】保存する譜面がありません");
            return false;
        }

        private void NotifyChartSaved(ChartDataOrigin chartDataOrigin, string savedPath)
        {
            saveGuard?.MarkSaved();
            OnChartSaved?.Invoke(chartDataOrigin, savedPath);
        }
    }
}
