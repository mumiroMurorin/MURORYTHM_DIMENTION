using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class SpaceEditor : MonoBehaviour
    {
        INotesDataSetter notesSetter;
        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter, IChartEditorDataSetter chartEditorDataSetter, INotesDataSetter notesSetter)
        {
            this.notesSetter = notesSetter;
            this.chartEditorDataGetter = chartEditorDataGetter;
            this.chartEditorDataSetter = chartEditorDataSetter;
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) { StartEditNote(); }
            else if (Input.GetMouseButtonDown(1)) { BackAutoMode(); }
        }

        private void StartEditNote()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.SpaceEdit) { return; }

            var collider = chartEditorDataGetter.GetInteractableCollider<ISpaceEditableCollider>();
            if(collider == null) { return; }

            ISpaceEditableObject editableObject = collider.Note;
            if (editableObject == null) { return; }

            Debug.Log("編集開始");
            chartEditorDataSetter.SetEditNoteType(EditNoteType.Vertices);
            chartEditorDataSetter.SetEditMode(EditMode.None);
            notesSetter.SetEditingVertices(editableObject.NoteData);
        }

        /// <summary>
        /// オートモードに戻す
        /// </summary>
        private void BackAutoMode()
        {
            chartEditorDataSetter.SetEditMode(EditMode.None);
        }
    }
}
