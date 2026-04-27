using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class SpaceHoldNoteTypeChangable : MonoBehaviour, IChangableObject
    {
        [SerializeField] NoteObject noteObject;
        [SerializeField] MeshRenderer noteMeshRenderer;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color hiddenColor = new Color(1, 1, 1, 0.25f); 

        ITypeChangableNoteData noteData;
        ITypeChangableNoteData IChangableObject.NoteData => noteData;

        CancellationTokenSource cts = new CancellationTokenSource();

        private void Start()
        {
            Bind(cts.Token).Forget();
        }

        private async UniTask Bind(CancellationToken token)
        {
            // ノートデータが存在するまで待つ
            await UniTask.WaitUntil(() => noteObject.NoteData != null, cancellationToken: token);

            // ノートデータが存在するまで待つ
            await UniTask.WaitUntil(() => noteObject.NoteData.Address != null, cancellationToken: token);

            // ITypeChangableNoteDataに変換
            if (noteObject.NoteData is not ITypeChangableNoteData) { return; }
            noteData = (ITypeChangableNoteData)noteObject.NoteData;

            // ノーツタイプが変更された時
            noteData.NoteTypeRP
                .Subscribe(ChangeNoteColor)
                .AddTo(this.gameObject);

            // IChainNoteDataに変換
            if (noteObject.NoteData is not IChainNoteData chainData) { return; }

            chainData.NoteObject.NextNote
                .Subscribe(next => {
                    ChangeNoteColor(noteData.NoteTypeRP.Value);
                })
                .AddTo(this.gameObject);

            chainData.NoteObject.BackNote
                .Subscribe(back => {
                    ChangeNoteColor(noteData.NoteTypeRP.Value);
                })
                .AddTo(this.gameObject);
        }

        private void ChangeNoteColor(DeploymentNoteType noteType)
        {
            if (noteType == DeploymentNoteType.SpaceHoldStart || noteType == DeploymentNoteType.SpaceHoldRelay || noteType == DeploymentNoteType.SpaceHoldEnd) { noteMeshRenderer.material.color = normalColor; }
            else if (noteType == DeploymentNoteType.SpaceHoldMeshRelay) { noteMeshRenderer.material.color = hiddenColor; }
        }

        private void OnDestroy()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
        }
    }

}