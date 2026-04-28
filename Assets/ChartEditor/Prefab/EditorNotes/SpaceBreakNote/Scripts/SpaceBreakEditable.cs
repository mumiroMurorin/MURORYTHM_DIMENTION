using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class SpaceBreakEditableObject : MonoBehaviour, ISpaceEditableObject
    {
        [SerializeField] NoteObject noteObject;
        IVerticesControlableNoteData noteData;
        IVerticesControlableNoteData ISpaceEditableObject.NoteData => noteData;

        CancellationTokenSource cts = new CancellationTokenSource();

        private void Start()
        {
            Load(cts.Token).Forget();
        }

        private async UniTask Load(CancellationToken token)
        {
            // ノートデータが存在するまで待つ
            await UniTask.WaitUntil(() => noteObject.NoteData != null, cancellationToken: token);

            // ノートデータが存在するまで待つ
            await UniTask.WaitUntil(() => noteObject.NoteData.Address != null, cancellationToken: token);

            // IVerticesControlableNoteDataに変換
            if (noteObject.NoteData is not IVerticesControlableNoteData noteData) { return; }
            this.noteData = noteData;
        }

        private void OnDestroy()
        {
            cts?.CancelAndDispose();
        }
    }

}
