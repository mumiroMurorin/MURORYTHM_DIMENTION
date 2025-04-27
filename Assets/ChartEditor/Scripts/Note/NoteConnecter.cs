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

            UpdateStartNote((IGroundChainNoteData)connectableObject.Note.NoteData);
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

            RegisterChainNote((IGroundChainNoteData)connectableObject.Note.NoteData);
            UpdateStartNote(startNote);
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
        /// ノーツデータを登録
        /// </summary>
        /// <param name="addNote"></param>
        private void RegisterChainNote(IGroundChainNoteData addNote)
        {
            IGroundChainNoteData comparisonNote = startNote;

            // 最初のノーツ以前のとき
            if (!comparisonNote.Address.IsEarlierThan(addNote.Address))
            {
                // 同じノートは追加できない
                if (comparisonNote == addNote) { return; }

                Debug.Log($"{addNote}, {comparisonNote}");

                addNote.SetNextNote(comparisonNote);
                comparisonNote.SetBackNote(addNote);
                return;
            }

            // 比較ノートが追加ノートの後続位置に来るまで繰り返す
            while (comparisonNote.Address.IsEarlierThan(addNote.Address))
            {
                // 終点以降のノートは追加できない
                if(comparisonNote.NextNote == null) { return; }
                // 最後のノートまで行ったら後続に追加
                if(comparisonNote.NextNote.Value == null) { break; }

                comparisonNote = comparisonNote.NextNote.Value;
            }

            // 同じノートは追加できない
            if (comparisonNote == addNote) { return; }

            // 始点以前のノートは追加できない
            if (comparisonNote.BackNote == null && !comparisonNote.Address.IsEarlierThan(addNote.Address)) { return; }

            // データをセット
            Debug.Log($"{addNote}, {comparisonNote}");
            addNote.SetNextNote(comparisonNote.NextNote.Value);
            addNote.SetBackNote(comparisonNote); 
            comparisonNote.SetNextNote(addNote); 
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