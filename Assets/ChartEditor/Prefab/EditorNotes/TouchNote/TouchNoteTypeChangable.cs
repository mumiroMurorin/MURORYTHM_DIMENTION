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
    public class TouchNoteTypeChangable : MonoBehaviour, IChangableObject
    {
        [SerializeField] MeshRenderer noteMeshRenderer;
        [SerializeField] Material normalMaterial;
        [SerializeField] Material divineMaterial;

        NoteObject noteObject;
        ITypeChangableNoteData noteData;
        ITypeChangableNoteData IChangableObject.NoteData => noteData;

        CancellationTokenSource cts = new CancellationTokenSource();

        private void Start()
        {
            noteObject = GetComponent<NoteObject>();
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
                .Subscribe(ChangeNoteMaterial)
                .AddTo(this.gameObject);
        }

        private void ChangeNoteMaterial(DeploymentNoteType noteType)
        {
            if (noteType == DeploymentNoteType.Touch) { noteMeshRenderer.material = normalMaterial; }
            else if (noteType == DeploymentNoteType.DivineTouch) { noteMeshRenderer.material = divineMaterial; }
        }

        public void OnChangeNoteType()
        {

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