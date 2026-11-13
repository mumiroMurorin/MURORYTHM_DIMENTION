using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.EventSystems;

namespace ChartEditor
{
    public class NoteSpeedSliderView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI noteSpeedValueText;
        [SerializeField] SliderView slider;

        /// <summary>
        /// スライダーを動かし終えたとき (動かした値は、ボタンを押さなくてもそのまま反映する)
        /// </summary>
        public event Action<float> OnNoteSpeedChangedListener;

        // 反映済みの値と、まだ反映していない値があるか
        float appliedValue = float.NaN;
        bool hasPendingValue;

        // 設定値をスライダーへ写している最中か (利用者の操作と区別するため)
        bool isShowingModelValue;

        private void OnEnable()
        {
            slider.OnSliderChangedListener += OnSliderChanged;
        }

        private void OnDisable()
        {
            // 登録時と同じメソッドを渡さないと解除されない (ラムダ式は毎回別物になる)
            slider.OnSliderChangedListener -= OnSliderChanged;
            hasPendingValue = false;
        }

        private void Update()
        {
            if (!hasPendingValue) { return; }

            // スライダーを掴んで動かしている間は待ち、離したときに反映する
            // (動かしている途中の値ごとにプレビューを並べ直すと、そのたびにノーツの位置が飛ぶ)
            if (Input.GetMouseButton(0)) { return; }

            hasPendingValue = false;
            ReleaseKeyboardFocus();

            float value = slider.SliderValue;
            if (Mathf.Approximately(value, appliedValue)) { return; }

            appliedValue = value;
            OnNoteSpeedChangedListener?.Invoke(value);
        }

        /// <summary>
        /// スライダーを、キー操作の対象から外す
        /// (スライダーは動かした後も選ばれたままになり、左右キーや A・D キーで値が変わってしまう。
        ///  譜面の操作で押したキーでノートスピードが変わらないようにする)
        /// </summary>
        private void ReleaseKeyboardFocus()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) { return; }

            var selected = eventSystem.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(this.transform))
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        /// <summary>
        /// スライダーが動いたとき (利用者の操作でも、下の OnValueChanged で値を入れたときでも呼ばれる)
        /// </summary>
        private void OnSliderChanged(float value)
        {
            SetValueText(value);

            // 設定値を写しただけのときは、反映し直さない
            // (スライダーの範囲に収まらない設定値を、勝手に書き換えてしまわないように)
            if (isShowingModelValue) { return; }

            // 反映済みの値と違うときだけ、後で反映する
            hasPendingValue = !Mathf.Approximately(value, appliedValue);
        }

        /// <summary>
        /// ノートスピードの設定値が変わったとき (設定値 → 表示)
        /// </summary>
        public void OnValueChanged(float value)
        {
            // 先に「反映済み」として覚えてから表示を変える (表示の変化で、もう一度反映しに行かないように)
            appliedValue = value;
            hasPendingValue = false;

            isShowingModelValue = true;
            try
            {
                slider.OnValueChanged(value);
            }
            finally
            {
                isShowingModelValue = false;
            }

            SetValueText(value);
        }

        private void SetValueText(float value)
        {
            if (noteSpeedValueText != null)
            {
                noteSpeedValueText.text = value.ToString("F1");
            }
        }
    }
}
