using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChartEditor
{
    public static class EditModeExtensions
    {
        public static bool IsInEditModeList(this EditMode editMode, EditMode[] targetEditModes)
        {
            foreach (var mode in targetEditModes)
            {
                if (mode == editMode) { return true; }
            }
            return false;
        }
    }

    /// <summary>
    /// 修飾キーの判定 (左右どちらのキーでも反応する)
    /// </summary>
    public static class ModifierKey
    {
        public static bool Ctrl => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        public static bool Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        public static bool Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
    }

    /// <summary>
    /// 入力欄(InputField)に文字を打っている最中かの判定
    /// </summary>
    public static class InputFieldFocus
    {
        /// <summary>
        /// 入力欄が「選択中」のまま残っていたら、選択を外す (入力を終えたときに呼ぶ)
        ///
        /// Enter で入力を終えても、入力欄は選択中のまま残る。Space キーは「選択中の部品を決定する」キーでもあるので、
        /// そのまま Space で再生を始めると、入力欄がもう一度入力状態になり、以後の Space (再生の停止) や
        /// ショートカットキーが「文字を打っている最中」として無視されてしまう。
        /// </summary>
        public static void Release(TMPro.TMP_InputField field)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || field == null) { return; }
            // ほかの部品をクリックして入力を終えたときは、選択の付け替えの最中なので触らない (すでに選択は外れる)
            if (eventSystem.alreadySelecting) { return; }
            if (eventSystem.currentSelectedGameObject != field.gameObject) { return; }

            eventSystem.SetSelectedGameObject(null);
        }

        /// <summary>
        /// 入力欄にフォーカスがあるか (打っている間は、キー操作を譜面の操作として扱わない)
        /// </summary>
        public static bool IsTyping
        {
            get
            {
                var eventSystem = EventSystem.current;
                if (eventSystem == null) { return false; }

                var selected = eventSystem.currentSelectedGameObject;
                if (selected == null) { return false; }

                if (selected.TryGetComponent(out TMPro.TMP_InputField tmpField)) { return tmpField.isFocused; }
                if (selected.TryGetComponent(out UnityEngine.UI.InputField field)) { return field.isFocused; }

                return false;
            }
        }
    }

    /// <summary>
    /// 選択中のノーツに対するキー操作 (削除・コピー・貼り付け) を受け付けてよい状態かの判定
    /// </summary>
    public static class SelectedNotesKeyOperation
    {
        /// <param name="ignoreEditModes">編集画面で、その操作を受け付けないエディットモード</param>
        /// <param name="isFromMenu">メニューの項目から行うとき true (キー操作のときは false)</param>
        public static bool IsAcceptable(IChartEditorDataGetter dataGetter, EditMode[] ignoreEditModes, bool isFromMenu = false)
        {
            // 譜面の読み込み中と、入力欄に文字を打っている間は受け付けない
            if (dataGetter.IsLoadingChart.Value) { return false; }
            if (InputFieldFocus.IsTyping) { return false; }

            var layer = dataGetter.EditNoteType.Value;
            var editMode = dataGetter.CurrentEditMode.Value;

            // 全画面プレビュー: プレビューの上で選んだノーツを、その場で操作できる
            // (ここではドラッグ中もエディットモードが Preview のままなので、マウスのボタンを押している間は受け付けない)
            if (layer == EditNoteType.Preview)
            {
                // メニューの項目は、押している間マウスのボタンが押されたままになる。メニューを開いている間は、
                // プレビューの上のドラッグは始まらないので、ボタンは見ない
                // (見ると、項目を押している間だけ「押せない」表示になり、離したときのクリックが捨てられることがある)
                return editMode == EditMode.Preview && (isFromMenu || !Input.GetMouseButton(0));
            }

            // 編集画面: グラウンド・スペースのレイヤーで、ほかの操作の途中でないとき
            if (layer != EditNoteType.Ground && layer != EditNoteType.Space) { return false; }

            return !editMode.IsInEditModeList(ignoreEditModes);
        }
    }
}