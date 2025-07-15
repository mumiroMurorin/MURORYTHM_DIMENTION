using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using System;
using System.Linq;

namespace ChartEditor
{
    public class NoteConnecter : MonoBehaviour
    {
        const int UNCHAINED_INDEX = -1;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        INotesDataGetter notesGetter;

        Dictionary<IChainNoteData, List<IDisposable>> noteDataToDisposables = new Dictionary<IChainNoteData, List<IDisposable>>();
        IChainNoteData startNote;
        bool isConnecting;
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

            if(editMode != EditMode.Connect && editMode != EditMode.Connecting && editMode != EditMode.DisConnect) 
            {
                isConnecting = false;
                return; 
            }

            // Ctrl押されている間は接続解除モード
            if (Input.GetKey(KeyCode.LeftControl)) { dataSetter.SetEditMode(EditMode.DisConnect); }
            // それ以外は接続モード
            else if (isConnecting) { dataSetter.SetEditMode(EditMode.Connecting); }
            else { dataSetter.SetEditMode(EditMode.Connect); }

            editMode = dataGetter.CurrentEditMode.Value;

            // 接続モード開始
            if (Input.GetMouseButtonDown(0) && editMode == EditMode.Connect) { StartConnectOnClick(); }
            // 接続解除
            else if (Input.GetMouseButtonDown(0) && editMode == EditMode.DisConnect) { DisConnectNoteOnClick(); }
            // 接続
            else if (Input.GetMouseButtonDown(0) && editMode == EditMode.Connecting) { ConnectNoteOnClick(); }
            // 接続モード終了
            else if (Input.GetMouseButtonDown(1)) { EndConnect(); }
        }

        /// <summary>
        /// 接続モードの開始
        /// </summary>
        private void StartConnectOnClick()
        {
            var collider = dataGetter.GetInteractableCollider<IConnectableCollider>();
            if(collider == null) { return; }

            IConnectableObject connectableObject = collider.Note;
            if (connectableObject == null) { return; }
            if (connectableObject.Note.NoteData is not IChainNoteData) { return; }

            startNote = (IChainNoteData)connectableObject.Note.NoteData;
            connectingChainIndex = startNote.ChainIndex.Value;

            dataSetter.SetEditMode(EditMode.Connecting);
            isConnecting = true;
        }

        /// <summary>
        /// ノーツの接続
        /// </summary>
        private void ConnectNoteOnClick()
        {
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
            // 追加ノートが番号を持っていたとき、番号を更新
            // 元ノートが番号を持っていた場合は何もしない(connectingChainIndexに既に代入済)
            else if (connectingChainIndex == UNCHAINED_INDEX && addNote.ChainIndex.Value != UNCHAINED_INDEX)
            {
                connectingChainIndex = addNote.ChainIndex.Value;
            }

            // 繋がってるノーツは全部繋げる
            var chainList = notesGetter.GetChainNoteList(addNote.ChainIndex.Value)?.ChainNoteList.ToList();
            if(chainList != null)
            {
                ConnectNote(startNote, connectingChainIndex);
                foreach (var chain in chainList)
                {
                    ConnectNote(chain, connectingChainIndex);
                }
            }
            // つながってないとき
            else
            {
                ConnectNote(startNote, connectingChainIndex);
                ConnectNote(addNote, connectingChainIndex);
            }
        }

        /// <summary>
        /// ノーツの接続解除
        /// </summary>
        private void DisConnectNoteOnClick()
        {
            var collider = dataGetter.GetInteractableCollider<IConnectableCollider>();
            if (collider == null) { return; }

            IConnectableObject connectableObject = collider.Note;
            if (connectableObject == null) { return; }
            if (connectableObject.Note.NoteData is not IChainNoteData removeNote) { return; }

            DisconnectNote(removeNote);
        }

        /// <summary>
        /// ノートの接続
        /// </summary>
        /// <param name="chainNote"></param>
        /// <param name="chainIndex"></param>
        private void ConnectNote(IChainNoteData chainNote, int chainIndex)
        {
            // データの入れ替え
            notesGetter.RemoveChainNote(chainNote);
            chainNote.SetChainIndex(chainIndex);
            notesGetter.AddChainNote(chainNote);

            // ノート順のアップデート
            UpdateChainNoteObj(chainNote, chainIndex);

            // すでに購読されていたとき、破棄
            if (noteDataToDisposables.TryGetValue(chainNote, out var disposables))
            {
                foreach(var dis in disposables) { dis.Dispose(); }
                noteDataToDisposables.Remove(chainNote);
            }

            // 購読
            var noteList = notesGetter.GetChainNoteList(chainIndex);
            var disposable1 = noteList.ChainNoteList.ObserveCountChanged()
                .Subscribe(_ => {
                    UpdateChainNoteObj(chainNote, chainIndex);
                })
                .AddTo(this.gameObject);

            var disposable2 = chainNote.Address.BarIndexRP
                .Subscribe(_ => { noteList.UpdateChainNoteData(chainNote); })
                .AddTo(this.gameObject);

            var disposable3 = chainNote.Address.SubDivisionIndexRP
                .Subscribe(_ => { noteList.UpdateChainNoteData(chainNote); })
                .AddTo(this.gameObject);

            // 購読データの追加、更新
            var disList = new List<IDisposable>() { disposable1, disposable2, disposable3 };
            if (!noteDataToDisposables.TryAdd(chainNote, disList))
            {
                noteDataToDisposables[chainNote] = disList;
            }
        }

        /// <summary>
        /// ノートの接続解除
        /// </summary>
        /// <param name="chainNote"></param>
        private void DisconnectNote(IChainNoteData chainNote)
        {
            notesGetter.RemoveChainNote(chainNote);
            chainNote.SetChainIndex(-1);
            UpdateChainNoteObj(chainNote, -1);

            // 購読の破棄
            if (noteDataToDisposables.TryGetValue(chainNote, out var disposables))
            {
                foreach (var dis in disposables) { dis.Dispose(); }
                noteDataToDisposables.Remove(chainNote);
            }
        }


        /// <summary>
        /// ノートの前後を更新
        /// </summary>
        /// <param name="chainNote"></param>
        /// <param name="chainIndex"></param>
        private void UpdateChainNoteObj(IChainNoteData chainNote, int chainIndex)
        {
            // NextNoteとBackNoteを更新する
            var noteList = notesGetter.GetChainNoteList(chainIndex);
            int index = noteList != null ? noteList.IndexOf(chainNote) : -1;

            if(index < 0) 
            {
                chainNote.NoteObject.SetBackNote(null);
                chainNote.NoteObject.SetNextNote(null);
                return;
            }

            var backNoteObj = index > 0 ? noteList.ChainNoteList[index - 1].NoteObject : null;
            var nextNoteObj = index < noteList.ChainNoteList.Count - 1 ? noteList.ChainNoteList[index + 1].NoteObject : null;

            chainNote.NoteObject.SetBackNote(backNoteObj);
            chainNote.NoteObject.SetNextNote(nextNoteObj);
        }

        /// <summary>
        /// 接続モードの終了
        /// </summary>
        private void EndConnect()
        {
            if (dataGetter.CurrentEditMode.Value != EditMode.Connecting) { return; }

            dataSetter.SetEditMode(EditMode.None);
            isConnecting = false;
        }
    }
}