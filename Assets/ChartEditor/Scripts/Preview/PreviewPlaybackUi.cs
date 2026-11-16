using VContainer.Unity;

namespace ChartEditor
{
    /// <summary>
    /// 全画面プレビューで再生している間は、まわりの画面部品を隠す (録音 20261117_024318)
    ///
    /// 隠す部品の一覧は、シーンに置いてある PlaybackHiddenUiView。止めたとき・プレビューを出たときに、元に戻す。
    /// </summary>
    public class PreviewPlaybackUi : ITickable
    {
        readonly IChartEditorDataGetter dataGetter;

        PlaybackHiddenUiView view;
        bool isSearched;
        bool isHidden;

        public PreviewPlaybackUi(IChartEditorDataGetter dataGetter)
        {
            this.dataGetter = dataGetter;
        }

        void ITickable.Tick()
        {
            bool shouldHide = dataGetter.EditNoteType.Value == EditNoteType.Preview && dataGetter.PlayMode.Value == PlayMode.Play;
            if (shouldHide == isHidden) { return; }

            if (!isSearched)
            {
                isSearched = true;
                view = UnityEngine.Object.FindObjectOfType<PlaybackHiddenUiView>(true);
            }

            isHidden = shouldHide;
            if (view != null) { view.SetHidden(isHidden); }
        }
    }
}
