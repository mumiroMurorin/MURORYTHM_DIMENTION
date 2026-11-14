using System;
using TMPro;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 譜面を延長するときの設定の入力欄 (延長する小節数、延長分の拍子、延長分の分割数)
    ///
    /// 入力欄は、シーンに最初から置いてある。同じものが2か所にある:
    /// クイックメニュー (左下の「譜面長さ」) と、画面上のメニューの「譜面」。
    /// ここでは、入力された値を知らせることと、渡された値を表示することだけを行う。
    /// 値を持っているのは ChartEditorOptionHolder で、つなぐのは ChartExtendSettings.Bind。
    /// </summary>
    public class ChartExtendSettingsView : MonoBehaviour
    {
        [Tooltip("延長する小節数")]
        [SerializeField] TMP_InputField barCountField;
        [Tooltip("延長分の拍子の分子 (1小節の拍の数)")]
        [SerializeField] TMP_InputField beatCountField;
        [Tooltip("延長分の拍子の分母 (1拍の長さ)")]
        [SerializeField] TMP_InputField beatUnitField;
        [Tooltip("延長分の分割数")]
        [SerializeField] TMP_InputField divisionNumField;

        bool isInitialized;

        /// <summary>
        /// 値が打ち込まれたとき (入力を終えた時点で、数として読めたときだけ)
        /// </summary>
        public Action<int> OnBarCountEnteredListener { get; set; }
        public Action<int> OnBeatCountEnteredListener { get; set; }
        public Action<float> OnBeatUnitEnteredListener { get; set; }
        public Action<int> OnDivisionNumEnteredListener { get; set; }

        /// <summary>
        /// 数として読めないものが打ち込まれたとき (空のまま入力を終えたときなど)
        /// </summary>
        public Action OnInvalidInputListener { get; set; }

        public void SetBarCount(int value) { SetText(barCountField, value.ToString()); }
        public void SetBeatCount(int value) { SetText(beatCountField, value.ToString()); }
        public void SetBeatUnit(float value) { SetText(beatUnitField, value.ToString("0.###")); }
        public void SetDivisionNum(int value) { SetText(divisionNumField, value.ToString()); }

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// 入力欄の準備 (最初の1回だけ)
        /// 画面上のメニューの入力欄は、メニューを開くまで非表示なので、値を表示するとき(SetBarCount など)にも呼ぶ
        /// </summary>
        private void Initialize()
        {
            if (isInitialized) { return; }
            isInitialized = true;

            SetUpIntField(barCountField, value => OnBarCountEnteredListener?.Invoke(value));
            SetUpIntField(beatCountField, value => OnBeatCountEnteredListener?.Invoke(value));
            SetUpIntField(divisionNumField, value => OnDivisionNumEnteredListener?.Invoke(value));

            if (beatUnitField != null)
            {
                beatUnitField.contentType = TMP_InputField.ContentType.DecimalNumber;
                beatUnitField.onEndEdit.AddListener(text =>
                {
                    if (float.TryParse(text, out float value) && !float.IsNaN(value)) { OnBeatUnitEnteredListener?.Invoke(value); }
                    else { OnInvalidInputListener?.Invoke(); }

                    InputFieldFocus.Release(beatUnitField);
                });
            }
        }

        private void SetUpIntField(TMP_InputField field, Action<int> onEntered)
        {
            if (field == null) { return; }

            field.contentType = TMP_InputField.ContentType.IntegerNumber;
            field.onEndEdit.AddListener(text =>
            {
                if (int.TryParse(text, out int value)) { onEntered(value); }
                else { OnInvalidInputListener?.Invoke(); }

                // 入力を終えたら、入力欄を「選択中」のままにしない
                InputFieldFocus.Release(field);
            });
        }

        private void SetText(TMP_InputField field, string text)
        {
            Initialize();

            if (field != null && field.text != text) { field.SetTextWithoutNotify(text); }
        }
    }
}
