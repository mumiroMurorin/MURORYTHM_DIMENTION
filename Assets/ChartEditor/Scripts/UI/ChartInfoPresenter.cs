using UnityEngine;
using VContainer.Unity;

namespace ChartEditor
{
    /// <summary>
    /// 編集画面の「譜面情報」の欄(ChartInfoView。シーンに置いてある画面部品)に、表示する値を渡す
    ///
    /// ・コンボ数と、ノーツの種類ごとの数・小節数・譜面の長さ・BPM を、一定の間隔で調べて渡す
    /// ・欄は、グラウンド・スペースのレイヤーを編集している間だけ出す
    ///   (全画面プレビューでは編集画面ごと隠れる。頂点レイヤーでは、同じ場所に頂点の欄が出る)
    /// </summary>
    public class ChartInfoPresenter : ITickable
    {
        // 調べ直す間隔(秒)
        const float REFRESH_INTERVAL_SECONDS = 0.3f;

        readonly IChartEditorDataGetter dataGetter;

        bool hasSearchedScene;
        ChartInfoView view;

        float nextRefreshTime;
        bool wasDetailShown;
        // 今表示している内容
        bool hasDisplayedCombo;
        int displayedCombo;
        bool hasDisplayedDetails;
        ChartStatisticsData displayedStatistics;
        int displayedSeconds;

        public ChartInfoPresenter(IChartEditorDataGetter dataGetter)
        {
            this.dataGetter = dataGetter;
        }

        void ITickable.Tick()
        {
            SearchSceneOnce();
            if (view == null) { return; }

            // グラウンド・スペースのレイヤーを編集している間だけ出す
            var layer = dataGetter.EditNoteType.Value;
            bool isVisible = layer == EditNoteType.Ground || layer == EditNoteType.Space;

            view.SetVisible(isVisible);
            if (!isVisible) { return; }

            // 欄が開かれたときは、閉じている間に数えていなかった分を、すぐ数える
            bool isDetailShown = view.IsDetailShown;
            if (isDetailShown && !wasDetailShown) { nextRefreshTime = 0f; }
            wasDetailShown = isDetailShown;

            float now = Time.unscaledTime;
            if (now < nextRefreshTime) { return; }
            nextRefreshTime = now + REFRESH_INTERVAL_SECONDS;

            // 譜面の読み込み中は調べない (読み込み途中の数が出てしまうため)
            if (dataGetter.IsLoadingChart.Value) { return; }

            Refresh(isDetailShown);
        }

        /// <summary>
        /// 今の譜面を調べて、前に表示した内容から変わっていれば、欄に渡す
        /// </summary>
        /// <param name="isDetailShown">ノーツの種類ごとの数なども、今見えているか (見えていない間は数えない)</param>
        private void Refresh(bool isDetailShown)
        {
            int combo = dataGetter.TotalNoteCount.Value;
            if (!hasDisplayedCombo || combo != displayedCombo)
            {
                hasDisplayedCombo = true;
                displayedCombo = combo;
                view.SetCombo(combo);
            }

            if (!isDetailShown) { return; }

            ChartStatisticsData statistics = ChartStatistics.Count(dataGetter.ChartData.Value);
            int seconds = Mathf.Max(0, Mathf.FloorToInt(dataGetter.ChartSeconds.Value));
            if (hasDisplayedDetails && statistics.Equals(displayedStatistics) && seconds == displayedSeconds) { return; }

            hasDisplayedDetails = true;
            displayedStatistics = statistics;
            displayedSeconds = seconds;
            view.SetDetails(statistics, seconds);
        }

        /// <summary>
        /// シーンに置いてある欄を探す (最初の1回だけ)
        /// </summary>
        private void SearchSceneOnce()
        {
            if (hasSearchedScene) { return; }
            hasSearchedScene = true;

            view = Object.FindObjectOfType<ChartInfoView>(true);
        }
    }
}
