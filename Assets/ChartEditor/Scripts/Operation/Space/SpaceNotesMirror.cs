using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static UndoRedo.Vertices.VerticesMoveRecord;

namespace ChartEditor
{
    public class SpaceNotesMirror : MonoBehaviour
    {
        IChartEditorDataGetter dataGetter;
        INotesDataGetter notesGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
        }

        public void MirrorSelectingSpaceNotes()
        {
            if (dataGetter == null || notesGetter == null) { return; }
            if (dataGetter.EditNoteType.Value != EditNoteType.Space) { return; }
            if (notesGetter.SelectingNotes == null || notesGetter.SelectingNotes.Count == 0) { return; }

            var previousPositions = new List<VertexDataToPos>();
            var currentPositions = new List<VertexDataToPos>();

            foreach (var note in notesGetter.SelectingNotes)
            {
                if (note is not IVerticesControlableNoteData verticesNote) { continue; }

                AddVertexPositions(verticesNote, previousPositions);
                MirrorVerticesLeftRight(verticesNote);
                AddVertexPositions(verticesNote, currentPositions);
            }

            if (previousPositions.Count == 0) { return; }

            RecordVertcesMoving(previousPositions, currentPositions);
        }

        void MirrorVerticesLeftRight(IVerticesControlableNoteData verticesNote)
        {
            verticesNote.SpaceVertices.ReverseVertices(new Vector2(0f, 1f), new Vector2(0f, -1f));
        }

        void AddVertexPositions(IVerticesControlableNoteData verticesNote, List<VertexDataToPos> positions)
        {
            foreach (var vertex in verticesNote.SpaceVertices.Vertices)
            {
                positions.Add(new VertexDataToPos(vertex, vertex.Position.Value));
            }
        }
    }
}
