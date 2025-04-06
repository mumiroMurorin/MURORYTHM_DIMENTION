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
        [SerializeField] GameObject obj;
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
            AudioClip clip = chartEditorDataGetter.Music.Value;
            float progress = chartEditorDataGetter.PlaybackProgress.Value;
            SoundManager.Instance.PlayBGM(clip, loopFlg: false, isFadeout: false, progress: progress);

            if (cts != null)
            {
                cts.Cancel();  
                cts.Dispose(); 
            }

            cts = new CancellationTokenSource();

            // 非同期で楽曲のグラウンドを進める
            UpdatePlaybackProgressAsync(cts.Token).Forget();
        }

        /// <summary>
        /// 楽曲の停止
        /// </summary>
        private void StopMusic()
        {
            SoundManager.Instance.StopBGM(false);
        }

        /// <summary>
        /// 非同期で楽曲を進める
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTask UpdatePlaybackProgressAsync(CancellationToken token)
        {
            if (chartEditorDataGetter == null) { return; }
            if (chartEditorDataGetter.ChartData == null) { return; }

            ChartData chartData = chartEditorDataGetter.ChartData.Value;
            float musicLength = chartEditorDataGetter.Music.Value.length;

            // 非同期で譜面データ内を走る
            foreach (var bar in chartData.BarDatas)
            {
                float beatUnit = bar.BeatUnit.Value;
                int divNum = bar.DivisionNum.Value;

                foreach (var sub in bar.SubDivisionDatas)
                {
                    float bpm = sub.Bpm.Value;

                    // その分線の通り過ぎる時間[sec]待つ
                    // 待ち時間[sec] = 曲の長さ[sec] / (拍の数) / 分割数
                    //               = 曲の長さ[sec] / (BeatUnit / 4 * bpm * 分割数)
                    float waitSec = musicLength / (beatUnit / 4 * bpm * divNum);
                    Debug.Log($"きちゃ1 {waitSec}, {bpm}");
                    await WaitWithAddPlaybackProgress(waitSec, bpm, token);
                }
            }
        }

        /// <summary>
        /// 非同期で進行度を追加
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="bpm"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTask WaitWithAddPlaybackProgress(float duration, float bpm, CancellationToken token)
        {
            float elapsed = 0f;
            float musicLength = chartEditorDataGetter.Music.Value.length;
            float currentProgress = chartEditorDataGetter.PlaybackProgress.Value;
            float quarterNoteLength = chartEditorDataGetter.ChartViewScale.Value;

            while (elapsed < duration)
            {
                // 1フレームに追加する進度[z] = 4分音符あたりの長さ[z] * 曲中の4分音符の数 * 1フレ[sec]
                //                            = 4分音符あたりの長さ[z] * 現bpm * 曲の長さ[min] * 1フレ[sec]
                float z = quarterNoteLength * bpm * musicLength * Time.deltaTime;
                obj.transform.position += Vector3.forward * z;
                //float addProgress = bpm * musicLength * Time.deltaTime / musicLength;
                //chartEditorDataSetter?.SetPlaybackProgress(currentProgress + addProgress);
                //Debug.Log($"きちゃ2 {elapsed}/{duration}: {chartEditorDataGetter.PlaybackProgress.Value}");

                await UniTask.Yield(token); // 次のフレームまで待機
                elapsed += Time.deltaTime;
            }
        }

        private void OnDestroy()
        {
            if (cts != null)
            {
                cts.Cancel();  // 先にキャンセル
                cts.Dispose(); // その後でDispose
                cts = null;    // 参照をクリア（安全のため）
            }
        }
    }
}