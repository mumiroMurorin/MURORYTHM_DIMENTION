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
            // 右クリック時、オートモードならタイプ変更モード解除
            else if (Input.GetMouseButtonDown(1)) { BackAutoMode(); }
        }

        /// <summary>
        /// ノーツタイプの変更
        /// </summary>
        private void ChangeNoteType()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.ChangeType) { return; }

            IChangableObject changableObject = chartEditorDataGetter.ChangableObject.Value;
            if (changableObject == null) { return; }
            if (changableObject.NoteData == null) { return; }

            changableObject.NoteData.ChangeNoteType();
            changableObject.OnChangeNoteType();
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