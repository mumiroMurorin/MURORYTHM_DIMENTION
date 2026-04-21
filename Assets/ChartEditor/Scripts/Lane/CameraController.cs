using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

namespace ChartEditor
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] Transform viewCameraParent;
        [SerializeField] GameObject setActiveObject;
        [SerializeField] GameObject offsetAxis;

        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorOptionGetter optionGetter;
        float chartLength;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter, IChartEditorOptionGetter optionGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
            this.optionGetter = optionGetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 倍率の変更
            optionGetter?.ChartViewScale
                .Subscribe(scale => {
                    UpdateChartLength(chartEditorDataGetter.ChartSeconds.Value, scale);
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

            // 譜面長さが変わった時
            chartEditorDataGetter?.ChartSeconds
                .Subscribe(seconds => {
                    UpdateChartLength(seconds, optionGetter.ChartViewScale.Value);
                })
                .AddTo(this.gameObject);

            // エディタノーツモード変更の際カメラオンオフ切り替え
            chartEditorDataGetter?.EditNoteType
                .Subscribe(mode => {
                    setActiveObject.SetActive(mode == EditNoteType.Ground || mode == EditNoteType.Space);
                })
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// 譜面の長さの更新
        /// </summary>
        /// <param name="music"></param>
        /// <param name="scale"></param>
        private void UpdateChartLength(float chartSeconds, float scale)
        {
            chartLength = chartSeconds * scale;
        }

        /// <summary>
        /// カメラ視点の移動
        /// </summary>
        private void MoveCamera(float ratio)
        {
            viewCameraParent.position = new Vector3(
                viewCameraParent.position.x,
                viewCameraParent.position.y,
                chartLength * ratio
                );
        }

        /// <summary>
        /// オフセットに対応したカメラ位置の調整
        /// </summary>
        private void ChangeCameraOffset(float offset)
        {
            float z = offset / 1000 * optionGetter.ChartViewScale.Value;
            offsetAxis.transform.localPosition = new Vector3(
                offsetAxis.transform.localPosition.x,
                z,
                offsetAxis.transform.localPosition.z
                );
        }
    }

}
