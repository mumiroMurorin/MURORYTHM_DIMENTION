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
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.ChangeType) { return; }

            // 左クリック
            if (Input.GetMouseButtonDown(0)) { ChangeNoteTypeOnClick(); }
        }

        /// <summary>
        /// ノーツタイプの変更
        /// </summary>
        private void ChangeNoteTypeOnClick()
        {

            var collider = chartEditorDataGetter.GetInteractableCollider<IChangableCollider>();
            if (collider == null) { return; }

            var changableObject = collider.Note;
            if (changableObject.NoteData == null) { return; }

            Record(() => { 
                ChangeNoteType(changableObject); 
            }, 
            () => {
                ChangeNoteType(changableObject);
            });
        }

        private void ChangeNoteType(IChangableObject changableObject)
        {
            changableObject.NoteData.ChangeNoteType();
            //changableObject.OnChangeNoteType();
        }
    }

}