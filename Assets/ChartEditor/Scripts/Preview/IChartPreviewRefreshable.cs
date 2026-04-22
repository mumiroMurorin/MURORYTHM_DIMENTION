using ChartConvert;
using Cysharp.Threading.Tasks;

namespace ChartEditor
{
    public interface IChartPreviewRefreshable
    {
        void RefreshPreview(ChartDataOrigin savedChartData);
        UniTask RefreshPreviewFromEditorDataAsync();
    }
}
