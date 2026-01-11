using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;

namespace ChartEditor
{
    public class SubdivisionLineInfoView : MonoBehaviour
    {
        [Header("BPM")]
        [SerializeField] GameObject bpmObject;
        [SerializeField] TextMeshPro bpmTmp;

        [Header("SpeedRatio")]
        [SerializeField] GameObject speedRatioObject;
        [SerializeField] TextMeshPro speedRatioTmp;

        /// <summary>
        /// BPM情報をUIにセット
        /// </summary>
        /// <param name="bpm"> -1 : 表示しない </param>
        public void SetBPM(float bpm = -1)
        {
            if (bpmTmp == null) { return; }

            bpmTmp.text = bpm == -1 ? "" : bpm.ToString();
            bpmObject.SetActive(bpm != -1);
        }

        /// <summary>
        /// スピード倍率をUIにセット
        /// </summary>
        /// <param name="speedRatio"></param>
        public void SetSpeedRatio(float speedRatio, bool isDiffBack)
        {
            if (speedRatioTmp == null) { return; }

            speedRatioTmp.text = !isDiffBack ? "" : "×<size=0.15>" + speedRatio.ToString();
            speedRatioObject.SetActive(isDiffBack);
        }
    }

}
