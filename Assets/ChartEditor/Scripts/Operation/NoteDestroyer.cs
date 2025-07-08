using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class NoteDestroyer : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        INotesDataGetter notesGetter;
        INotesDataSetter notesSetter;
        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;

        EditMode[] ignoreEditModes = new EditMode[] {
             EditMode.Connecting,
             EditMode.EditingBarConfig,
             EditMode.EditingSubDivisionConfig,
             EditMode.Moving,
             EditMode.Scaling,
        };

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        private void Update()
        {
            if(dataGetter.EditNoteType.Value != EditNoteType.Ground &&
                dataGetter.EditNoteType.Value != EditNoteType.Space) { return; }

            // Deleteキーで消す
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                // 除外エディットモード中は返す
                if (dataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes)) { return; }

                DestroyNotes();
            }
        }

        private void DestroyNotes()
        {
            foreach(var data in notesGetter.SelectingNotes) { DestroyNote(data); }
        }

        public void DestroyNote(IDeployableNoteData noteData)
        {
            // オブジェクトの削除
            var noteObject = notesGetter.GetNoteObject(noteData);
            if (noteObject == null || !noteObject.TryGetComponent(out IDestroyableObject destroyableObject)) { return; }

            destroyableObject.OnDestroy();

            // データの削除
            notesSetter.RemoveDataToNoteObject(noteData);
        }

    }

}
