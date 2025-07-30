using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;

namespace ChartEditor
{
    public class BarLineInfoView : MonoBehaviour
    {
        [Header("è¨êﬂî‘çÜ")]
        [SerializeField] TextMeshPro numberTmp;
        [Header("îèéq")]
        [SerializeField] GameObject beatObject;
        [SerializeField] TextMeshPro beatCountTmp;
        [SerializeField] TextMeshPro beatUnitTmp;
        [Header("ï™äÑêî")]
        [SerializeField] GameObject divisionObject;
        [SerializeField] TextMeshPro divisionNumTmp;

        bool isChangeBeatCount;
        bool isChangeBeatUnit;

        /// <summary>
        /// è¨êﬂî‘çÜÇÃê›íË
        /// </summary>
        public void SetBarNumber(int number)
        {
            if (numberTmp == null) { return; }

            if (number == -1) { numberTmp.text = ""; }
            else { numberTmp.text = number.ToString(); }
        }

        public void SetBeatCount(int beatCount)
        {
            if (beatCountTmp == null) { return; }

            isChangeBeatCount = beatCount != -1;
            beatCountTmp.text = isChangeBeatCount ? beatCount.ToString() : beatCountTmp.text;
            beatObject.SetActive(isChangeBeatCount || isChangeBeatUnit);
        }

        public void SetBeatUnit(float beatUnit)
        {
            if (beatUnitTmp == null) { return; }

            isChangeBeatUnit = beatUnit != -1;
            beatCountTmp.text = isChangeBeatUnit ? beatUnit.ToString() : beatCountTmp.text;
            beatObject.SetActive(isChangeBeatCount || isChangeBeatUnit);
        }

        /// <summary>
        /// ï™äÑêîÇÃê›íË
        /// </summary>
        public void SetDivisionNum(int divNum)
        {
            if (divisionNumTmp == null) { return; }

            if (divNum == -1) { divisionNumTmp.text = ""; }
            else { divisionNumTmp.text = divNum.ToString() + "ï™äÑ"; }

            divisionObject.SetActive(divNum != -1);
        }
    }

}
