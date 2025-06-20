using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

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

            chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices.ReverseVertices(new Vector2(0, 1), new Vector2(0, -1));
        }

        public void ReverseXAxis()
        {
            if (chartEditorDataGetter == null) { return; }
            if (chartEditorDataGetter.EditingVertices.Value == null) { return; }

            chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices.ReverseVertices(new Vector2(1, 0), new Vector2(-1, 0));
        }
    }

}
