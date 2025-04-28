using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;
using MeshGenerate;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class NoteConnectable : MonoBehaviour, IConnectableObject
    {
        [SerializeField] Material meshMaterial;
        [SerializeField] float meshHeight = 0.01f;
        [SerializeField] Transform meshRightEdge;
        [SerializeField] Transform meshLeftEdge;

        Transform IConnectableObject.MeshRightEdge => meshRightEdge;
        Transform IConnectableObject.MeshLeftEdge => meshLeftEdge;

        NoteObject noteObject;
        NoteObject IConnectableObject.Note => noteObject;

        bool isSubscribedThisNote;
        GameObject meshObject;
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
            if (noteObject.NoteData is not IGroundChainNoteData) { return; }
            var data = (IGroundChainNoteData)(noteObject.NoteData);

            // 次ノーツが変わった時購読しなおす
            data.NextNote?
                .Where(next => next != null)
                .Subscribe(next => {
                    BindForThisNote(data);
                    BindForNextNote(next);
                })
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// このノーツに対するバインド
        /// </summary>
        /// <param name="thisNote"></param>
        private void BindForThisNote(IGroundChainNoteData thisNote)
        {
            if (isSubscribedThisNote) { return; }
            if (thisNote.NextNote == null) { return; }
            if (thisNote.NextNote.Value == null) { return; }
            IGroundChainNoteData nextNote = thisNote.NextNote.Value;
            
            // 2フレームごとに位置が変わってないかチェック
            Observable.IntervalFrame(2)
                .Select(_ => meshLeftEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ => GenerateMesh(nextNote.NoteObject.MeshRightEdge.position, nextNote.NoteObject.MeshLeftEdge.position))
                .AddTo(this);

            Observable.IntervalFrame(2)
                .Select(_ => meshRightEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ => GenerateMesh(nextNote.NoteObject.MeshRightEdge.position, nextNote.NoteObject.MeshLeftEdge.position))
                .AddTo(this);

            isSubscribedThisNote = true;
        }

        /// <summary>
        /// 次ノーツに対するバインド
        /// </summary>
        /// <param name="nextNote"></param>
        private void BindForNextNote(IGroundChainNoteData nextNote)
        {
            // 2フレームごとに位置が変わってないかチェック
            Observable.IntervalFrame(2)
                .Select(_ => nextNote.NoteObject.MeshLeftEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ => GenerateMesh(nextNote.NoteObject.MeshRightEdge.position, nextNote.NoteObject.MeshLeftEdge.position))
                .AddTo(this);

            Observable.IntervalFrame(2)
                .Select(_ => nextNote.NoteObject.MeshRightEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ => GenerateMesh(nextNote.NoteObject.MeshRightEdge.position, nextNote.NoteObject.MeshLeftEdge.position))
                .AddTo(this);
        }

        private void GenerateMesh(Vector3 nextRight, Vector3 nextLeft)
        {
            if(meshObject != null) { Destroy(meshObject); }

            meshObject = new GameObject("Mesh");
            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
            Mesh mesh = MeshGenerator.GenerateMesh(
                new Vector3(meshLeftEdge.position.x, meshHeight, meshLeftEdge.position.z),
                new Vector3(nextLeft.x, meshHeight, nextLeft.z),
                new Vector3(nextRight.x, meshHeight, nextRight.z),
                new Vector3(meshRightEdge.position.x, meshHeight, meshRightEdge.position.z)
                );
            meshFilter.mesh = mesh;

            meshRenderer.material = meshMaterial;
            meshObject.transform.SetParent(noteObject.transform);
            //meshObject.transform.localScale = Vector3.one;
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