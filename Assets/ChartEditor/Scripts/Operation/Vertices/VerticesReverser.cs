using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using static UndoRedo.History;

namespace ChartEditor
{
    public class VerticesReverser : MonoBehaviour
    {
        IChartEditorDataGetter dataGetter;
        INotesDataGetter notesGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
        }

        public void ReverseYAxis()
        {
            if (dataGetter == null) { return; }
            if (notesGetter.EditingVertices.Value == null) { return; }

            var vertices = notesGetter.EditingVertices.Value.SpaceVertices;

            Record(() =>
            // 反転
            {
                vertices.ReverseVertices(new Vector2(0, 1), new Vector2(0, -1));
            }, () =>
            // 同じく反転
            {
                vertices.ReverseVertices(new Vector2(0, 1), new Vector2(0, -1));
            });
        }

        public void ReverseXAxis()
        {
            if (dataGetter == null) { return; }
            if (notesGetter.EditingVertices.Value == null) { return; }

            var vertices = notesGetter.EditingVertices.Value.SpaceVertices;

            Record(() =>
            // 反転
            {
                vertices.ReverseVertices(new Vector2(1, 0), new Vector2(-1, 0));
            }, () =>
            // 同じく反転
            {
                vertices.ReverseVertices(new Vector2(1, 0), new Vector2(-1, 0));
            });
            
        }
    }

}
