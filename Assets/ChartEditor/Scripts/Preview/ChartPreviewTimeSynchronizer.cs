using UniRx;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    /// <summary>
    /// ChartEditor のスクロール位置(PlaybackProgress)をプレビュー用の時刻に同期する。
    /// </summary>
    public class ChartPreviewTimeSynchronizer : MonoBehaviour, ITimeGetter
    {
        private IChartEditorDataGetter chartEditorDataGetter;
        private readonly ReactiveProperty<float> time = new ReactiveProperty<float>(0f);

        public float Time => time.Value;
        public IReadOnlyReactiveProperty<float> TimeRP => time;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        private void Start()
        {
            Bind();
            UpdateTimeFromProgress(chartEditorDataGetter?.PlaybackProgress?.Value ?? 0f);
        }

        private void Bind()
        {
            chartEditorDataGetter?.PlaybackProgress
                .Subscribe(UpdateTimeFromProgress)
                .AddTo(this.gameObject);
        }

        private void UpdateTimeFromProgress(float progress)
        {
            if (chartEditorDataGetter == null)
            {
                time.Value = 0f;
                return;
            }

            float chartSeconds = chartEditorDataGetter.ChartSeconds.Value;
            time.Value = chartSeconds * progress;
        }
    }
}
