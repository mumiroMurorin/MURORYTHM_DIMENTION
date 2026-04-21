using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System.Linq;
using static UndoRedo.History;

namespace ChartEditor
{
    public class NoteDestroyer : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        INotesDataGetter notesGetter;
        INotesDataSetter notesSetter;
        IChartEditorDataGetter dataGetter;

        EditMode[] ignoreEditModes = new EditMode[] {
             EditMode.Connecting,
             EditMode.EditingBarConfig,
             EditMode.EditingSubDivisionConfig,
             EditMode.Moving,
             EditMode.Scaling,
            EditMode.Preview,
        };

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
            this.dataGetter = dataGetter;
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
            var datasCopy = notesGetter.SelectingNotes.ToList();
            notesSetter.ClearSelectingNotes();

            // 削除
            Record(() => { 
                foreach(var data in datasCopy) { DestroyNote(data); }
            }, 
            // 配置
            () => {
                foreach (var data in datasCopy) { DeployNote(data); }
            });
        }

        /// <summary>
        /// データの削除
        /// </summary>
        /// <param name="noteData"></param>
        private void DestroyNote(IDeployableNoteData noteData)
        {
            dataGetter.ChartData.Value.RemoveNote(noteData);
        }

        /// <summary>
        /// データの追加
        /// </summary>
        /// <param name="noteData"></param>
        private void DeployNote(IDeployableNoteData noteData)
        {
            dataGetter.ChartData.Value.AddNote(noteData);
        }

    }

}

