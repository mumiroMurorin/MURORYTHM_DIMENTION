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
            EditMode editMode = chartEditorDataGetter.CurrentEditMode.Value;

            // 左クリック
            if (Input.GetMouseButtonDown(0) && editMode == EditMode.ChangeType) { ChangeNoteType(); }
            // 右クリック
            else if (Input.GetMouseButtonDown(1)) { ChangeNoteType(); }
        }

        /// <summary>
        /// ノーツタイプの変更
        /// </summary>
        private void ChangeNoteType()
        {
            IChangableObject changableObject = chartEditorDataGetter.ChangableObject.Value;
            if (changableObject == null) { return; }
            if (changableObject.NoteData == null) { return; }

            changableObject.NoteData.ChangeNoteType(false);
            changableObject.OnChangeNoteType();
        }
    }

}