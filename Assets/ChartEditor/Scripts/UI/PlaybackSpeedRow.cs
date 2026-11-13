using TMPro;
using UniRx;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 設定パネルに「再生速度」の切り替え行を追加する
    ///
    /// 「レーン分割数」の行(見出し + 押すたびに値が切り替わるボタン)を実行時に複製して作る。
    /// 見た目と並びは複製元と同じになる。
    /// </summary>
    public static class PlaybackSpeedRow
    {
        const string LABEL_TEXT = "再生速度";

        /// <param name="laneDivNumButton">複製元にする「レーン分割数」のボタン</param>
        public static void Create(
            ChangeLaneDivNumButtonView laneDivNumButton,
            IChartEditorOptionGetter optionGetter,
            IChartEditorOptionSetter optionSetter)
        {
            if (laneDivNumButton == null || optionGetter == null || optionSetter == null) { return; }

            // ボタンの親が、見出しを含む「行」
            Transform templateRow = laneDivNumButton.transform.parent;
            if (templateRow == null || templateRow.parent == null) { return; }

            // 複製して、複製元のすぐ下に並べる
            GameObject row = Object.Instantiate(templateRow.gameObject, templateRow.parent);
            row.name = "PlaybackSpeed";
            row.transform.SetSiblingIndex(templateRow.GetSiblingIndex() + 1);

            var button = row.GetComponentInChildren<ChangeLaneDivNumButtonView>(true);
            if (button == null)
            {
                Object.Destroy(row);
                return;
            }

            // 見出し = ボタンの外にある文字、値 = ボタンの中にある文字
            TextMeshProUGUI valueText = null;
            foreach (var text in row.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.transform.IsChildOf(button.transform)) { valueText = text; }
                else { text.text = LABEL_TEXT; }
            }

            // 「×0.75」は複製元の値(1〜2桁の数字)より長いので、ボタンに収まるよう文字を自動で縮める
            if (valueText != null)
            {
                valueText.enableWordWrapping = false;
                valueText.fontSizeMax = valueText.fontSize;
                valueText.fontSizeMin = Mathf.Min(valueText.fontSize, 8f);
                valueText.enableAutoSizing = true;
            }

            // 押すたびに次の速度へ
            button.OnPushButtonListner += optionSetter.SwitchPlaybackSpeed;

            // 現在の速度を表示
            optionGetter.PlaybackSpeed
                .Subscribe(speed =>
                {
                    if (valueText != null) { valueText.text = $"×{speed:0.##}"; }
                })
                .AddTo(row);
        }
    }
}
