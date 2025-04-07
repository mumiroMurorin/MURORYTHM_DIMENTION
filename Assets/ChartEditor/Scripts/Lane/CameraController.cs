using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

namespace ChartEditor
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] GameObject viewCameraParent;
        [SerializeField] GameObject offsetAxis;

        IChartEditorDataGetter chartEditorDataGetter;
        float chartLength;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 倍率の変更
            chartEditorDataGetter?.ChartViewScale
                .Subscribe(scale => {
                    UpdateChartLength(chartEditorDataGetter.Music.Value,scale);
                    MoveCamera(chartEditorDataGetter.PlaybackProgress.Value);
                    ChangeCameraOffset(chartEditorDataGetter.Offset.Value);
                })
                .AddTo(this.gameObject);

            // 再生位置
            chartEditorDataGetter?.PlaybackProgress
                .Subscribe(MoveCamera)
                .AddTo(this.gameObject);

            // オフセット
            chartEditorDataGetter?.Offset
                .Subscribe(ChangeCameraOffset)
                .AddTo(this.gameObject);

            // 楽曲が変わった時
            chartEditorDataGetter?.Music
                .Subscribe(music => {
                    UpdateChartLength(music, chartEditorDataGetter.ChartViewScale.Value);
                })
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// 譜面の長さの更新
        /// </summary>
        /// <param name="music"></param>
        /// <param name="scale"></param>
        private void UpdateChartLength(AudioClip music, float scale)
        {
            if (music == null) { return; }

            chartLength = music.length * scale;
        }

        /// <summary>
        /// カメラ視点の移動
        /// </summary>
        private void MoveCamera(float ratio)
        {
            viewCameraParent.transform.position = new Vector3(
                viewCameraParent.transform.position.x,
                viewCameraParent.transform.position.y,
                chartLength * ratio
                );
        }

        /// <summary>
        /// オフセットに対応したカメラ位置の調整
        /// </summary>
        private void ChangeCameraOffset(float offset)
        {
            if (chartEditorDataGetter.Music.Value == null) { return; }

            float musicLength = chartEditorDataGetter.Music.Value.length;

            float z = offset / 1000 * (chartLength / musicLength);
            offsetAxis.transform.localPosition = new Vector3(
                offsetAxis.transform.localPosition.x,
                z,
                offsetAxis.transform.localPosition.z
                );
        }
    }

}
