using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace ChartEditor
{
    public class RhythmConfigBarView : MonoBehaviour
    {
        [SerializeField] TMP_InputField beatCountField;
        [SerializeField] TMP_InputField beatUnitField;
        [SerializeField] TMP_InputField divisionNumField;
        [Tooltip("どの小節の設定かを書く見出し")]
        [SerializeField] TextMeshProUGUI titleText;
        [Tooltip("Cancel ボタン (Apply の右。押すと、入力した内容を適用せずに閉じる)")]
        [SerializeField] Button cancelButton;

        public Action OnClickedApplyButtonListner { get; set; }

        /// <summary>
        /// どの小節の設定を開いているかを、見出しに書く
        /// </summary>
        /// <param name="barIndex">小節 (0 始まり。1小節目が 0)</param>
        public void SetTarget(int barIndex)
        {
            if (titleText == null) { return; }

            titleText.text = barIndex < 0 ? "" : $"{barIndex + 1} 小節目";
        }

        /// <summary>
        /// Cancel ボタンが押されたとき (入力した内容を適用せずに閉じる)
        /// </summary>
        public Action OnClickedCancelButtonListner { get; set; }

        private void Awake()
        {
            if (cancelButton != null) { cancelButton.onClick.AddListener(OnClickedCancelButton); }
        }

        private void OnClickedCancelButton()
        {
            // 押したボタンが選択中のまま残らないようにする (残ると、Space キーや Enter キーでもう一度押されてしまう)
            if (EventSystem.current != null) { EventSystem.current.SetSelectedGameObject(null); }

            OnClickedCancelButtonListner?.Invoke();
        }

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
            if (string.IsNullOrWhiteSpace(beatCountField.text) || !int.TryParse(beatCountField.text, out beatCount) || beatCount < 1)
            {
                ShowInputError($"拍子の分子には1以上の整数を入力してください。\n入力値: {beatCountField.text}");
                return;
            }

            // BeatUnitのセット
            if (string.IsNullOrWhiteSpace(beatUnitField.text) || !float.TryParse(beatUnitField.text, out beatUnit) || !(beatUnit > 0f) || float.IsInfinity(beatUnit))
            {
                ShowInputError($"拍子の分母には0より大きい数を入力してください。\n入力値: {beatUnitField.text}");
                return;
            }

            // DivisionNumのセット
            if (string.IsNullOrWhiteSpace(divisionNumField.text) || !int.TryParse(divisionNumField.text, out divNum) || divNum < 1)
            {
                ShowInputError($"分割数には1以上の整数を入力してください。\n入力値: {divisionNumField.text}");
                return;
            }

            var barConfig = new BarConfig(beatCount, beatUnit, divNum);
            setBarConfig.Invoke(barConfig);
        }

        private void ShowInputError(string message)
        {
            MessageDialog.ShowError("小節の設定を変更できません", message);
        }

        public void OnClickedApplyButton()
        {
            OnClickedApplyButtonListner.Invoke();
        }
    }
}