using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;
using MeshGenerate;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class HoldNoteConnectable : MonoBehaviour, IConnectableObject
    {
        [Header("ノーツマテリアル")]
        [SerializeField] Material startMaterial;
        [SerializeField] Material relayMaterial;
        [SerializeField] Material endMaterial;
        [SerializeField] Material meshMaterial;
        [Space(40)]
        [SerializeField] float meshHeight = 0.01f;
        [SerializeField] MeshRenderer noteMeshRenderer;
        [SerializeField] Transform meshRightEdge;
        [SerializeField] Transform meshLeftEdge;

        Transform IConnectableObject.MeshRightEdge => meshRightEdge;
        Transform IConnectableObject.MeshLeftEdge => meshLeftEdge;

        NoteObject noteObject;
        NoteObject IConnectableObject.Note => noteObject;

        GameObject meshObject;
        IGroundChainNoteData chainNoteData;
        List<IDisposable> nextNoteDisposables = new List<IDisposable>();
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
            chainNoteData = (IGroundChainNoteData)(noteObject.NoteData);

            // 次ノーツが変わった時購読しなおす
            chainNoteData.NextNote
                .Subscribe(next => {
                    DisposeHoldMesh();
                    ChangeNoteMaterial(chainNoteData.BackNote.Value, next);
                    if(next != null)
                    {
                        BindForThisNote(chainNoteData);
                        BindForNextNote(next);
                    }
                })
                .AddTo(this.gameObject);

            chainNoteData.BackNote
                .Subscribe(back => {
                    ChangeNoteMaterial(back, chainNoteData.NextNote.Value);
                })
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// このノーツに対するバインド
        /// </summary>
        /// <param name="thisNote"></param>
        private void BindForThisNote(IGroundChainNoteData thisNote)
        {
            if (thisNote.NextNote == null) { return; }
            if (thisNote.NextNote.Value == null) { return; }
            IGroundChainNoteData nextNote = thisNote.NextNote.Value;

            // 2フレームごとに位置が変わってないかチェック
            var disposable1 = Observable.IntervalFrame(2)
                .Select(_ => meshLeftEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ => GenerateMesh(nextNote.NoteObject.MeshRightEdge.position, nextNote.NoteObject.MeshLeftEdge.position))
                .AddTo(this);

            var disposable2 = Observable.IntervalFrame(2)
                .Select(_ => meshRightEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ => GenerateMesh(nextNote.NoteObject.MeshRightEdge.position, nextNote.NoteObject.MeshLeftEdge.position))
                .AddTo(this);

            nextNoteDisposables.Add(disposable1);
            nextNoteDisposables.Add(disposable2);
        }

        /// <summary>
        /// 次ノーツに対するバインド
        /// </summary>
        /// <param name="nextNote"></param>
        private void BindForNextNote(IGroundChainNoteData nextNote)
        {
            // 2フレームごとに位置が変わってないかチェック
            var disposable1 = Observable.IntervalFrame(2)
                .Select(_ => nextNote.NoteObject.MeshLeftEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ => GenerateMesh(nextNote.NoteObject.MeshRightEdge.position, nextNote.NoteObject.MeshLeftEdge.position))
                .AddTo(this);

            var disposable2 = Observable.IntervalFrame(2)
                .Select(_ => nextNote.NoteObject.MeshRightEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ => GenerateMesh(nextNote.NoteObject.MeshRightEdge.position, nextNote.NoteObject.MeshLeftEdge.position))
                .AddTo(this);

            nextNoteDisposables.Add(disposable1);
            nextNoteDisposables.Add(disposable2);
        }

        /// <summary>
        /// 次ノーツに対するバインドを消す
        /// </summary>
        private void DisposeHoldMesh()
        {
            if (meshObject != null) { Destroy(meshObject); }

            foreach (var dis in nextNoteDisposables)
            {
                dis.Dispose();
            }

            nextNoteDisposables = new List<IDisposable>();
        }

        /// <summary>
        /// マテリアルの変更
        /// </summary>
        /// <param name="holdNoteType"></param>
        private void ChangeNoteMaterial(IGroundChainNoteData back, IGroundChainNoteData next)
        {
            if (back != null && next != null) { noteMeshRenderer.material = relayMaterial; }
            else if (back != null && next == null) { noteMeshRenderer.material = endMaterial; }
            else if (back == null && next != null) { noteMeshRenderer.material = startMaterial; }
            else { noteMeshRenderer.material = relayMaterial; }
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
        }

        private void OnDestroy()
        {
            chainNoteData?.RemoveNote();

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
        }
    }

}