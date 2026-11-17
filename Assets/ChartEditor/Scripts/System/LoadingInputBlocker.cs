using System;
using UniRx;
using UnityEngine;
using VContainer.Unity;

namespace ChartEditor
{
    /// <summary>
    /// 譜面を読み込んでいる間、画面全体を薄く覆って、ボタンや譜面へのクリックを受け付けないようにする
    ///
    /// 読み込みは数フレームに分けて行うので、その途中で編集されると譜面が食い違ってしまう。
    /// 覆いはシーンに置いてある (EditorOverlays の LoadingBlocker。いちばん手前の重なり順で、クリックを受け取る)。
    /// キー操作は覆いでは止められないので、各操作の側で読み込み中かどうかを確認している。
    /// </summary>
    public class LoadingInputBlocker : IStartable, IDisposable
    {
        readonly IChartEditorDataGetter dataGetter;

        IDisposable subscription;

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
        }

        private void SetBlocking(bool isBlocking)
        {
            var overlay = EditorOverlays.Instance != null ? EditorOverlays.Instance.LoadingBlocker : null;
            if (overlay == null)
            {
                if (isBlocking) { Debug.LogWarning("【System】読み込み中の覆い (EditorOverlays) がシーンにありません"); }
                return;
            }

            if (overlay.activeSelf != isBlocking) { overlay.SetActive(isBlocking); }
        }
    }
}
