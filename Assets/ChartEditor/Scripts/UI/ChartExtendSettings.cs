using TMPro;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 譜面を延長するときの設定 (何小節延長するか、延長する小節の拍子と分割数)
    ///
    /// 設定パネルの「譜面長さ」の行のすぐ下に、入力欄の行を3つ追加する。
    /// 行は「OffSet」の行を実行時に複製して作る。見た目と並びは複製元と同じになる。
    /// (シーンを直接編集せずに設定項目を増やすための仕組み)
    /// </summary>
    public class ChartExtendSettings
    {
        // 一度に延長できる小節数の上限 (桁を打ち間違えたときに、延々と小節を作り続けて固まるのを防ぐ)
        public const int MAX_BAR_COUNT = 500;

        // 最初に入れておく値 (これまでの延長と同じ: 1小節、4/4拍子、4分割)
        const int DEFAULT_BAR_COUNT = 1;
        const int DEFAULT_BEAT_COUNT = 4;
        const float DEFAULT_BEAT_UNIT = 4f;
        const int DEFAULT_DIVISION_NUM = 4;

        // 拍子の行の、入力欄1つの幅と、間に置く「/」の幅
        const float BEAT_FIELD_WIDTH = 50f;
        const float SLASH_WIDTH = 25f;
        // 幅の狭い入力欄の文字の大きさ (複製元の大きさのままでは、2桁の数字が収まらない)
        const float NARROW_FIELD_FONT_SIZE = 26f;
        // 行の見出しの文字の大きさの下限 (長い見出しは、行に収まるよう自動で縮める)
        const float LABEL_FONT_SIZE_MIN = 12f;

        const string ERROR_TITLE = "譜面を延長できません";

        TMP_InputField barCountField;
        TMP_InputField beatCountField;
        TMP_InputField beatUnitField;
        TMP_InputField divisionNumField;
        // 延長ボタンに書いてある文字 (延長する小節数を書き添える)
        TMP_Text extendButtonText;

        /// <summary>
        /// 入力欄の行を作る
        /// </summary>
        /// <param name="extendButton">延長ボタン (その親が「譜面長さ」の行)</param>
        /// <param name="offsetField">複製元にする「OffSet」の入力欄 (その親が、見出しを含む行)</param>
        /// <returns>作れなかったときは null (その場合、延長は今までどおり1小節ずつになる)</returns>
        public static ChartExtendSettings Create(ChartExtendButtonView extendButton, OffsetInputFieldView offsetField)
        {
            if (extendButton == null || offsetField == null) { return null; }

            Transform chartLengthRow = extendButton.transform.parent;
            Transform templateRow = offsetField.transform.parent;
            if (chartLengthRow == null || templateRow == null || templateRow.parent == null) { return null; }
            // 同じ並びの中にある行同士でないと、すぐ下には置けない
            if (chartLengthRow.parent != templateRow.parent) { return null; }

            var settings = new ChartExtendSettings();
            int siblingIndex = chartLengthRow.GetSiblingIndex();

            settings.barCountField = CreateRow(templateRow, "ExtendBarCount", "延長する小節数", siblingIndex + 1);
            settings.beatCountField = CreateRow(templateRow, "ExtendBeat", "延長分の拍子", siblingIndex + 2);
            settings.divisionNumField = CreateRow(templateRow, "ExtendDivisionNum", "延長分の分割数", siblingIndex + 3);

            if (settings.barCountField == null || settings.beatCountField == null || settings.divisionNumField == null) { return null; }

            // 拍子の行は、入力欄を2つ並べる (分子 / 分母)
            settings.beatUnitField = SplitBeatField(settings.beatCountField);
            if (settings.beatUnitField == null) { return null; }

            SetUpField(settings.barCountField, TMP_InputField.ContentType.IntegerNumber, DEFAULT_BAR_COUNT.ToString(), false);
            SetUpField(settings.beatCountField, TMP_InputField.ContentType.IntegerNumber, DEFAULT_BEAT_COUNT.ToString(), true);
            SetUpField(settings.beatUnitField, TMP_InputField.ContentType.DecimalNumber, DEFAULT_BEAT_UNIT.ToString(), true);
            SetUpField(settings.divisionNumField, TMP_InputField.ContentType.IntegerNumber, DEFAULT_DIVISION_NUM.ToString(), false);

            // 延長ボタンに、延長する小節数を書き添える。小節数を打ち終えるたびに書き直す
            settings.extendButtonText = extendButton.GetComponentInChildren<TMP_Text>(true);
            settings.barCountField.onEndEdit.AddListener(_ => settings.RefreshExtendButtonText());
            settings.RefreshExtendButtonText();

            return settings;
        }

        /// <summary>
        /// 入力されている値を読み取る。正しくない値があるときは、理由を表示して false を返す
        /// </summary>
        /// <param name="barCount">延長する小節数</param>
        /// <param name="barConfig">延長する小節の拍子と分割数</param>
        public bool TryGetValues(out int barCount, out BarConfig barConfig)
        {
            barConfig = null;

            if (!int.TryParse(barCountField.text, out barCount) || barCount < 1 || barCount > MAX_BAR_COUNT)
            {
                MessageDialog.ShowError(ERROR_TITLE, $"延長する小節数には、1 以上 {MAX_BAR_COUNT} 以下の整数を入力してください。\n入力値: {barCountField.text}");
                return false;
            }

            if (!int.TryParse(beatCountField.text, out int beatCount) || beatCount < 1)
            {
                MessageDialog.ShowError(ERROR_TITLE, $"拍子の分子には1以上の整数を入力してください。\n入力値: {beatCountField.text}");
                return false;
            }

            if (!float.TryParse(beatUnitField.text, out float beatUnit) || !(beatUnit > 0f) || float.IsInfinity(beatUnit))
            {
                MessageDialog.ShowError(ERROR_TITLE, $"拍子の分母には0より大きい数を入力してください。\n入力値: {beatUnitField.text}");
                return false;
            }

            if (!int.TryParse(divisionNumField.text, out int divisionNum) || divisionNum < 1)
            {
                MessageDialog.ShowError(ERROR_TITLE, $"分割数には1以上の整数を入力してください。\n入力値: {divisionNumField.text}");
                return false;
            }

            barConfig = new BarConfig(beatCount, beatUnit, divisionNum);
            return true;
        }

        /// <summary>
        /// 延長ボタンの文字を、今入力されている小節数に合わせる
        /// </summary>
        private void RefreshExtendButtonText()
        {
            if (extendButtonText == null) { return; }

            // 正しくない値が入っているときは、小節数を「?」にしておく (押したときに理由が表示される)
            bool isValid = int.TryParse(barCountField.text, out int barCount) && barCount >= 1 && barCount <= MAX_BAR_COUNT;
            extendButtonText.text = $"延長\n<size=15>({(isValid ? barCount.ToString() : "?")}小節)";
        }

        #region 行を作る

        /// <summary>
        /// 複製元の行を複製して、見出しと入力欄1つの行を作る
        /// </summary>
        /// <returns>その行の入力欄。作れなかったときは null</returns>
        private static TMP_InputField CreateRow(Transform templateRow, string rowName, string labelText, int siblingIndex)
        {
            GameObject row = Object.Instantiate(templateRow.gameObject, templateRow.parent);
            row.name = rowName;
            row.transform.SetSiblingIndex(siblingIndex);

            var field = row.GetComponentInChildren<TMP_InputField>(true);
            if (field == null)
            {
                Object.Destroy(row);
                return null;
            }

            // 複製元に付いている、オフセット用の処理を外す
            foreach (var view in row.GetComponentsInChildren<InputFieldView>(true))
            {
                Object.Destroy(view);
            }

            // 見出し = 入力欄の外にある文字
            foreach (var text in row.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.transform.IsChildOf(field.transform)) { continue; }

                text.text = labelText;
                // 複製元(OffSet)より長いので、行に収まるよう文字を自動で縮める
                text.enableWordWrapping = false;
                text.fontSizeMax = text.fontSize;
                text.fontSizeMin = Mathf.Min(text.fontSize, LABEL_FONT_SIZE_MIN);
                text.enableAutoSizing = true;
            }

            return field;
        }

        /// <summary>
        /// 拍子の行の入力欄を「分子 / 分母」の2つに分ける
        /// </summary>
        /// <param name="beatCountField">行に元からある入力欄 (分子の欄になる)</param>
        /// <returns>分母の入力欄。作れなかったときは null</returns>
        private static TMP_InputField SplitBeatField(TMP_InputField beatCountField)
        {
            var countRect = (RectTransform)beatCountField.transform;
            Transform row = countRect.parent;

            // 行の見出し (「/」の文字の見た目を、これに合わせる)
            TextMeshProUGUI label = null;
            foreach (var text in row.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.transform.IsChildOf(beatCountField.transform)) { continue; }

                label = text;
                break;
            }

            if (label == null) { return null; }

            // 分母の欄: 元の欄を複製して、元の位置(行の右端)に置く
            GameObject unitObject = Object.Instantiate(beatCountField.gameObject, row);
            unitObject.name = "BeatUnitInputField";
            foreach (var view in unitObject.GetComponentsInChildren<InputFieldView>(true))
            {
                Object.Destroy(view);
            }

            var unitRect = (RectTransform)unitObject.transform;
            unitRect.sizeDelta = new Vector2(BEAT_FIELD_WIDTH, unitRect.sizeDelta.y);

            // 分子の欄: その左に、「/」の幅を空けて置く
            countRect.sizeDelta = new Vector2(BEAT_FIELD_WIDTH, countRect.sizeDelta.y);
            countRect.anchoredPosition = unitRect.anchoredPosition - new Vector2(BEAT_FIELD_WIDTH + SLASH_WIDTH, 0f);

            // 「/」: 2つの欄の間に置く
            GameObject slashObject = Object.Instantiate(label.gameObject, row);
            slashObject.name = "Slash";
            var slashRect = (RectTransform)slashObject.transform;
            slashRect.anchorMin = unitRect.anchorMin;
            slashRect.anchorMax = unitRect.anchorMax;
            slashRect.pivot = unitRect.pivot;
            slashRect.anchoredPosition = unitRect.anchoredPosition - new Vector2(BEAT_FIELD_WIDTH, 0f);
            slashRect.sizeDelta = new Vector2(SLASH_WIDTH, unitRect.sizeDelta.y);

            var slashText = slashObject.GetComponent<TextMeshProUGUI>();
            slashText.text = "/";
            slashText.enableAutoSizing = false;
            slashText.fontSize = label.fontSizeMax;
            slashText.alignment = TextAlignmentOptions.Center;

            return unitObject.GetComponent<TMP_InputField>();
        }

        /// <summary>
        /// 複製した入力欄を、数値の入力欄として使えるようにする
        /// </summary>
        /// <param name="isNarrow">幅の狭い欄か (文字を小さくする)</param>
        private static void SetUpField(TMP_InputField field, TMP_InputField.ContentType contentType, string text, bool isNarrow)
        {
            // 複製元から引き継いだ「入力されたときの処理」(オフセットの変更) を外す
            field.onEndEdit = new TMP_InputField.SubmitEvent();
            field.onSubmit = new TMP_InputField.SubmitEvent();
            field.onValueChanged = new TMP_InputField.OnChangeEvent();

            field.interactable = true;
            field.contentType = contentType;
            if (isNarrow) { field.pointSize = NARROW_FIELD_FONT_SIZE; }

            if (field.placeholder is TMP_Text placeholder) { placeholder.text = string.Empty; }

            field.text = text;
        }

        #endregion
    }
}
