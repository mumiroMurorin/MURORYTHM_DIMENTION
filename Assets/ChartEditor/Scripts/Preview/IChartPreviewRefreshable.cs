using ChartConvert;
using Cysharp.Threading.Tasks;

namespace ChartEditor
{
    public interface IChartPreviewRefreshable
    {
        /// <summary>
        /// 保存形式の譜面データをプレビューに反映する
        /// </summary>
        void RefreshPreview(ChartDataOrigin savedChartData);

        /// <summary>
        /// 編集中の譜面をプレビューに反映する (変わったノーツだけ作り直す)
        /// </summary>
        UniTask RefreshPreviewFromEditorDataAsync();

        /// <summary>
        /// 編集中の譜面でプレビューをすべて作り直す
        /// </summary>
        void RebuildPreviewFromEditorData();
    }
}
