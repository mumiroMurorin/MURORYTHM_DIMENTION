using System;
using TMPro;
using UniRx;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 設定パネルに「見出し + 押すたびに値が切り替わるボタン」の行を追加する
    ///
    /// 「レーン分割数」の行を実行時に複製して作る。見た目と並びは複製元と同じになる。
    /// (シーンを直接編集せずに設定項目を増やすための仕組み)
    /// </summary>
    public static class SettingsCycleRow
    {
        /// <param name="laneDivNumButton">複製元にする「レーン分割数」のボタン</param>
        /// <param name="rowName">行のオブジェクト名</param>
        /// <param name="labelText">見出しの文字</param>
        /// <param name="valueText">ボタンに表示する現在の値</param>
        /// <param name="onPush">ボタンが押されたときの処理 (次の値へ切り替える)</param>
        /// <param name="siblingOffset">複製元の何行下に置くか</param>
        public static void Create(
            ChangeLaneDivNumButtonView laneDivNumButton,
            string rowName,
            string labelText,
            IObservable<string> valueText,
            Action onPush,
            int siblingOffset)
        {
            if (laneDivNumButton == null || valueText == null || onPush == null) { return; }

            // ボタンの親が、見出しを含む「行」
            Transform templateRow = laneDivNumButton.transform.parent;
            if (templateRow == null || templateRow.parent == null) { return; }

            // 複製して、複製元の下に並べる
            GameObject row = UnityEngine.Object.Instantiate(templateRow.gameObject, templateRow.parent);
            row.name = rowName;
            row.transform.SetSiblingIndex(templateRow.GetSiblingIndex() + siblingOffset);

            var button = row.GetComponentInChildren<ChangeLaneDivNumButtonView>(true);
            if (button == null)
            {
                UnityEngine.Object.Destroy(row);
                return;
            }

            // 見出し = ボタンの外にある文字、値 = ボタンの中にある文字
            TextMeshProUGUI valueLabel = null;
            foreach (var text in row.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.transform.IsChildOf(button.transform)) { valueLabel = text; }
                else { text.text = labelText; }
            }

            // 値は複製元(1〜2桁の数字)より長くなるので、ボタンに収まるよう文字を自動で縮める
            if (valueLabel != null)
            {
                valueLabel.enableWordWrapping = false;
                valueLabel.fontSizeMax = valueLabel.fontSize;
                valueLabel.fontSizeMin = Mathf.Min(valueLabel.fontSize, 8f);
                valueLabel.enableAutoSizing = true;
            }

            button.OnPushButtonListner += onPush;

            valueText
                .Subscribe(value =>
                {
                    if (valueLabel != null) { valueLabel.text = value; }
                })
                .AddTo(row);
        }

        /// <summary>
        /// 譜面エディタで追加している設定行をまとめて作る (編集画面・プレビュー画面で共通)
        /// </summary>
        public static void CreateEditorRows(
            ChangeLaneDivNumButtonView laneDivNumButton,
            IChartEditorOptionGetter optionGetter,
            IChartEditorOptionSetter optionSetter)
        {
            if (optionGetter == null || optionSetter == null) { return; }

            // 再生速度
            Create(
                laneDivNumButton,
                "PlaybackSpeed",
                "再生速度",
                optionGetter.PlaybackSpeed.Select(speed => $"×{speed:0.##}"),
                optionSetter.SwitchPlaybackSpeed,
                1);

            // 再生中にノーツが鳴らす音を、ノーツの種類ごとに変えるか、1種類にそろえるか
            Create(
                laneDivNumButton,
                "NoteSoundMode",
                "ノーツ音",
                optionGetter.IsSingleNoteSound.Select(isSingle => isSingle ? "単一" : "種類別"),
                optionSetter.SwitchNoteSoundMode,
                2);
        }
    }
}
