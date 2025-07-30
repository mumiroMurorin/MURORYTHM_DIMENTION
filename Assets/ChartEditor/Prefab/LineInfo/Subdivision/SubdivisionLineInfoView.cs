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
        /// BPM情報をUIにセット
        /// </summary>
        /// <param name="bpm"> -1 : 表示しない </param>
        public void SetBPM(float bpm = -1)
        {
            if (bpmTmp == null) { return; }

            bpmTmp.text = bpm == -1 ? "" : bpm.ToString();
            bpmObject.SetActive(bpm != -1);
        }
    }

}
