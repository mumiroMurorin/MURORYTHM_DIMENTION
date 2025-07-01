using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static UndoRedo.History;

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

            var vertices = chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices;

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