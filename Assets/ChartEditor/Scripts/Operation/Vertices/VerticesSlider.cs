using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static UndoRedo.History;

namespace ChartEditor
{
    public class VerticesSlider : MonoBehaviour
    {
        IChartEditorDataGetter dataGetter;
        INotesDataGetter notesGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
        }

        public void SlideIndices(int delta)
        {
            if(dataGetter == null) { return; }
            if(notesGetter.EditingVertices.Value == null) { return; }

            var vertices = notesGetter.EditingVertices.Value.SpaceVertices;

            Record(() => 
            // ‚¸‚ç‚·
            {
                vertices.SlideVertexIndices(delta);
            }, () =>
            // ‹t‚É‚¸‚ç‚·
            {
                vertices.SlideVertexIndices(-delta);
            });
        }
    }

}