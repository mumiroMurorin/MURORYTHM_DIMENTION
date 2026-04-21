using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class SpaceEditor : MonoBehaviour
    {
        [SerializeField] float doubleClickInterval = 0.3f;

        INotesDataSetter notesSetter;
        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;

        EditMode[] ignoreEditModes = new EditMode[] {
            EditMode.SpaceMoving,
            EditMode.Connecting,
            EditMode.ChangeType,
            EditMode.Preview,
        };

        float count = 0f;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, INotesDataSetter notesSetter)
        {
            this.notesSetter = notesSetter;
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        void Update()
        {
            if (dataGetter.EditNoteType.Value != EditNoteType.Space) { return; }
            if (dataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes)) { return; }

            // カウントダウン
            if(count > 0) { count -= Time.deltaTime; }
            if(count < 0) { count = 0f; }

            // カーソル下に編集可能オブジェクトがあるときのみ通す
            var collider = dataGetter.GetInteractableCollider<ISpaceEditableCollider>();
            if (collider == null) { return; }

            // ダブルクリックカウントダウン開始
            if (count <= 0f && Input.GetMouseButtonDown(0)) { count = doubleClickInterval; }
            // ダブルクリック判定
            else if (count > 0f && Input.GetMouseButtonDown(0)) { StartEditNote(); }
        }

        private void StartEditNote()
        {
            var collider = dataGetter.GetInteractableCollider<ISpaceEditableCollider>();
            if(collider == null) { return; }

            ISpaceEditableObject editableObject = collider.Note;
            if (editableObject == null) { return; }

            dataSetter.SetEditNoteType(EditNoteType.Vertices);
            notesSetter.SetEditingVertices(editableObject.NoteData);
        }
    }
}

