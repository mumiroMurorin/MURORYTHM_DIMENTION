using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class VerticesSlider : MonoBehaviour
    {
        IChartEditorDataGetter chartEditorDataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        public void SlideIndices(int delta)
        {
            if(chartEditorDataGetter == null) { return; }
            if(chartEditorDataGetter.EditingVertices.Value == null) { return; }

            chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices.SlideVertexIndices(delta);
        }
    }

}