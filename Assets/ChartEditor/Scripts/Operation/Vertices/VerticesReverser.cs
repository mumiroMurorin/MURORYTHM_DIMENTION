using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using static UndoRedo.Vertices.VerticesMoveRecord;

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

            // à»ëOÇÃç¿ïWÇãLò^
            var previousPos = ConvertVertexDatas(chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices.Vertices);

            // îΩì]
            chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices.ReverseVertices(new Vector2(0, 1), new Vector2(0, -1));

            // åªç›ÇÃç¿ïWÇ‡ãLò^ÇµÇƒRedoUndoópÇ…ìoò^
            var currentPos = ConvertVertexDatas(chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices.Vertices);
            RecordVertcesMoving(previousPos, currentPos);
        }

        public void ReverseXAxis()
        {
            if (chartEditorDataGetter == null) { return; }
            if (chartEditorDataGetter.EditingVertices.Value == null) { return; }

            // à»ëOÇÃç¿ïWÇãLò^
            var previousPos = ConvertVertexDatas(chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices.Vertices);

            // îΩì]
            chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices.ReverseVertices(new Vector2(1, 0), new Vector2(-1, 0));

            // åªç›ÇÃç¿ïWÇ‡ãLò^ÇµÇƒRedoUndoópÇ…ìoò^
            var currentPos = ConvertVertexDatas(chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices.Vertices);
            RecordVertcesMoving(previousPos, currentPos);
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
