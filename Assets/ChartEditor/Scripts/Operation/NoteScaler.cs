using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class NoteScaler : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        IChartEditorDataGetter chartEditorDataGetter;
        IScalableObject scaledNote;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0)) { ScaleNote(); }
        }

        private void ScaleNote()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.scale) { return; }

            IScalableObject scalableObject = chartEditorDataGetter.ScalableObject.Value;
            if (scalableObject == null) { return; }

            scalableObject.OnScale();
        }
    }

}