using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class NoteDestroyer : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        [SerializeField] MultiNoteSelector noteSelector;
        [SerializeField] NoteObjectsController noteObjectsController;

        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;

        EditMode[] ignoreEditModes = new EditMode[] {
             
        };

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter, IChartEditorDataSetter chartEditorDataSetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
            this.chartEditorDataSetter = chartEditorDataSetter;
        }

        private void Update()
        {
            if(chartEditorDataGetter.EditNoteType.Value != EditNoteType.Ground &&
                chartEditorDataGetter.EditNoteType.Value != EditNoteType.Space) { return; }

            // Deleteキーで消す
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                // 除外エディットモード中は返す
                if (chartEditorDataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes)) { return; }

                DestroyNotes();
            }
        }

        private void DestroyNotes()
        {
            foreach(var data in noteSelector.SelectingNotes) { DestroyNote(data); }
        }

        public void DestroyNote(IDeployableNoteData noteData)
        {
            // オブジェクトの削除
            var noteObject = noteObjectsController.DataToObj.GetObject(noteData);
            if (noteObject == null || !noteObject.TryGetComponent(out IDestroyableObject destroyableObject)) { return; }

            destroyableObject.OnDestroy();

            // データの削除
            noteObjectsController.DataToObj.Remove(noteData);
            chartEditorDataGetter.ChartData.Value.RemoveNote(noteData);
        }

    }

}
