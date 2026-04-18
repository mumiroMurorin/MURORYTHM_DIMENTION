using ChartConvert;

namespace ChartEditor
{
    public interface IChartPreviewRefreshable
    {
        void RefreshPreview(ChartDataOrigin savedChartData, string savedFilePath);
    }
}
