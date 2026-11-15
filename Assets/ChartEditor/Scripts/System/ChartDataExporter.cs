using System;
using UnityEngine;
using VContainer;
using ChartConvert;
using static JsonUtil.JsonWriter;

namespace ChartEditor
{
    /// <summary>
    /// 譜面を、ファイルに保存する (上書き保存、名前を付けて保存)
    ///
    /// レギュレーション違反 (重なっているノーツ、つながっていないホールドなど) が残っている譜面は、保存しない。
    /// 違反の内容を知らせて、直してから保存し直してもらう。
    /// 一定時間ごとの自動保存 (ChartSaveGuard のバックアップ) は、ここを通らないので、違反があっても作られる
    /// (作業中の譜面が、違反のせいで失われないように)。
    /// </summary>
    public class ChartDataExporter : MonoBehaviour
    {
        IChartEditorDataGetter editorDataGetter;
        ChartSaveGuard saveGuard;
        RegulationWarningNotifier regulationNotifier;
        public event Action<ChartDataOrigin, string> OnChartSaved;

        [Inject]
        public void Construct(IChartEditorDataGetter editorDataGetter, ChartSaveGuard saveGuard, RegulationWarningNotifier regulationNotifier)
        {
            this.editorDataGetter = editorDataGetter;
            this.saveGuard = saveGuard;
            this.regulationNotifier = regulationNotifier;
        }

        public void Export()
        {
            if (!HasChart()) { return; }
            if (!HasNoRegulationViolation()) { return; }

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
            if (!HasNoRegulationViolation()) { return; }

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
            // 譜面の読み込み中は、読み込み途中の譜面を保存してしまうので受け付けない
            if (editorDataGetter.IsLoadingChart.Value) { return false; }

            var chartData = editorDataGetter.ChartData.Value;
            if (chartData != null && chartData.BarDatas.Count > 0) { return true; }

            Debug.Log("【Export】保存する譜面がありません");
            return false;
        }

        /// <summary>
        /// レギュレーション違反が残っていないか。残っているときは、内容を知らせる (保存はしない)
        /// </summary>
        private bool HasNoRegulationViolation()
        {
            string violations = regulationNotifier != null ? regulationNotifier.GetViolationText() : string.Empty;
            if (string.IsNullOrEmpty(violations)) { return true; }

            MessageDialog.ShowError(
                "保存できません",
                "レギュレーション違反が残っているため、保存できません。\n" +
                "赤い縁取りが出ているノーツを直してから、もう一度保存してください。\n\n" +
                violations + "\n\n" +
                "(自動保存のバックアップは、違反があっても作られます)");
            return false;
        }

        private void NotifyChartSaved(ChartDataOrigin chartDataOrigin, string savedPath)
        {
            saveGuard?.MarkSaved();
            OnChartSaved?.Invoke(chartDataOrigin, savedPath);
        }
    }
}
