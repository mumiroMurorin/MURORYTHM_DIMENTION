using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace ChartEditor
{
    /// <summary>
    /// 編集画面の「全体マップ」(ChartOverviewView。シーンに置いてある画面部品)に、縮小図と今の再生位置を渡す
    ///
    /// ・譜面が編集されたら、少し間を置いて縮小図を描き直す
    /// ・今の再生位置の印と、今いる小節の番号を、毎フレーム合わせる
    /// ・縮小図の上をクリック・ドラッグされたら、その場所へ再生位置を移す (再生中と、譜面の読み込み中は動かさない)
    /// ・全体マップは、グラウンド・スペースのレイヤーを編集している間だけ出す
    /// </summary>
    public class ChartOverviewPresenter : ITickable, IDisposable
    {
        // 最後の編集からこの秒数たったら、縮小図を描き直す (続けて行った編集を1回にまとめるため)
        const float RENDER_DELAY_SECONDS = 0.2f;
        // 編集の合図が無くても、この間隔(秒)で描き直す (ドラッグの途中の動きや、合図の出ない変更を拾うため)
        const float PERIODIC_RENDER_SECONDS = 1f;
        // 縮小図の絵の大きさの範囲
        const int MIN_TEXTURE_SIZE = 8;
        const int MAX_TEXTURE_WIDTH = 256;
        const int MAX_TEXTURE_HEIGHT = 2048;

        readonly IChartEditorDataGetter dataGetter;
        readonly IChartEditorDataSetter dataSetter;

        bool hasSearchedScene;
        ChartOverviewView view;

        // 縮小図の絵
        Texture2D texture;
        Color32[] pixels;

        // 描き直しの待ち状態
        bool isDirty = true;
        float dirtyTime;
        float nextPeriodicRenderTime;
        ChartData renderedChartData;
        bool wasLoading;

        // 小節ごとの先頭の時刻と、譜面全体の長さ (今いる小節を求めるのに使う)
        readonly List<float> barStartSeconds = new List<float>();
        float totalSeconds;
        // 今表示している小節の番号と、小節の数
        int displayedBarIndex = -1;
        int displayedBarCount = -1;

        readonly IChartEditorOptionGetter optionGetter;

        public ChartOverviewPresenter(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, IChartEditorOptionGetter optionGetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            this.optionGetter = optionGetter;

            // 編集操作はすべてUndo履歴を通るので、履歴の変化を「譜面が変わった」合図にする
            UndoRedo.History.OnChanged += MarkDirty;
        }

        void IDisposable.Dispose()
        {
            UndoRedo.History.OnChanged -= MarkDirty;

            if (view != null) { view.OnSeekRequestedListener -= Seek; }
            if (texture != null) { UnityEngine.Object.Destroy(texture); }

            texture = null;
        }

        void ITickable.Tick()
        {
            SearchSceneOnce();
            if (view == null) { return; }

            // グラウンド・スペースのレイヤーを編集している間だけ出す (メニューの「表示」で消されているときは出さない)
            var layer = dataGetter.EditNoteType.Value;
            bool isVisible =
                (layer == EditNoteType.Ground || layer == EditNoteType.Space) &&
                optionGetter.GetPanelVisibility(EditorPanel.ChartOverview).Value;

            view.SetVisible(isVisible);
            if (!isVisible) { return; }

            ChartData chartData = dataGetter.ChartData.Value;
            bool isLoading = dataGetter.IsLoadingChart.Value;
            float now = Time.unscaledTime;

            // 譜面が丸ごと入れ替わったとき、読み込みが終わったときも、描き直す
            if (!ReferenceEquals(chartData, renderedChartData) || (wasLoading && !isLoading))
            {
                isDirty = true;
                dirtyTime = now - RENDER_DELAY_SECONDS;
            }

            wasLoading = isLoading;

            // 譜面の読み込み中は描かない (読み込み途中の譜面が出てしまうため)
            bool isRenderTime = (isDirty && now - dirtyTime >= RENDER_DELAY_SECONDS) || now >= nextPeriodicRenderTime;
            if (!isLoading && isRenderTime) { Render(chartData, now); }

            // 今の再生位置
            float progress = dataGetter.PlaybackProgress.Value;
            view.SetMarker(progress);
            RefreshBarText(progress);
        }

        /// <summary>
        /// 譜面が変わったので、少し待ってから縮小図を描き直す
        /// </summary>
        private void MarkDirty()
        {
            isDirty = true;
            dirtyTime = Time.unscaledTime;
        }

        /// <summary>
        /// 縮小図を描き直す
        /// </summary>
        private void Render(ChartData chartData, float now)
        {
            isDirty = false;
            nextPeriodicRenderTime = now + PERIODIC_RENDER_SECONDS;
            renderedChartData = chartData;

            // 小節の先頭の時刻も、ここで求め直す (今いる小節の番号に使う)
            totalSeconds = ChartOverviewImage.GetBarStartSeconds(chartData, barStartSeconds);
            displayedBarIndex = -1;

            if (!EnsureTexture()) { return; }

            ChartOverviewImage.Render(chartData, pixels, texture.width, texture.height);
            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        /// <summary>
        /// 縮小図の絵を、画面部品の大きさに合わせて用意する
        /// </summary>
        private bool EnsureTexture()
        {
            Vector2Int size = view.MapPixelSize;
            int width = Mathf.Clamp(size.x, MIN_TEXTURE_SIZE, MAX_TEXTURE_WIDTH);
            int height = Mathf.Clamp(size.y, MIN_TEXTURE_SIZE, MAX_TEXTURE_HEIGHT);

            if (texture != null && texture.width == width && texture.height == height) { return true; }

            if (texture != null) { UnityEngine.Object.Destroy(texture); }

            texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "ChartOverview",
                // 細い線がにじまないよう、点のまま拡大する
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            pixels = new Color32[width * height];

            view.SetMapTexture(texture);
            return true;
        }

        /// <summary>
        /// 今いる小節の番号を表示する (変わったときだけ書き換える)
        /// </summary>
        private void RefreshBarText(float progress)
        {
            int barCount = barStartSeconds.Count;
            int barIndex = ChartOverviewImage.FindBarIndex(barStartSeconds, progress * totalSeconds);
            if (barIndex == displayedBarIndex && barCount == displayedBarCount) { return; }

            displayedBarIndex = barIndex;
            displayedBarCount = barCount;
            view.SetBarText(barCount > 0 ? $"{barIndex + 1}/{barCount}" : "-");
        }

        /// <summary>
        /// 縮小図の上がクリック・ドラッグされたとき。その場所へ再生位置を移す
        /// </summary>
        /// <param name="progress">譜面のどのあたりか (0 = 先頭、1 = 末尾)</param>
        private void Seek(float progress)
        {
            // 譜面の読み込み中と、再生中は動かさない (再生中は、マウスホイールでの移動も受け付けていない)
            if (dataGetter.IsLoadingChart.Value) { return; }
            if (dataGetter.PlayMode.Value == PlayMode.Play) { return; }

            dataSetter.SetPlaybackProgress(progress);
        }

        /// <summary>
        /// シーンに置いてある全体マップを探す (最初の1回だけ)
        /// </summary>
        private void SearchSceneOnce()
        {
            if (hasSearchedScene) { return; }
            hasSearchedScene = true;

            view = UnityEngine.Object.FindObjectOfType<ChartOverviewView>(true);
            if (view != null) { view.OnSeekRequestedListener += Seek; }
        }
    }
}
