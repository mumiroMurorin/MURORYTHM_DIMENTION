using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 数値の入力欄の扱い
    ///
    /// ・打っている間は、何も反映しない。入力を終えたとき (Enter、ほかの場所をクリック) に、1回だけ値を知らせる
    ///   (1文字ごとに反映すると、打っている途中の数がそのまま使われ、欄の表示も書き直されて、思ったとおりに打てない)
    /// ・Esc でやめたとき、何も変えずに離れたとき、数として読めないものを打ったときは、何も知らせず、元の表示に戻す
    /// ・全角の数字や小数点 (日本語入力のまま打ったもの) も、数として読む
    /// ・打っている間に、外から表示を変えようとしても、打っている文字は書き換えない (入力を終えたときに、その表示にする)
    /// ・ほかの場所をクリックして入力を終えたときは、そのクリックでほかの処理が動く前に、打った値を知らせる
    ///   (CommitIfClickedElsewhere を、ほかのスクリプトより先に毎フレーム呼ぶ。呼ばないと、クリックで頂点の選択などが先に変わり、
    ///    打った値は「前の対象のためのもの」として捨てられてしまう)
    /// ・EnableScrub を呼ぶと、欄の名前の文字を左右にドラッグして、値を増やす・減らすこともできる (NumberScrubHandle。録音 20261118_162040)
    /// </summary>
    public class NumberInputField
    {
        readonly TMP_InputField field;
        readonly Action<float> onValueEntered;

        // 欄に出しておきたい表示 (外から最後に指定されたもの)
        string shownText = string.Empty;
        // 実際に欄へ入れた表示 (入力を終えたとき、これと同じなら「変えていない」)
        string writtenText = string.Empty;
        // 打っている間に、外から別の表示を指定されたか (見せる対象が替わった。打っていた数は、前の対象のためのものなので使わない)
        bool isReplacedWhileTyping;

        /// <param name="field">入力欄 (null のときは、何もしない)</param>
        /// <param name="onValueEntered">数値が入力されたときに呼ぶ処理</param>
        public NumberInputField(TMP_InputField field, Action<float> onValueEntered)
        {
            this.field = field;
            this.onValueEntered = onValueEntered;
            if (field == null) { return; }

            shownText = field.text ?? string.Empty;
            writtenText = shownText;
            field.onEndEdit.AddListener(OnEndEdit);
        }

        /// <summary>
        /// 欄の名前の文字を左右にドラッグして、値を増やす・減らすことができるようにする (ドラッグしている間は、その場で反映する。
        /// 押してから離すまでの変化は、1回の「元に戻す」にまとまる)
        /// </summary>
        /// <param name="label">欄の名前の文字 (null のときは、同じ親の中で、欄のすぐ前にある文字)</param>
        public NumberInputField EnableScrub(NumberScrubHandle.Settings settings, Graphic label = null)
        {
            if (field == null) { return this; }

            label ??= NumberScrubHandle.FindLabel(field);
            NumberScrubHandle.Attach(field, label, settings, ApplyScrubbedValue);
            return this;
        }

        void ApplyScrubbedValue(float value)
        {
            onValueEntered?.Invoke(value);
            // 知らせた先が、今の値の表示を指定し直している
            Write();
        }

        /// <summary>
        /// 欄の表示を変える (今の値を見せる)
        /// </summary>
        public void SetText(string text)
        {
            text = text ?? string.Empty;
            bool isChanged = text != shownText;
            shownText = text;
            if (field == null) { return; }

            // 打っている最中の文字は書き換えない
            if (field.isFocused)
            {
                if (isChanged) { isReplacedWhileTyping = true; }
                return;
            }

            Write();
        }

        /// <summary>
        /// 打っている間に、この欄の外がクリックされたら、その場で入力を終える (打った値を知らせる)
        /// 欄を持つ画面部品が、ほかのスクリプトより先に (DefaultExecutionOrder で) 毎フレーム呼ぶ。
        /// </summary>
        public void CommitIfClickedElsewhere()
        {
            if (field == null || !field.isFocused) { return; }
            if (!Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1)) { return; }
            if (IsPointerOnField(Input.mousePosition)) { return; }

            // 入力を終える (OnEndEdit が呼ばれて、打った値を知らせる)
            field.DeactivateInputField();
        }

        bool IsPointerOnField(Vector2 screenPosition)
        {
            var rect = field.transform as RectTransform;
            if (rect == null) { return false; }

            var canvas = field.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
        }

        void Write()
        {
            writtenText = shownText;
            isReplacedWhileTyping = false;
            field.SetTextWithoutNotify(shownText);
        }

        void OnEndEdit(string value)
        {
            // Enter で終えたときに、欄が「選択中」のまま残らないようにする (Space キーで、もう一度入力状態になってしまう)
            InputFieldFocus.Release(field);

            // 変えていない (Esc でやめた、何も打たずに離れた)、または、数として読めない: 何も知らせず、今の値の表示にする
            if (value == writtenText || isReplacedWhileTyping || !TryParseNumber(value, out float number))
            {
                Write();
                return;
            }

            onValueEntered?.Invoke(number);

            // 知らせた先が、今の値の表示を指定し直している (受け付けられなかったときは、元の表示のまま)
            Write();
        }

        /// <summary>
        /// 入力された文字を、数として読む (全角の数字・小数点・符号、前後の空白も受け付ける)
        /// </summary>
        public static bool TryParseNumber(string text, out float number)
        {
            number = 0f;
            if (string.IsNullOrEmpty(text)) { return false; }

            var builder = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c >= '\uFF10' && c <= '\uFF19') { builder.Append((char)('0' + (c - '\uFF10'))); }       // 全角の数字
                else if (c == '\uFF0E') { builder.Append('.'); }                                          // 全角のピリオド
                else if (c == '\uFF0D' || c == '\u2212' || c == '\u30FC' || c == '\u2010') { builder.Append('-'); }   // 全角のマイナス、長音など
                else if (c == '\uFF0B') { builder.Append('+'); }                                          // 全角のプラス
                else if (c == '\u3000') { builder.Append(' '); }                                          // 全角の空白
                else { builder.Append(c); }
            }

            string normalized = builder.ToString().Trim();
            if (normalized.Length == 0) { return false; }

            if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) { return false; }

            return !float.IsNaN(number) && !float.IsInfinity(number);
        }
    }
}
