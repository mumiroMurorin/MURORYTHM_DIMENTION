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

        [Tooltip("警告アウトライン色")]
        [SerializeField] ColorSetting outlineColorOnAlone;

        [Space(40)]
        [SerializeField] float meshHeight = 0.01f;
        [SerializeField] MeshRenderer noteMeshRenderer;
        [SerializeField] Transform meshRightEdge;
        [SerializeField] Transform meshLeftEdge;

        public Action OnDestroyListner { get; set; }

        // 右端左端
        Transform IConnectableObject.MeshRightEdge => meshRightEdge;
        Transform IConnectableObject.MeshLeftEdge => meshLeftEdge;

        // ノート本体
        NoteObject noteObject;
        NoteObject IConnectableObject.Note => noteObject;

        // 次ノート
        ReactiveProperty<IConnectableObject> nextNote = new ReactiveProperty<IConnectableObject>();
        IReadOnlyReactiveProperty<IConnectableObject> IConnectableObject.NextNote => nextNote;
        void IConnectableObject.SetNextNote(IConnectableObject nextNote) { this.nextNote.Value = nextNote; }

        // 前ノート
        ReactiveProperty<IConnectableObject> backNote = new ReactiveProperty<IConnectableObject>();
        IReadOnlyReactiveProperty<IConnectableObject> IConnectableObject.BackNote => backNote;
        void IConnectableObject.SetBackNote(IConnectableObject backNote) { this.backNote.Value = backNote; }

        // メンバ変数
        GameObject meshObject;
        List<IDisposable> nextNoteDisposables = new List<IDisposable>();
        CancellationTokenSource cts = new CancellationTokenSource();
        bool isAlone;

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

            // 次ノーツが変わった時購読しなおす
            nextNote
                .Subscribe(next => {
                    DisposeHoldMesh();
                    ChangeNoteMaterial(backNote.Value, next);
                    SetWarning();
                    if (next != null)
                    {
                        BindForThisNote();
                        BindForNextNote(next);
                    }
                })
                .AddTo(this.gameObject);

            backNote
                .Subscribe(back => {
                    SetWarning();
                    ChangeNoteMaterial(back, nextNote.Value);
                })
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// このノーツに対するバインド
        /// </summary>
        /// <param name="thisNote"></param>
        private void BindForThisNote()
        {
            if (this.nextNote == null) { return; }
            if (this.nextNote.Value == null) { return; }

            // 2フレームごとに位置が変わってないかチェック
            var disposable1 = Observable.IntervalFrame(2)
                .Select(_ => meshLeftEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ => GenerateMesh(nextNote.Value.MeshRightEdge.position, nextNote.Value.MeshLeftEdge.position))
                .AddTo(this);

            var disposable2 = Observable.IntervalFrame(2)
                .Select(_ => meshRightEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ => GenerateMesh(nextNote.Value.MeshRightEdge.position, nextNote.Value.MeshLeftEdge.position))
                .AddTo(this);

            nextNoteDisposables.Add(disposable1);
            nextNoteDisposables.Add(disposable2);
        }

        /// <summary>
        /// 次ノーツに対するバインド
        /// </summary>
        /// <param name="nextNote"></param>
        private void BindForNextNote(IConnectableObject nextNote)
        {
            // 2フレームごとに位置が変わってないかチェック
            var disposable1 = Observable.IntervalFrame(2)
                .Select(_ => nextNote.MeshLeftEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ => GenerateMesh(nextNote.MeshRightEdge.position, nextNote.MeshLeftEdge.position))
                .AddTo(this);

            var disposable2 = Observable.IntervalFrame(2)
                .Select(_ => nextNote.MeshRightEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ => GenerateMesh(nextNote.MeshRightEdge.position, nextNote.MeshLeftEdge.position))
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
        /// 警告の表示
        /// </summary>
        private void SetWarning()
        {
            // 独りぼっち
            if (backNote.Value == null && nextNote.Value == null && !isAlone)
            {
                noteObject.OutlineColors.Add(outlineColorOnAlone);
                isAlone = true;
            }
            // 独立でない
            else if ((backNote.Value != null || nextNote.Value != null) && isAlone) 
            {
                noteObject.OutlineColors.Remove(outlineColorOnAlone);
                isAlone = false;
            }
        }

        /// <summary>
        /// マテリアルの変更
        /// </summary>
        /// <param name="holdNoteType"></param>
        private void ChangeNoteMaterial(IConnectableObject back, IConnectableObject next)
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
                new Vector3(meshLeftEdge.position.x, meshLeftEdge.position.y + meshHeight, meshLeftEdge.position.z),
                new Vector3(nextLeft.x, nextLeft.y + meshHeight, nextLeft.z),
                new Vector3(nextRight.x, nextLeft.y + meshHeight, nextRight.z),
                new Vector3(meshRightEdge.position.x, meshRightEdge.position.y + meshHeight, meshRightEdge.position.z)
                );
            meshFilter.mesh = mesh;

            meshRenderer.material = meshMaterial;
            meshObject.transform.SetParent(noteObject.transform);
        }

        private void OnDestroy()
        {
            OnDestroyListner?.Invoke();

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
        }
    }

}