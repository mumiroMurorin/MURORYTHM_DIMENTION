using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class MusicPlayer : MonoBehaviour
    {
        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;

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

        private void Update()
        {
            if (chartEditorDataGetter.PlayMode.Value == PlayMode.Play)
            {
                UpdatePlaybackProgress();
            }
        }

        /// <summary>
        /// 楽曲の再生
        /// </summary>
        private void PlayMusic()
        {
            AudioClip clip = chartEditorDataGetter.Music.Value;
            float progress = chartEditorDataGetter.PlaybackProgress.Value;
            SoundManager.Instance.PlayBGM(clip, loopFlg: false, isFadeout: false, progress: progress);
        }

        /// <summary>
        /// 楽曲の停止
        /// </summary>
        private void StopMusic()
        {
            SoundManager.Instance.StopBGM(false);
        }

        /// <summary>
        /// 再生時間の更新
        /// </summary>
        private void UpdatePlaybackProgress()
        {
            float currentProgress = chartEditorDataGetter.PlaybackProgress.Value;
            float addProgress = Time.deltaTime / chartEditorDataGetter.Music.Value.length;
            chartEditorDataSetter?.SetPlaybackProgress(currentProgress + addProgress);
        }
    }
}