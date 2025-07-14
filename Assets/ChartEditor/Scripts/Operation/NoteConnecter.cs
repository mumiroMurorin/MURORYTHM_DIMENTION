using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using System;

namespace ChartEditor
{
    public class NoteConnecter : MonoBehaviour
    {
        const int UNCHAINED_INDEX = -1;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        INotesDataGetter notesGetter;

        Dictionary<IChainNoteData, IDisposable> noteDataToDisposable = new Dictionary<IChainNoteData, IDisposable>();
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

            if (Input.GetMouseButtonDown(0) && editMode == EditMode.Connect) { StartConnectOnClick(); }
            else if (Input.GetMouseButtonDown(0) && editMode == EditMode.Connecting) { ConnectNoteOnClick(); }
            else if (Input.GetMouseButtonDown(1)) { EndConnect(); }
        }

        /// <summary>
        /// 接続モードの開始
        /// </summary>
        private void StartConnectOnClick()
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
        private void ConnectNoteOnClick()
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
                startNote.SetChainIndex(connectingChainIndex);
            }
            // 追加ノートが番号を持っていたとき、番号を更新
            // 元ノートが番号を持っていた場合は何もしない(connectingChainIndexに既に代入済)
            else if (connectingChainIndex == UNCHAINED_INDEX && addNote.ChainIndex.Value != UNCHAINED_INDEX)
            {
                connectingChainIndex = addNote.ChainIndex.Value;
                startNote.SetChainIndex(connectingChainIndex);
            }

            // 繋がってるノーツは全部繋げる
            var chainList = notesGetter.GetChainNoteList(addNote.ChainIndex.Value)?.ChainNoteList;
            if(chainList != null)
            {
                foreach (var chain in chainList)
                {
                    ConnectNote(chain, connectingChainIndex);
                }
            }
        }

        private void ConnectNote(IChainNoteData chainNote, int chainIndex)
        {
            // データの入れ替え
            notesGetter.RemoveChainNote(chainNote);
            chainNote.SetChainIndex(chainIndex);
            notesGetter.AddChainNote(chainNote);
            UpdateChainNoteObj(chainNote, chainIndex);

            // すでに購読されていたとき、破棄
            if (noteDataToDisposable.TryGetValue(chainNote, out var dis))
            {
                dis.Dispose();
            }

            // 購読
            var noteList = notesGetter.GetChainNoteList(chainIndex);
            var dispossable = noteList.ChainNoteList.ObserveCountChanged()
                .Subscribe(_ => {
                    UpdateChainNoteObj(chainNote, chainIndex);
                })
                .AddTo(this.gameObject);

            // 購読データの追加、更新
            if(!noteDataToDisposable.TryAdd(chainNote, dispossable))
            {
                noteDataToDisposable[chainNote] = dispossable;
            }
        }

        /// <summary>
        /// ChainNoteDataに対する購読
        /// </summary>
        /// <param name="chainNote"></param>
        /// <param name="chainIndex"></param>
        private void UpdateChainNoteObj(IChainNoteData chainNote, int chainIndex)
        {
            // NextNoteとBackNoteを更新する

            var noteList = notesGetter.GetChainNoteList(chainIndex);
            int index = noteList.IndexOf(chainNote);

            if(index < 0) 
            { 
                Debug.LogWarning($"【Connecter】該当ノートが見つかりませんでした: {chainNote}, {chainIndex}"); 
                return;
            }

            var backNoteObj = index > 0 ? noteList.ChainNoteList[index - 1].NoteObject : null;
            var nextNoteObj = index < noteList.ChainNoteList.Count - 1 ? noteList.ChainNoteList[index + 1].NoteObject : null;

            chainNote.NoteObject.SetBackNote(backNoteObj);
            chainNote.NoteObject.SetNextNote(nextNoteObj);
            Debug.Log($"きちゃ: {chainIndex}, {backNoteObj}, {nextNoteObj}");
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