using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;

namespace ChartEditor
{
    public class RhythmConfigSubView : MonoBehaviour
    {
        [SerializeField] TMP_InputField bpmField;
        [SerializeField] TMP_InputField speedRatioField;

        public Action OnClickedApplyButtonListner { get; set; }

        /// <summary>
        /// Cancel ボタンが押されたとき (入力した内容を適用せずに閉じる)
        /// </summary>
        public Action OnClickedCancelButtonListner { get; set; }

        // Cancel ボタンを作り終えたか
        bool hasCancelButton;

        public void SetActive(bool value)
        {
            // Cancel ボタンは、最初に開くときに Apply ボタンを複製して作る
            if (value && !hasCancelButton)
            {
                hasCancelButton = true;
                DialogCancelButton.Create(this.transform, () => OnClickedCancelButtonListner?.Invoke());
            }

            this.gameObject.SetActive(value);
        }

        /// <summary>
        /// その時点の引数のデータ代入
        /// bpm以外 -1 で非表示
        /// </summary>
        public void SetDataOnUI(SubdivisionConfig subConfig)
        {
            // 表示非表示
            bpmField.interactable = subConfig.Bpm != -1;

            // 数値のセット
            bpmField.text = subConfig.Bpm == -1 ? "" : subConfig.Bpm.ToString();
            speedRatioField.text = subConfig.SpeedRatio.ToString();
        }

        /// <summary>
        /// 分線に対するデータのセット
        /// </summary>
        /// <param name="barData"></param>
        public void SetData(SubDivisionDataInBeat subDivisionData)
        {
            // BPMのセット
            if (!string.IsNullOrWhiteSpace(bpmField.text) && float.TryParse(bpmField.text, out float bpm))
            {
                subDivisionData.SetBpm(bpm);
            }

            // SpeedRatioのセット
            if (!string.IsNullOrWhiteSpace(speedRatioField.text) && float.TryParse(speedRatioField.text, out float speedRatio))
            {
                subDivisionData.SetSpeedRatio(speedRatio);
            }
        }

        /// <summary>
        ///  以降の分線全部BPMセット
        /// </summary>
        /// <param name="subDivisionData"></param>
        public void SetData(Action<SubdivisionConfig> setSubdivisionConfig)
        {
            float bpm = 1;
            float speedRatio = 1;

            // BPMのセット
            if (string.IsNullOrWhiteSpace(bpmField.text) || !float.TryParse(bpmField.text, out bpm) || !(bpm > 0f) || float.IsInfinity(bpm))
            {
                MessageDialog.ShowError("分線の設定を変更できません", $"BPMには0より大きい数を入力してください。\n入力値: {bpmField.text}");
                return;
            }

            // SpeedRatioのセット
            if (string.IsNullOrWhiteSpace(speedRatioField.text) || !float.TryParse(speedRatioField.text, out speedRatio) || float.IsNaN(speedRatio) || float.IsInfinity(speedRatio))
            {
                MessageDialog.ShowError("分線の設定を変更できません", $"スピード倍率には数値を入力してください。\n入力値: {speedRatioField.text}");
                return;
            }

            var subdivConfig = new SubdivisionConfig(bpm, speedRatio);
            setSubdivisionConfig.Invoke(subdivConfig);
        }

        public void OnClickedApplyButton()
        {
            OnClickedApplyButtonListner.Invoke();
        }
    }
}