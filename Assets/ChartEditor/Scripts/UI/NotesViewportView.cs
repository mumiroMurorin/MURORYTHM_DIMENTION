using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 置けるノーツの一覧 (タッチノート、ダイナミック、ホールドなどのボタンの列)
    ///
    /// 頂点のレイヤー、ノーツを接続している途中、ホールドを1本だけ編集している間 (HoldIsolation) は、隠す。
    /// </summary>
    public class NotesViewportView : MonoBehaviour
    {
        [SerializeField] GameObject obj;

        EditMode editMode;
        EditNoteType editNoteType;
        bool isHiddenByIsolation;

        public void OnChangeEditMode(EditMode mode, EditNoteType editNoteType)
        {
            this.editMode = mode;
            this.editNoteType = editNoteType;
            Refresh();
        }

        /// <summary>
        /// ホールドを1本だけ編集している間は、ノーツを置けないので、一覧を隠す (終わったら、元の状態に戻す)
        /// </summary>
        public void SetHiddenByIsolation(bool isHidden)
        {
            isHiddenByIsolation = isHidden;
            Refresh();
        }

        private void Refresh()
        {
            bool isShown = editNoteType != EditNoteType.Vertices && editMode != EditMode.Connecting && !isHiddenByIsolation;
            if (obj.activeSelf != isShown) { obj.SetActive(isShown); }
        }
    }

}
