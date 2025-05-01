using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class NoteConnecter : MonoBehaviour
    {
        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;
        IGroundChainNoteData startNote;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter, IChartEditorDataSetter chartEditorDataSetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
            this.chartEditorDataSetter = chartEditorDataSetter;
        }

        private void Update()
        {
            EditMode editMode = chartEditorDataGetter.CurrentEditMode.Value;

            if (Input.GetMouseButtonDown(0) && editMode == EditMode.Connect) { StartConnect(); }
            else if (Input.GetMouseButtonDown(0) && editMode == EditMode.Connecting) { ConnectNote(); }
            else if (Input.GetMouseButtonDown(1)) { EndConnect(); }
        }

        /// <summary>
        /// 接続モードの開始
        /// </summary>
        private void StartConnect()
        {
            if(chartEditorDataGetter.CurrentEditMode.Value != EditMode.Connect) { return; }

            IConnectableObject connectableObject = chartEditorDataGetter.ConnectableObject.Value;
            if (connectableObject == null) { return; }

            if (connectableObject.Note.NoteData is not IGroundChainNoteData) { return; }

            chartEditorDataSetter.SetEditMode(EditMode.Connecting);
            startNote = (IGroundChainNoteData)connectableObject.Note.NoteData;
        }

        /// <summary>
        /// ノーツの接続
        /// </summary>
        private void ConnectNote()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.Connecting) { return; }
            if (startNote == null) { return; }

            IConnectableObject connectableObject = chartEditorDataGetter.ConnectableObject.Value;
            if (connectableObject == null) { return; }
            if (connectableObject.Note.NoteData is not IGroundChainNoteData) { return; }

            startNote.AddChainNote((IGroundChainNoteData)connectableObject.Note.NoteData);
        }

        /// <summary>
        /// 始点ノーツデータを更新
        /// </summary>
        /// <returns></returns>
        private void UpdateStartNote(IGroundChainNoteData note)
        {
            while (note.BackNote != null && note.BackNote.Value != null)
            {
                note = note.BackNote.Value;
            }

            startNote = note;
        }

        /// <summary>
        /// 接続モードの終了
        /// </summary>
        private void EndConnect()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.Connecting) { return; }
            chartEditorDataSetter.SetEditMode(EditMode.None);
        }
    }
}