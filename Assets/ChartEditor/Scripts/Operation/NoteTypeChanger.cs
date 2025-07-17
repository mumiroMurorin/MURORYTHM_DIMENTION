using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static UndoRedo.History;

namespace ChartEditor
{
    public class NoteTypeChanger : MonoBehaviour
    {
        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter, IChartEditorDataSetter chartEditorDataSetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
            this.chartEditorDataSetter = chartEditorDataSetter;
        }

        private void Update()
        {
            // 左クリック
            if (Input.GetMouseButtonDown(0)) { ChangeNoteTypeOnClick(); }
            // 右クリック時、タイプ変更モード解除
            else if (Input.GetMouseButtonDown(1)) { OnEndChangeMode(); }
        }

        /// <summary>
        /// ノーツタイプの変更
        /// </summary>
        private void ChangeNoteTypeOnClick()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.ChangeType) { return; }

            var collider = chartEditorDataGetter.GetInteractableCollider<IChangableCollider>();
            if (collider == null) { return; }

            var changableObject = collider.Note;
            if (changableObject.NoteData == null) { return; }

            Record(() => { 
                ChangeNoteType(changableObject, true); 
            }, 
            () => {
                ChangeNoteType(changableObject, false);
            });
        }

        private void ChangeNoteType(IChangableObject changableObject, bool isDone)
        {
            changableObject.NoteData.ChangeNoteType(isDone);
            changableObject.OnChangeNoteType();
        }

        private void OnEndChangeMode()
        {
            chartEditorDataSetter.SetEditMode(EditMode.None);
        }
    }

}