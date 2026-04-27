using VContainer;
using UnityEngine;

namespace ChartEditor
{
    public class VerticesChainNavigator
    {
        IChartEditorDataGetter dataGetter;
        INotesDataGetter notesGetter;
        INotesDataSetter notesSetter;

        [Inject]
        public VerticesChainNavigator(
            IChartEditorDataGetter dataGetter,
            INotesDataGetter notesGetter,
            INotesDataSetter notesSetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
        }

        public void MoveToNext()
        {
            MoveToConnectedVertices(true);
        }

        public void MoveToPrevious()
        {
            MoveToConnectedVertices(false);
        }

        void MoveToConnectedVertices(bool moveToNext)
        {
            if (dataGetter.EditNoteType.Value != EditNoteType.Vertices) { return; }

            if (notesGetter.EditingVertices.Value is not IChainNoteData currentChainData) { return; }
            if (currentChainData.NoteObject == null) { return; }

            var targetObject = moveToNext
                ? currentChainData.NoteObject.NextNote.Value
                : currentChainData.NoteObject.BackNote.Value;

            if (targetObject?.Note?.NoteData is not IVerticesControlableNoteData targetVerticesData) { return; }

            notesSetter.SetEditingVertices(targetVerticesData);
        }
    }
}
