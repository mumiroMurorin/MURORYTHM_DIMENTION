using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

namespace ChartEditor
{
    public class RhythmConfigBarView : MonoBehaviour
    {
        [SerializeField] TMP_InputField beatCountField;
        [SerializeField] TMP_InputField beatUnitField;
        [SerializeField] TMP_InputField divisionNumField;

        public Action OnClickedApplyButtonListner { get; set; }

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
        public void SetDataOnUI(int beatCount = -1, float beatUnit = -1, int divNum = -1)
        {
            // 表示非表示
            beatCountField.interactable = beatCount != -1 || beatUnit != -1;
            beatUnitField.interactable = beatUnit != -1;
            divisionNumField.interactable = divNum != -1;

            // 数値のセット
            beatCountField.text = beatCount == -1 || beatUnit == -1 ? "" : beatCount.ToString();
            beatUnitField.text = beatCount == -1 || beatUnit == -1 ? "" : beatUnit.ToString();
            divisionNumField.text = divNum == -1 ? "" : divNum.ToString();
        }

        /// <summary>
        /// 小節線に対するデータのセット
        /// </summary>
        /// <param name="barData"></param>
        public void SetData(ChartData chartData, int barIndex)
        {
            int beatCount = 1;
            float beatUnit = 1;
            int divNum = 1;

            // BeatCountのセット
            if (string.IsNullOrWhiteSpace(beatCountField.text) || !int.TryParse(beatCountField.text, out beatCount))
            {
                Debug.Log($"BeatUnitに有効な数字を入力してください: {beatCount}");
                return;
            }

            // BeatUnitのセット
            if (string.IsNullOrWhiteSpace(beatUnitField.text) || !float.TryParse(beatUnitField.text, out beatUnit)) 
            {
                Debug.Log($"BeatUnitに有効な数字を入力してください: {beatUnit}");
                return;
            }

            // DivisionNumのセット
            if (string.IsNullOrWhiteSpace(divisionNumField.text) || !int.TryParse(divisionNumField.text, out divNum))
            {
                Debug.Log($"DivisionNumに有効な数字を入力してください: {divNum}");
                return;
            }

            chartData.SetBarDataProperty(barIndex, beatCount, beatUnit, divNum);
        }

        public void OnClickedApplyButton()
        {
            OnClickedApplyButtonListner.Invoke();
        }
    }
}