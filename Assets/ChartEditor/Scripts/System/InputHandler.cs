using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] AudioClip kariMusic;
        [SerializeField] float kariBpm;

        [SerializeField] KeyCode playKey = KeyCode.Space;

        [Header("マウス関係")]
        [Tooltip("拡大縮小の感度")]
        [SerializeField] float scalingSensitivity = 0.1f;
        [Tooltip("再生位置移動の感度")]
        [SerializeField] float moveSensitivity = 0.01f;

        IChartEditorDataSetter chartEditorDataSetter;
        IChartEditorDataGetter chartEditorDataGetter;

        [Inject] 
        public void Construct(IChartEditorDataSetter chartEditorDataSetter, IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataSetter = chartEditorDataSetter;
            this.chartEditorDataGetter = chartEditorDataGetter;

            chartEditorDataSetter.SetMusic(kariMusic);
            chartEditorDataSetter.SetMainBpm(kariBpm);
        }

        private void Update()
        {
            if (Input.GetKeyDown(playKey))
            {
                switch (chartEditorDataGetter.PlayMode.Value)
                {
                    case PlayMode.Play:
                        chartEditorDataSetter.SetPlayMode(PlayMode.Stop);
                        break;
                    case PlayMode.Stop:
                        // 音楽を流す
                        chartEditorDataSetter?.SetPlayMode(PlayMode.Play);
                        break;
                }
            }

            OperateChartViewScale();
            OperatePlaybackProgress();
        }

        /// <summary>
        /// 拡大率の操作
        /// </summary>
        private void OperateChartViewScale()
        {
            var scroll = Input.mouseScrollDelta.y;

            if (Mathf.Abs(scroll) < 0.01f) { return; }
            if (!Input.GetKey(KeyCode.LeftControl)) { return; }

            chartEditorDataSetter?.SetChartViewScale(chartEditorDataGetter.ChartViewScale.Value + scroll * scalingSensitivity);
        }

        /// <summary>
        /// 再生位置の操作
        /// </summary>
        private void OperatePlaybackProgress()
        {
            // 再生中は操作を受け付けない
            if(chartEditorDataGetter.PlayMode.Value == PlayMode.Play) { return; }

            var scroll = Input.mouseScrollDelta.y;

            if (Mathf.Abs(scroll) < 0.01f) { return; }
            if (Input.GetKey(KeyCode.LeftControl)) { return; }

            // スクロール感度と拡大率によって変える
            float ratio = moveSensitivity * Mathf.Clamp(10f - chartEditorDataGetter.ChartViewScale.Value / 0.15f, 1f, 10f);
            chartEditorDataSetter?.SetPlaybackProgress(chartEditorDataGetter.PlaybackProgress.Value + scroll * ratio);
        }
    }
}