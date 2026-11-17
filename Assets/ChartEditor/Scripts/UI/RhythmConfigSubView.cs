using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace ChartEditor
{
    public class RhythmConfigSubView : MonoBehaviour
    {
        [SerializeField] TMP_InputField bpmField;
        [SerializeField] TMP_InputField speedRatioField;
        [Tooltip("どの分線の設定かを書く見出し")]
        [SerializeField] TextMeshProUGUI titleText;
        [Tooltip("Cancel ボタン (Apply の右。押すと、入力した内容を適用せずに閉じる)")]
        [SerializeField] Button cancelButton;

        public Action OnClickedApplyButtonListner { get; set; }

        /// <summary>
        /// どの分線の設定を開いているかを、見出しに書く
        /// </summary>
        /// <param name="barIndex">分線がある小節 (0 始まり。1小節目が 0)</param>
        /// <param name="subDivisionIndex">小節の中での、分線の番号 (0 始まり。小節線が 0)</param>
        public void SetTarget(int barIndex, int subDivisionIndex)
        {
            if (titleText == null) { return; }

            titleText.text = barIndex < 0 || subDivisionIndex < 0 ? "" : $"{barIndex + 1} 小節目 ・ {subDivisionIndex + 1} 本目の分線";
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