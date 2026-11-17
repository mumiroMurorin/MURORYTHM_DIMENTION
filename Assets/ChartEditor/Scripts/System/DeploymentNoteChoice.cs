using System;

namespace ChartEditor
{
    /// <summary>
    /// 置くノーツを、何も選んでいない状態か (録音 20261118_010533)
    ///
    /// 編集の道具で、ノーツもレーンも無い所をダブルクリックすると、置くノーツの選択が外れる (InputHandler)。
    /// その間は、レーンを押してもノーツを置かず (NoteDeployerBase)、ダイナミックノーツを選んでいたときのように
    /// ノーツの上が「載せる」場所にもならないので、ノーツを動かせる。一覧のボタンを押す (Alt+数字も) と、また選んだ状態になる。
    /// 選んでいた種類 (DeploymentNoteType) は覚えたまま (一覧の見た目だけ、どれも選んでいない形にする)。
    /// </summary>
    public static class DeploymentNoteChoice
    {
        static bool isNone;

        /// <summary>置くノーツを、何も選んでいないか</summary>
        public static bool IsNone => isNone;

        /// <summary>選んでいない状態が変わったとき (一覧のボタンの見た目を合わせる)</summary>
        public static event Action OnChanged;

        public static void SetNone(bool value)
        {
            if (isNone == value) { return; }
            isNone = value;
            OnChanged?.Invoke();
        }
    }
}
