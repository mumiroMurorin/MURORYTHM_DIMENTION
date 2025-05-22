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
    public class HoldNoteTypeChangable : MonoBehaviour, IChangableObject
    {
        [SerializeField] MeshRenderer noteMeshRenderer;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color hiddenColor = new Color(1, 1, 1, 0.25f); 
        [SerializeField] Color hiddenJudgedColor = new Color(1, 0.5f, 0.5f, 0.25f); 

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

            // IGroundChainNoteDataに変換
            if(noteObject.NoteData is not ITypeChangableNoteData) { return; }
            noteData = (ITypeChangableNoteData)noteObject.NoteData;

            // ノーツタイプが変更された時
            noteData.NoteTypeRP
                .Subscribe(type => {
                    if (type == DeploymentNoteType.Hold) { noteMeshRenderer.material.color = normalColor; }
                    else if(type == DeploymentNoteType.HoldHidden) { noteMeshRenderer.material.color = hiddenColor; }
                    else if(type == DeploymentNoteType.HoldHiddenJudged) { noteMeshRenderer.material.color = hiddenJudgedColor; }
                })
                .AddTo(this.gameObject);
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