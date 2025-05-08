using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ChartEditor
{
    public class MusicPlayer : MonoBehaviour
    {
        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;

        CancellationTokenSource cts;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter, IChartEditorDataSetter chartEditorDataSetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
            this.chartEditorDataSetter = chartEditorDataSetter;
        }

        void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 楽曲の再生
            chartEditorDataGetter?.PlayMode
                .Where(mode => mode == PlayMode.Play)
                .Subscribe(_ => PlayMusic())
                .AddTo(this.gameObject);

            // 楽曲の停止
            chartEditorDataGetter?.PlayMode
                .Where(mode => mode == PlayMode.Stop)
                .Subscribe(_ => StopMusic())
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// 楽曲の再生
        /// </summary>
        private void PlayMusic()
        {
            // 楽曲の再生
            AudioClip clip = chartEditorDataGetter.Music.Value;
            if(clip != null)
            {
                float chartLength = chartEditorDataGetter.ChartSeconds.Value;
                float progress = Mathf.Clamp01((chartLength * chartEditorDataGetter.PlaybackProgress.Value) / clip.length);
                SoundManager.Instance.PlayBGM(clip, loopFlg: false, isFadeout: false, progress: progress);
            }

            // 譜面再生
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            cts = new CancellationTokenSource();
            UpdatePlaybackProgressAsync(cts.Token).Forget();
        }

        private async UniTask UpdatePlaybackProgressAsync(CancellationToken token)
        {
            float chartLength = chartEditorDataGetter.ChartSeconds.Value;

            while (!token.IsCancellationRequested)
            {
                float currentProgress = chartEditorDataGetter.PlaybackProgress.Value;
                float addProgress = Time.deltaTime / chartLength;

                if(currentProgress == 1f) { break; }
                chartEditorDataSetter.SetPlaybackProgress(currentProgress + addProgress);

                // 1フレーム待つ
                await UniTask.Yield(token);
            }
        }

        /// <summary>
        /// 楽曲の停止
        /// </summary>
        private void StopMusic()
        {
            SoundManager.Instance.StopBGM(false);

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
        }

        private void OnDestroy()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
        }
    }
}