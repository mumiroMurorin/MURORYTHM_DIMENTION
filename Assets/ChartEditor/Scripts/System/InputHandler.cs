using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] KeyCode playKey = KeyCode.Space;

        [Header("マウス関係")]
        [Tooltip("拡大縮小の感度")]
        [SerializeField] float scalingSensitivity = 0.1f;
        [Tooltip("再生位置移動の感度基準")]
        [SerializeField] float moveSensitivityMax = 0.01f;

        [Tooltip("譜面エクスポート")]
        [SerializeField] ChartDataExporter chartDataExporter;

        IChartEditorDataSetter dataSetter;
        IChartEditorOptionSetter optionSetter;
        IChartEditorDataGetter dataGetter;
        IChartEditorOptionGetter optionGetter;

        [Inject]
        public void Construct(IChartEditorDataSetter dataSetter, IChartEditorDataGetter dataGetter, IChartEditorOptionSetter optionSetter, IChartEditorOptionGetter optionGetter)
        {
            this.dataSetter = dataSetter;
            this.dataGetter = dataGetter;

            this.optionSetter = optionSetter;
            this.optionGetter = optionGetter;
        }

        private void Update()
        {
            OperateMusicPlay();
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

            optionSetter?.SetChartViewScale(optionGetter.ChartViewScale.Value + scroll * scalingSensitivity);
        }

        /// <summary>
        /// 再生位置の操作
        /// </summary>
        private void OperatePlaybackProgress()
        {
            // 再生中は操作を受け付けない
            if(dataGetter.PlayMode.Value == PlayMode.Play) { return; }

            var scroll = Input.mouseScrollDelta.y;

            if (Mathf.Abs(scroll) < 0.01f) { return; }
            if (Input.GetKey(KeyCode.LeftControl)) { return; }

            // スクロール感度と拡大率によって変える
            float ratio = moveSensitivityMax * optionGetter.ScrollSensitivity.Value * Mathf.Clamp(10f - optionGetter.ChartViewScale.Value / 0.15f, 1f, 10f);
            dataSetter?.SetPlaybackProgress(dataGetter.PlaybackProgress.Value + scroll * ratio);
        }

        /// <summary>
        /// 楽曲再生/停止の操作
        /// </summary>
        private void OperateMusicPlay()
        {
            if (!Input.GetKeyDown(playKey)) { return; }

            switch (dataGetter.PlayMode.Value)
            {
                case PlayMode.Play:
                    dataSetter?.SetPlayMode(PlayMode.Stop);
                    break;
                case PlayMode.Stop:
                    dataSetter?.SetPlayMode(PlayMode.Play);
                    break;
            }

        }

        private void SaveChart()
        {
            //if(!Input.GetKeyDown())
        }
    }
}