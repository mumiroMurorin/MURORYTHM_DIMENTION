using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

namespace ChartEditor
{
    public class LaneController : MonoBehaviour
    {
        [SerializeField] List<SerializeInterface<ILaneDeployable>> deplayables;
        [SerializeField] GameObject viewCamera;
        [SerializeField] GameObject ground;

        IChartEditorDataGetter chartEditorDataGetter;
        float currentScale = 1f;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        void Start()
        {
            Initialize();
            Bind();
        }

        private void Initialize()
        {

        }

        private void Bind()
        {
            // 拡大率
            chartEditorDataGetter?.ChartViewScale
                .Subscribe(scale => {
                    OnChangeChartViewScale(scale);
                    OnChangePlaybackProgress(chartEditorDataGetter.PlaybackProgress.Value);
                })
                .AddTo(this.gameObject);

            // 再生位置
            chartEditorDataGetter?.PlaybackProgress
                .Subscribe(OnChangePlaybackProgress)
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// 拡大率より拡大縮小を行う
        /// </summary>
        /// <param name="scale"></param>
        private void OnChangeChartViewScale(float scale)
        {
            // 各線
            foreach (SerializeInterface<ILaneDeployable> deployable in deplayables)
            {
                deployable.Value.Scaling(scale);
            }

            // グラウンド
            ground.transform.localScale = new Vector3(
                ground.transform.localScale.x,
                ground.transform.localScale.y * (scale / currentScale),
                ground.transform.localScale.z);

            ground.transform.position = new Vector3(
                ground.transform.position.x,
                ground.transform.position.y,
                ground.transform.localScale.y / 2f
                );

            currentScale = scale;
        }

        /// <summary>
        /// カメラ視点の移動
        /// </summary>
        private void OnChangePlaybackProgress(float ratio) 
        {
            viewCamera.transform.position = new Vector3(
                viewCamera.transform.position.x,
                viewCamera.transform.position.y,
                ground.transform.localScale.y * ratio
                );
        }
    }

}
