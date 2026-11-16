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
        [Tooltip("譜面の再生位置と音声の再生位置がこの秒数以上ずれたら音声に合わせ直す")]
        [SerializeField] float resyncThresholdSeconds = 0.03f;

        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;
        IChartEditorOptionGetter optionGetter;

        CancellationTokenSource cts;

        // 再生中の楽曲 (再生していないときは null)
        AudioClip playingClip;

        [Inject]
        public void Construct(
            IChartEditorDataGetter chartEditorDataGetter,
            IChartEditorDataSetter chartEditorDataSetter,
            IChartEditorOptionGetter optionGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
            this.chartEditorDataSetter = chartEditorDataSetter;
            this.optionGetter = optionGetter;
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

            // 再生中に速度が変えられたら、鳴っている楽曲にもすぐ反映する
            optionGetter?.PlaybackSpeed
                .Subscribe(_ => ApplyPlaybackSpeedToMusic())
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// 現在の再生速度 (1 = 等倍)
        /// </summary>
        private float PlaybackSpeed => optionGetter != null ? optionGetter.PlaybackSpeed.Value : 1f;

        /// <summary>
        /// 楽曲の再生
        /// </summary>
        private void PlayMusic()
        {
            // 楽曲の再生
            AudioClip clip = chartEditorDataGetter.Music.Value;
            float chartLength = chartEditorDataGetter.ChartSeconds.Value;
            if (clip != null && chartLength > 0f)
            {
                float progress = Mathf.Clamp01((chartLength * chartEditorDataGetter.PlaybackProgress.Value) / clip.length);
                // フェードインせずに、すぐ普通の音量で始める (録音 20261117_045937)
                SoundManager.Instance.PlayBGM(clip, loopFlg: false, isFadeout: false, progress: progress, isFadeIn: false);

                playingClip = clip;
                ApplyPlaybackSpeedToMusic();
            }

            // 譜面再生
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            cts = new CancellationTokenSource();
            UpdatePlaybackProgressAsync(clip, cts.Token).Forget();
        }

        private async UniTask UpdatePlaybackProgressAsync(AudioClip clip, CancellationToken token)
        {
            // 再生開始の通知が全て終わってから処理する (通知の途中で停止に切り替えないため)
            await UniTask.Yield(token);

            float chartLength = chartEditorDataGetter.ChartSeconds.Value;

            // 譜面が空のときは再生できない
            if (chartLength <= 0f)
            {
                chartEditorDataSetter.SetPlayMode(PlayMode.Stop);
                return;
            }

            // 譜面上の再生位置(秒)
            float chartTime = chartLength * chartEditorDataGetter.PlaybackProgress.Value;

            while (!token.IsCancellationRequested)
            {
                // 再生速度のぶんだけ進める (0.5倍速なら、実時間1秒で譜面は0.5秒進む)
                chartTime += Time.unscaledDeltaTime * PlaybackSpeed;

                // 音声が鳴っている間は音声の再生位置を正とし、ずれたら合わせ直す
                if (TryGetMusicTime(clip, out float musicTime) && Mathf.Abs(musicTime - chartTime) > resyncThresholdSeconds)
                {
                    chartTime = musicTime;
                }

                chartEditorDataSetter.SetPlaybackProgress(chartTime / chartLength);

                // 末尾まで再生したら停止する
                if (chartTime >= chartLength)
                {
                    chartEditorDataSetter.SetPlayMode(PlayMode.Stop);
                    return;
                }

                // 1フレーム待つ
                await UniTask.Yield(token);
            }
        }

        /// <summary>
        /// 再生中の楽曲の再生位置(秒)を取得する
        /// </summary>
        private bool TryGetMusicTime(AudioClip clip, out float seconds)
        {
            seconds = 0f;
            if (clip == null) { return false; }

            foreach (var source in SoundManager.Instance.BGMSources)
            {
                if (source == null || source.clip != clip || !source.isPlaying) { continue; }

                seconds = source.time;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 再生中の楽曲に、現在の再生速度を反映する
        /// 音声は再生の速さ(ピッチ)を変えて遅くするので、速度を下げると音程も下がる
        /// </summary>
        private void ApplyPlaybackSpeedToMusic()
        {
            if (playingClip == null) { return; }

            foreach (var source in SoundManager.Instance.BGMSources)
            {
                if (source == null || source.clip != playingClip) { continue; }

                source.pitch = PlaybackSpeed;
            }
        }

        /// <summary>
        /// 楽曲の再生の速さを等倍に戻す (譜面エディタ以外の再生に影響を残さないため)
        /// </summary>
        private void ResetMusicSpeed()
        {
            playingClip = null;

            if (SoundManager.Instance == null) { return; }

            foreach (var source in SoundManager.Instance.BGMSources)
            {
                if (source != null) { source.pitch = 1f; }
            }
        }

        /// <summary>
        /// 楽曲の停止
        /// </summary>
        private void StopMusic()
        {
            SoundManager.Instance.StopBGM(false);
            ResetMusicSpeed();

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
