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

        public Action OnClickedApplyButtonListner { get; set; }

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
        }

        /// <summary>
        ///  以降の分線全部BPMセット
        /// </summary>
        /// <param name="subDivisionData"></param>
        public void SetData(Action<SubdivisionConfig> setSubdivisionConfig)
        {
            float bpm = 1;

            // BPMのセット
            if (string.IsNullOrWhiteSpace(bpmField.text) || !float.TryParse(bpmField.text, out bpm))
            {
                Debug.Log($"DivisionNumに有効な数字を入力してください: {bpmField.text}");
                return;
            }

            var subdivConfig = new SubdivisionConfig(bpm);
            setSubdivisionConfig.Invoke(subdivConfig);
        }

        public void OnClickedApplyButton()
        {
            OnClickedApplyButtonListner.Invoke();
        }
    }
}