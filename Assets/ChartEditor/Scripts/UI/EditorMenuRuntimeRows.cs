using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// メニューバーの「表示」の一覧に、実行時に行を足す
    ///
    /// シーンに置いてある行を複製して、操作と値を付け替える (見た目は、複製元とまったく同じになる)。
    /// ・「オニオンスキン」の行: 「レーン分割数」の行を複製して、「ウィンドウサイズ」の行のすぐ下に入れる
    /// ・「SpaceHoldを3Dで編集」の行: 「プレビューを作り直す」の行を複製して、いちばん下に足す
    /// 一覧の行は、決まった位置に置いてある (並べる部品は使っていない) ので、
    /// 入れた行より下の行を、行の高さぶん下へずらして、一覧の枠を伸ばす。
    /// 複製元の行が見つからないとき (シーンの作りが変わったとき) は、その行を足さない。
    /// </summary>
    public static class EditorMenuRuntimeRows
    {
        /// <summary>
        /// オニオンスキンの行で選べる枚数 (前後それぞれ)
        /// </summary>
        public static readonly int[] OnionSkinChoices = { 0, 1, 2, 3, 5 };

        const string ROW_LABEL_NAME = "Label";

        /// <summary>
        /// 「表示」の一覧に行を足して、足した項目も含めた一覧を返す
        /// </summary>
        /// <param name="items">メニューバーの一覧にある項目 (シーンに置いてあるもの)</param>
        public static EditorMenuItem[] AddViewRows(EditorMenuItem[] items)
        {
            if (items == null) { return items; }

            var added = new List<EditorMenuItem>();

            try
            {
                AddOnionSkinRow(items, added);
                Add3DViewRow(items, added);
            }
            catch (System.Exception exception)
            {
                // 行を足せなくても、メニューそのものは使えるようにしておく
                Debug.LogException(exception);
            }

            if (added.Count == 0) { return items; }

            var result = new List<EditorMenuItem>(items);
            result.AddRange(added);
            return result.ToArray();
        }

        /// <summary>
        /// 「オニオンスキン」の行 (枚数を選ぶボタンが並ぶ行)
        /// </summary>
        private static void AddOnionSkinRow(EditorMenuItem[] items, List<EditorMenuItem> added)
        {
            EditorMenuItem laneChoice = Find(items, EditorMenuCommand.SelectLaneDivision);
            if (laneChoice == null) { return; }

            var templateRow = laneChoice.transform.parent as RectTransform;
            var list = templateRow != null ? templateRow.parent as RectTransform : null;
            if (list == null) { return; }

            // 「ウィンドウサイズ」の行のすぐ下に入れる (無ければ、レーン分割数の行の下)
            EditorMenuItem windowChoice = Find(items, EditorMenuCommand.SelectWindowSize);
            var previousRow = windowChoice != null && windowChoice.transform.parent is RectTransform windowRow && windowRow.parent == list
                ? windowRow
                : templateRow;

            RectTransform row = InsertRowBelow(list, templateRow, previousRow, "OnionSkin");

            SetRowLabel(row, "オニオンスキン");

            // 枚数のボタン (複製元のボタンの数が足りないときは、ある分だけ。多いときは、余りを消す)
            var choices = new List<EditorMenuItem>(row.GetComponentsInChildren<EditorMenuItem>(true));
            choices.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            for (int i = 0; i < choices.Count; i++)
            {
                if (i >= OnionSkinChoices.Length)
                {
                    Object.Destroy(choices[i].gameObject);
                    continue;
                }

                int count = OnionSkinChoices[i];
                choices[i].Configure(EditorMenuCommand.SelectOnionSkinCount, count);
                choices[i].SetLabel(count.ToString());
                added.Add(choices[i]);
            }
        }

        /// <summary>
        /// 「SpaceHoldを3Dで編集」の行
        /// </summary>
        private static void Add3DViewRow(EditorMenuItem[] items, List<EditorMenuItem> added)
        {
            EditorMenuItem rebuild = Find(items, EditorMenuCommand.RebuildPreview);
            if (rebuild == null) { return; }

            var templateRow = rebuild.transform as RectTransform;
            var list = templateRow != null ? templateRow.parent as RectTransform : null;
            if (list == null) { return; }

            RectTransform row = InsertRowBelow(list, templateRow, templateRow, "SpaceHold3DView");

            var item = row.GetComponent<EditorMenuItem>();
            if (item == null)
            {
                Object.Destroy(row.gameObject);
                return;
            }

            item.Configure(EditorMenuCommand.ToggleSpaceHold3DView, 0);
            item.SetLabel("SpaceHoldを3Dで編集");
            item.SetShortcutLabel("Alt+ダブルクリック");
            added.Add(item);
        }

        /// <summary>
        /// 行を複製して、previousRow のすぐ下に入れる (下の行をずらし、一覧の枠を伸ばす)
        /// </summary>
        private static RectTransform InsertRowBelow(RectTransform list, RectTransform templateRow, RectTransform previousRow, string name)
        {
            float rowHeight = templateRow.sizeDelta.y;
            float previousY = previousRow.anchoredPosition.y;

            // previousRow より下にある行 (区切り線も) を、1行ぶん下へずらす
            foreach (Transform child in list)
            {
                if (child is not RectTransform rect) { continue; }
                if (rect.anchoredPosition.y < previousY - 0.5f)
                {
                    rect.anchoredPosition -= new Vector2(0f, rowHeight);
                }
            }

            var row = (RectTransform)Object.Instantiate(templateRow.gameObject, list).transform;
            row.name = name;
            row.anchoredPosition = new Vector2(templateRow.anchoredPosition.x, previousY - previousRow.sizeDelta.y);
            row.SetSiblingIndex(previousRow.GetSiblingIndex() + 1);

            list.sizeDelta += new Vector2(0f, rowHeight);
            return row;
        }

        /// <summary>
        /// 行の左に書いてある名前を変える
        /// </summary>
        private static void SetRowLabel(RectTransform row, string text)
        {
            Transform labelTransform = row.Find(ROW_LABEL_NAME);
            if (labelTransform != null && labelTransform.TryGetComponent(out TextMeshProUGUI label))
            {
                label.text = text;
            }
        }

        private static EditorMenuItem Find(EditorMenuItem[] items, EditorMenuCommand command)
        {
            foreach (var item in items)
            {
                if (item != null && item.Command == command) { return item; }
            }

            return null;
        }
    }
}
