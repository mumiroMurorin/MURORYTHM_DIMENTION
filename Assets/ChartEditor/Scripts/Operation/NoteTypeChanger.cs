using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class NoteTypeChanger : MonoBehaviour
    {
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
            // 左クリック
            if (Input.GetMouseButtonDown(0)) { ChangeNoteType(); }
            // 右クリック時、タイプ変更モード解除
            else if (Input.GetMouseButtonDown(1)) { OnEndChangeMode(); }
        }

        /// <summary>
        /// ノーツタイプの変更
        /// </summary>
        private void ChangeNoteType()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.ChangeType) { return; }

            var collider = chartEditorDataGetter.GetInteractableCollider<IChangableCollider>();
            if (collider == null) { return; }

            var changableObject = collider.Note;
            if (changableObject.NoteData == null) { return; }

            changableObject.NoteData.ChangeNoteType();
            changableObject.OnChangeNoteType();
        }

        private void OnEndChangeMode()
        {
            chartEditorDataSetter.SetEditMode(EditMode.None);
        }
    }

}