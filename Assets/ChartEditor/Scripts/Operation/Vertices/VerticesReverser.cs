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
        IChartEditorDataGetter chartEditorDataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        public void ReverseYAxis()
        {
            if (chartEditorDataGetter == null) { return; }
            if (chartEditorDataGetter.EditingVertices.Value == null) { return; }

            var vertices = chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices;

            Record(() =>
            // ”½“]
            {
                vertices.ReverseVertices(new Vector2(0, 1), new Vector2(0, -1));
            }, () =>
            // “¯‚¶‚­”½“]
            {
                vertices.ReverseVertices(new Vector2(0, 1), new Vector2(0, -1));
            });
        }

        public void ReverseXAxis()
        {
            if (chartEditorDataGetter == null) { return; }
            if (chartEditorDataGetter.EditingVertices.Value == null) { return; }

            var vertices = chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices;

            Record(() =>
            // ”½“]
            {
                vertices.ReverseVertices(new Vector2(1, 0), new Vector2(-1, 0));
            }, () =>
            // “¯‚¶‚­”½“]
            {
                vertices.ReverseVertices(new Vector2(1, 0), new Vector2(-1, 0));
            });
            
        }

        private List<VertexDataToPos> ConvertVertexDatas(IReadOnlyReactiveCollection<VertexData> vertexDatas)
        {
            var list = new List<VertexDataToPos>();
            foreach (var data in chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices.Vertices)
            {
                list.Add(new VertexDataToPos(data, data.Position.Value));
            }

            return list;
        }
    }

}
