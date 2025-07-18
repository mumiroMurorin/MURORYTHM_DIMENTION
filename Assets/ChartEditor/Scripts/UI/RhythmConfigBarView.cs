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
        public void SetDataOnUI(BarConfig barConfig)
        {
            // 表示非表示
            beatCountField.interactable = barConfig.BeatCount != -1 || barConfig.BeatUnit != -1;
            beatUnitField.interactable = barConfig.BeatUnit != -1;
            divisionNumField.interactable = barConfig.DivisionNum != -1;

            // 数値のセット
            beatCountField.text = barConfig.BeatCount == -1 || barConfig.BeatUnit == -1 ? "" : barConfig.BeatCount.ToString();
            beatUnitField.text = barConfig.BeatCount == -1 || barConfig.BeatUnit == -1 ? "" : barConfig.BeatUnit.ToString();
            divisionNumField.text = barConfig.DivisionNum == -1 ? "" : barConfig.DivisionNum.ToString();
        }

        /// <summary>
        /// 小節線に対するデータのセット
        /// </summary>
        /// <param name="barData"></param>
        public void SetData(Action<BarConfig> setBarConfig)
        {
            int beatCount = 1;
            float beatUnit = 1;
            int divNum = 1;

            // BeatCountのセット
            if (string.IsNullOrWhiteSpace(beatCountField.text) || !int.TryParse(beatCountField.text, out beatCount))
            {
                Debug.Log($"BeatUnitに有効な数字を入力してください: {beatCountField.text}");
                return;
            }

            // BeatUnitのセット
            if (string.IsNullOrWhiteSpace(beatUnitField.text) || !float.TryParse(beatUnitField.text, out beatUnit)) 
            {
                Debug.Log($"BeatUnitに有効な数字を入力してください: {beatUnitField.text}");
                return;
            }

            // DivisionNumのセット
            if (string.IsNullOrWhiteSpace(divisionNumField.text) || !int.TryParse(divisionNumField.text, out divNum))
            {
                Debug.Log($"DivisionNumに有効な数字を入力してください: {divisionNumField.text}");
                return;
            }

            var barConfig = new BarConfig(beatCount, beatUnit, divNum);
            setBarConfig.Invoke(barConfig);
        }

        public void OnClickedApplyButton()
        {
            OnClickedApplyButtonListner.Invoke();
        }
    }
}