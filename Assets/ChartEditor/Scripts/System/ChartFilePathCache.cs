namespace ChartEditor
{
    /// <summary>
    /// Stores the current chart file path used by the editor.
    /// </summary>
    public static class ChartFilePathCache
    {
        public static string CurrentChartFilePath { get; set; } = string.Empty;
    }
}