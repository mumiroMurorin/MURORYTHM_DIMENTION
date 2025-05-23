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
        IChartEditorDataSetter chartEditorDataSetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter, IChartEditorDataSetter chartEditorDataSetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
            this.chartEditorDataSetter = chartEditorDataSetter;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0)) { DestroyNote(); }
            // 右クリック時、オートモードならタイプ変更モード解除
            else if (Input.GetMouseButtonDown(1)) { BackAutoMode(); }
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

        /// <summary>
        /// オートモードに戻す
        /// </summary>
        private void BackAutoMode()
        {
            if (!chartEditorDataGetter.AutoEditMode.Value) { return; }
            chartEditorDataSetter.SetEditMode(EditMode.None);
        }
    }

}
