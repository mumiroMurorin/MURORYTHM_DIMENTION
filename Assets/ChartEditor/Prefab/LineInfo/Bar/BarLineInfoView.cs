using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;

namespace ChartEditor
{
    public class BarLineInfoView : MonoBehaviour
    {
        [Header("小節番号")]
        [SerializeField] TextMeshPro numberTmp;
        [Header("拍子")]
        [SerializeField] GameObject beatObject;
        [SerializeField] TextMeshPro beatCountTmp;
        [SerializeField] TextMeshPro beatUnitTmp;
        [Header("分割数")]
        [SerializeField] GameObject divisionObject;
        [SerializeField] TextMeshPro divisionNumTmp;

        /// <summary>
        /// UIにデータをセット
        /// </summary>
        /// <param name="number"></param>
        /// <param name="bpm"></param>
        /// <param name="beatCount"></param>
        /// <param name="beatUnit"></param>
        public void SetDatas(int barNumber = -1,int beatCount = -1, float beatUnit = -1, int divNum = -1)
        {
            SetBarNumber(barNumber);
            SetBeatCountAndBeatUnit(beatCount, beatUnit);
            SetDivisionNum(divNum);
        }

        /// <summary>
        /// 小節番号の設定
        /// </summary>
        public void SetBarNumber(int number)
        {
            if (numberTmp == null) { return; }

            if (number == -1) { numberTmp.text = ""; }
            else { numberTmp.text = number.ToString(); }
        }

        /// <summary>
        /// 拍子の設定
        /// </summary>
        public void SetBeatCountAndBeatUnit(int beatCount, float beatUnit)
        {
            if (beatCountTmp == null) { return; }
            if (beatUnitTmp == null) { return; }

            beatObject.SetActive(beatCount != -1 || beatUnit != -1);

            // 非表示
            if (beatCount == -1 && beatUnit == -1)
            {
                beatCountTmp.text = "";
                beatUnitTmp.text = "";
            }

            beatCountTmp.text = beatCount != -1 ? beatCount.ToString() : beatCountTmp.text;
            beatUnitTmp.text = beatUnit != -1 ? beatUnit.ToString() : beatUnitTmp.text;
        }

        /// <summary>
        /// 分割数の設定
        /// </summary>
        public void SetDivisionNum(int divNum)
        {
            if (divisionNumTmp == null) { return; }

            if (divNum == -1) { divisionNumTmp.text = ""; }
            else { divisionNumTmp.text = divNum.ToString() + "分割"; }

            divisionObject.SetActive(divNum != -1);
        }
    }

}
