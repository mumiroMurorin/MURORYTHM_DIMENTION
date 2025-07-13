using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class NoteConnecter : MonoBehaviour
    {
        const int UNCHAINED_INDEX = -1;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        INotesDataGetter notesGetter;

        IChainNoteData startNote;
        int connectingChainIndex;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, INotesDataGetter notesGetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            this.notesGetter = notesGetter;
        }

        private void Update()
        {
            EditMode editMode = dataGetter.CurrentEditMode.Value;

            if (Input.GetMouseButtonDown(0) && editMode == EditMode.Connect) { StartConnect(); }
            else if (Input.GetMouseButtonDown(0) && editMode == EditMode.Connecting) { ConnectNote(); }
            else if (Input.GetMouseButtonDown(1)) { EndConnect(); }
        }

        /// <summary>
        /// 接続モードの開始
        /// </summary>
        private void StartConnect()
        {
            if(dataGetter.CurrentEditMode.Value != EditMode.Connect) { return; }

            var collider = dataGetter.GetInteractableCollider<IConnectableCollider>();
            if(collider == null) { return; }

            IConnectableObject connectableObject = collider.Note;
            if (connectableObject == null) { return; }
            if (connectableObject.Note.NoteData is not IChainNoteData) { return; }

            dataSetter.SetEditMode(EditMode.Connecting);
            startNote = (IChainNoteData)connectableObject.Note.NoteData;

            connectingChainIndex = startNote.ChainIndex.Value;
        }

        /// <summary>
        /// ノーツの接続
        /// </summary>
        private void ConnectNote()
        {
            if (dataGetter.CurrentEditMode.Value != EditMode.Connecting) { return; }

            var collider = dataGetter.GetInteractableCollider<IConnectableCollider>();
            if (collider == null) { return; }

            IConnectableObject connectableObject = collider.Note;
            if (connectableObject == null) { return; }
            if (connectableObject.Note.NoteData is not IChainNoteData addNote) { return; }

            // 接続番号の取得
            // 接続元も追加ノートも番号を持っていなかったとき(両ノート未接続時)、新規取得
            if (connectingChainIndex == UNCHAINED_INDEX && addNote.ChainIndex.Value == UNCHAINED_INDEX)
            {
                connectingChainIndex = notesGetter.GetUsableChainNoteIndex();
            }
            // 追加ノートが番号を持っていた時
            // 元ノートが番号を持っていた場合は何もしない(connectingChainIndexに既に代入済)
            else if (connectingChainIndex == UNCHAINED_INDEX && addNote.ChainIndex.Value != UNCHAINED_INDEX)
            {
                connectingChainIndex = addNote.ChainIndex.Value;
            }
            // 逆にどっちも番号を持っていた時
            else if (connectingChainIndex != UNCHAINED_INDEX && addNote.ChainIndex.Value != UNCHAINED_INDEX)
            {

            }

            startNote.SetChainIndex(connectingChainIndex);
            addNote.SetChainIndex(connectingChainIndex);
        }

        /// <summary>
        /// 接続モードの終了
        /// </summary>
        private void EndConnect()
        {
            if (dataGetter.CurrentEditMode.Value != EditMode.Connecting) { return; }
            dataSetter.SetEditMode(EditMode.None);
        }
    }
}