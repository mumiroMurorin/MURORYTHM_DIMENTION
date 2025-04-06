using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

namespace ChartEditor
{
    public class RhythmConfigView : MonoBehaviour
    {
        [SerializeField] TMP_InputField bpmField;
        [SerializeField] TMP_InputField beatCountField;
        [SerializeField] TMP_InputField beatUnitField;
        [SerializeField] TMP_InputField divisionNumField;

        public Action OnClickedDecisionButtonListner { get; set; }

        public void SetActive(bool value)
        {
            this.gameObject.SetActive(value);
        }

        /// <summary>
        /// その時点の引数のデータ代入
        /// bpm以外 -1 で非表示
        /// </summary>
        /// <param name="bpm"></param>
        /// <param name="beatCount"></param>
        /// <param name="beatUnit"></param>
        /// <param name="divNum"></param>
        public void SetDataOnUI(float bpm, int beatCount = -1, float beatUnit = -1, int divNum = -1)
        {
            // 表示非表示
            beatCountField.interactable = beatCount != -1 || beatUnit != -1;
            beatUnitField.interactable = beatUnit != -1;
            divisionNumField.interactable = divNum != -1;

            // 数値のセット
            bpmField.text = bpm.ToString();
            beatCountField.text = beatCount == -1 || beatUnit == -1 ? "" : beatCount.ToString();
            beatUnitField.text = beatCount == -1 || beatUnit == -1 ? "" : beatUnit.ToString();
            divisionNumField.text = divNum == -1 ? "" : divNum.ToString();
        }

        /// <summary>
        /// 小節線に対するデータのセット
        /// </summary>
        /// <param name="barData"></param>
        public void SetData(BarDataInChart barData)
        {
            // BPMのセット
            if (!string.IsNullOrWhiteSpace(bpmField.text) && float.TryParse(bpmField.text, out float bpm)) 
            { 
                foreach(var subdivision in barData.SubDivisionDatas)
                {
                    subdivision.SetBpm(bpm);
                }
            }

            // BeatCount、BeatUnitのセット
            if (!string.IsNullOrWhiteSpace(beatCountField.text) && int.TryParse(beatCountField.text, out int beatCount) &&
                !string.IsNullOrWhiteSpace(beatUnitField.text) && float.TryParse(beatUnitField.text, out float beatUnit)) 
            {
                barData.SetBeatCount(beatCount);
                barData.SetBeatUnit(beatUnit);
            }

            // DivisionNumのセット
            if (!string.IsNullOrWhiteSpace(divisionNumField.text) && int.TryParse(divisionNumField.text, out int divNum))
            {
                barData.SetDivisionNum(divNum);
            }
        }

        public void OnClickedDecisionButton()
        {
            OnClickedDecisionButtonListner.Invoke();
        }
    }
}