using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class SpaceEditor : MonoBehaviour
    {
        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter, IChartEditorDataSetter chartEditorDataSetter)
        {
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
            chartEditorDataSetter.SetEditingVertices(editableObject.NoteData);
            chartEditorDataSetter.SetEditMode(EditMode.None);
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
