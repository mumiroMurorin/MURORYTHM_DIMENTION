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

        /// <summary>
        /// UIにデータをセット
        /// </summary>
        public void SetDatas(float bpm = -1)
        {
            SetBPM(bpm);
        }

        /// <summary>
        /// 小節番号の設定
        /// </summary>
        public void SetBPM(float bpm)
        {
            if (bpmTmp == null) { return; }

            bpmTmp.text = bpm == -1 ? "" : bpm.ToString();
            bpmObject.SetActive(bpm != -1);
        }
    }

}
