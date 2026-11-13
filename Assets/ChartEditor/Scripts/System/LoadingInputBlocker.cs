using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;

namespace ChartEditor
{
    /// <summary>
    /// 譜面を読み込んでいる間、画面全体を薄く覆って、ボタンや譜面へのクリックを受け付けないようにする
    ///
    /// 読み込みは数フレームに分けて行うので、その途中で編集されると譜面が食い違ってしまう。
    /// 覆いは実行時に作る (シーンに専用のオブジェクトを置かなくて済むように)。
    /// キー操作は覆いでは止められないので、各操作の側で読み込み中かどうかを確認している。
    /// </summary>
    public class LoadingInputBlocker : IStartable, IDisposable
    {
        // ほかのどの画面部品よりも手前に出す
        const int SORTING_ORDER = 30000;
        static readonly Color overlayColor = new Color(0f, 0f, 0f, 0.3f);

        readonly IChartEditorDataGetter dataGetter;

        IDisposable subscription;
        GameObject overlay;

        public LoadingInputBlocker(IChartEditorDataGetter dataGetter)
        {
            this.dataGetter = dataGetter;
        }

        void IStartable.Start()
        {
            subscription = dataGetter.IsLoadingChart.Subscribe(SetBlocking);
        }

        void IDisposable.Dispose()
        {
            subscription?.Dispose();
            if (overlay != null) { UnityEngine.Object.Destroy(overlay); }
        }

        private void SetBlocking(bool isBlocking)
        {
            if (!isBlocking)
            {
                if (overlay != null) { overlay.SetActive(false); }
                return;
            }

            if (overlay == null) { overlay = CreateOverlay(); }
            overlay.SetActive(true);
        }

        /// <summary>
        /// 画面全体を覆う、クリックを遮る半透明の板を作る
        /// </summary>
        private static GameObject CreateOverlay()
        {
            var root = new GameObject("LoadingInputBlocker");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SORTING_ORDER;
            root.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Blocker", typeof(RectTransform));
            panel.transform.SetParent(root.transform, false);

            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = panel.AddComponent<Image>();
            image.color = overlayColor;
            image.raycastTarget = true;

            return root;
        }
    }
}
