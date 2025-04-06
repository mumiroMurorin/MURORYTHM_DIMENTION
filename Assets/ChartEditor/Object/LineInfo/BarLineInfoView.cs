using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;

namespace ChartEditor
{
    public class BarLineInfoView : MonoBehaviour
    {
        [Header("UIs")]
        [SerializeField] TextMeshPro[] numberTmps;
        [SerializeField] TextMeshPro[] bpmTmps;
        [SerializeField] TextMeshPro[] beatCountTmps;
        [SerializeField] TextMeshPro[] beatUnitTmps;
        [SerializeField] TextMeshPro[] divisionNumTmps;

        /// <summary>
        /// UIにデータをセット
        /// </summary>
        /// <param name="number"></param>
        /// <param name="bpm"></param>
        /// <param name="beatCount"></param>
        /// <param name="beatUnit"></param>
        public void SetDatas(int barNumber = -1, float bpm = -1, int beatCount = -1, float beatUnit = -1, int divNum = -1)
        {
            SetBarNumber(barNumber);
            SetBPM(bpm);
            SetBeatCount(beatCount);
            SetBeatUnit(beatCount);
            SetDivisionNum(divNum);
        }

        /// <summary>
        /// 小節番号の設定
        /// </summary>
        public void SetBarNumber(int number)
        {
            if (numberTmps == null) { return; }

            foreach (TextMeshPro tmp in numberTmps)
            {
                if(number == -1) { tmp.text = ""; }
                else { tmp.text = number.ToString(); }
            }
        }

        /// <summary>
        /// BPMの設定
        /// </summary>
        public void SetBPM(float bpm)
        {
            if (bpmTmps == null) { return; }

            foreach (TextMeshPro tmp in bpmTmps)
            {
                if (bpm == -1) { tmp.text = ""; }
                else { tmp.text = "BPM" + bpm.ToString(); }
            }
        }

        /// <summary>
        /// N分のM拍子のMの設定
        /// </summary>
        public void SetBeatCount(int beatCount)
        {
            if (beatCountTmps == null) { return; }

            foreach (TextMeshPro tmp in beatCountTmps)
            {
                if (beatCount == -1) { tmp.text = ""; }
                else { tmp.text = beatCount.ToString(); }
            }
        }

        /// <summary>
        /// N分のM拍子のNの設定
        /// </summary>
        public void SetBeatUnit(float beatUnit)
        {
            if (beatUnitTmps == null) { return; }

            foreach (TextMeshPro tmp in beatUnitTmps)
            {
                if (beatUnit == -1) { tmp.text = ""; }
                else { tmp.text = beatUnit.ToString(); }
            }
        }

        /// <summary>
        /// 分割数の設定
        /// </summary>
        public void SetDivisionNum(int divNum)
        {
            if (divisionNumTmps == null) { return; }

            foreach (TextMeshPro tmp in divisionNumTmps)
            {
                if (divNum == -1) { tmp.text = ""; }
                else { tmp.text = divNum.ToString() + "分割"; }
            }
        }
    }

}
