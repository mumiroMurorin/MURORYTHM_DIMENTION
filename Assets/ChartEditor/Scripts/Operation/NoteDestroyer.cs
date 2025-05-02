using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class NoteDestroyer : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        IChartEditorDataGetter chartEditorDataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0)) { DestroyNote(); }
        }

        private void DestroyNote()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.Destroy) { return; }

            IDestroyableObject destroyableObject = chartEditorDataGetter.DestroyableObject.Value;
            if (destroyableObject == null) { return; }

            chartEditorDataGetter.ChartData.Value.RemoveNote(destroyableObject.Note.NoteData);

            destroyableObject.OnDestroy();
            destroyableObject.Note.NoteData = null;
        }
    }

}
