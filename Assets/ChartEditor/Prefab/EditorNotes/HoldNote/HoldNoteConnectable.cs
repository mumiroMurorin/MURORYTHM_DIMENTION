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
        [SerializeField] NoteObject noteObject;

        [Header("ノーツマテリアル")]
        [SerializeField] Material startMaterial;
        [SerializeField] Material relayMaterial;
        [SerializeField] Material endMaterial;
        [SerializeField] Material meshMaterial;

        [Header("中継点の大きさ")]
        [Tooltip("中継点のノーツを、時間方向(画面では縦)に縮める割合。1 で始点・終点と同じ大きさ")]
        [SerializeField, Range(0.1f, 1f)] float relayDepthScale = 0.5f;

        [Header("同じ分線に並べたとき")]
        [Tooltip("同じ分線に、同じ接続のノーツを2つ以上置いたとき、2つ目以降を時間方向(画面では縦)にずらして見せる量。つながる順番が後ろのノーツほど上に見える。見た目だけで、譜面上の時刻は変わらない")]
        [SerializeField] float sameSubdivisionOffset = 0.15f;

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

        NoteObject IConnectableObject.Note => noteObject;

        // 次ノート
        ReactiveProperty<IConnectableObject> nextNote = new ReactiveProperty<IConnectableObject>();
        IReadOnlyReactiveProperty<IConnectableObject> IConnectableObject.NextNote => nextNote;
        void IConnectableObject.SetNextNote(IConnectableObject nextNote) { this.nextNote.Value = nextNote; }

        // 前ノート
        ReactiveProperty<IConnectableObject> backNote = new ReactiveProperty<IConnectableObject>();
        IReadOnlyReactiveProperty<IConnectableObject> IConnectableObject.BackNote => backNote;
        void IConnectableObject.SetBackNote(IConnectableObject backNote) { this.backNote.Value = backNote; }

        IChainNoteData IConnectableObject.ChainNoteData => noteObject != null ? noteObject.NoteData as IChainNoteData : null;

        // 同じ分線に並んだノーツの中で、このノーツが何番目か (0 が先頭)
        int subdivisionOrder;
        // ずらして見せる部品 (ノーツの絵・当たり判定・帯の端をまとめている親) と、ずらす前の位置
        Transform offsetBody;
        float offsetBodyBaseZ;
        // 音を鳴らすための当たり判定 (時刻どおりに鳴るよう、これだけは元の位置に残す)
        Transform offsetSound;
        float offsetSoundBaseZ;

        /// <summary>
        /// 同じ分線に、同じ接続のノーツが並んでいるとき、2番目以降を少し上にずらして見せる
        /// (ぴったり重なっていると、順番が分からず、片方しかクリックできず、間をつなぐ帯も面積が無くて見えないため)
        /// </summary>
        void IConnectableObject.SetSubdivisionOrder(int index)
        {
            if (subdivisionOrder == index) { return; }
            subdivisionOrder = index;

            if (offsetBody == null)
            {
                if (noteMeshRenderer == null) { return; }

                offsetBody = noteMeshRenderer.transform.parent;
                if (offsetBody == null || offsetBody == noteObject.transform)
                {
                    offsetBody = null;
                    return;
                }

                offsetBodyBaseZ = offsetBody.localPosition.z;

                var sound = offsetBody.GetComponentInChildren<NoteSoundCollider>(true);
                if (sound != null && sound.transform != offsetBody)
                {
                    offsetSound = sound.transform;
                    offsetSoundBaseZ = offsetSound.localPosition.z;
                }
            }

            float offset = subdivisionOrder * sameSubdivisionOffset;

            Vector3 bodyPosition = offsetBody.localPosition;
            bodyPosition.z = offsetBodyBaseZ + offset;
            offsetBody.localPosition = bodyPosition;

            if (offsetSound != null)
            {
                Vector3 soundPosition = offsetSound.localPosition;
                soundPosition.z = offsetSoundBaseZ - offset;
                offsetSound.localPosition = soundPosition;
            }
        }

        // メンバ変数
        GameObject meshObject;
        List<IDisposable> nextNoteDisposables = new List<IDisposable>();
        CancellationTokenSource cts = new CancellationTokenSource();
        bool isAlone;
        // 中継点を細長くする処理 (最初に種類が決まったときに用意する)
        NoteDepthScaler depthScaler;

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

            // ノーツの種類変更
            noteObject.NoteData.NoteTypeRP
                .Subscribe(ChangeNoteMaterial)
                .AddTo(this.gameObject);

            // 次ノーツが変わった時購読しなおす
            nextNote
                .Subscribe(next => {
                    DisposeHoldMesh();
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
                .Subscribe(_ =>
                {
                    GenerateMesh(nextNote.Value.MeshRightEdge.position, nextNote.Value.MeshLeftEdge.position);
                })
                .AddTo(this.gameObject);

            var disposable2 = Observable.IntervalFrame(2)
                .Select(_ => meshRightEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ =>
                {
                    GenerateMesh(nextNote.Value.MeshRightEdge.position, nextNote.Value.MeshLeftEdge.position);
                })
                .AddTo(this.gameObject);

            nextNoteDisposables.Add(disposable1);
            nextNoteDisposables.Add(disposable2);
        }

        /// <summary>
        /// 次ノーツに対するバインド
        /// </summary>
        /// <param name="nextNote"></param>
        private void BindForNextNote(IConnectableObject nextNote)
        {
            if (nextNote == null) { return; }

            // 2フレームごとに位置が変わってないかチェック
            var disposable1 = Observable.IntervalFrame(2)
                .Select(_ => nextNote.MeshLeftEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ =>
                {
                    GenerateMesh(nextNote.MeshRightEdge.position, nextNote.MeshLeftEdge.position);
                })
                .AddTo(this.gameObject);

            var disposable2 = Observable.IntervalFrame(2)
                .Select(_ => nextNote.MeshRightEdge.position)
                .DistinctUntilChanged()
                .Subscribe(_ =>
                {
                    GenerateMesh(nextNote.MeshRightEdge.position, nextNote.MeshLeftEdge.position);
                })
                .AddTo(this.gameObject);

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
                noteObject.SetWarning(NoteWarningType.UnconnectedHold, true);
                isAlone = true;
            }
            // 独立でない
            else if ((backNote.Value != null || nextNote.Value != null) && isAlone)
            {
                noteObject.OutlineColors.Remove(outlineColorOnAlone);
                noteObject.SetWarning(NoteWarningType.UnconnectedHold, false);
                isAlone = false;
            }
        }

        /// <summary>
        /// マテリアルの変更
        /// 差し替える前に付いていた色は引き継ぐ (色は別の処理(〜TypeChangable)が付けている。引き継がないと、白いノーツになることがある)
        /// </summary>
        /// <param name="holdNoteType"></param>
        private void ChangeNoteMaterial(DeploymentNoteType noteType)
        {
            switch (noteType)
            {
                case DeploymentNoteType.HoldStart:
                case DeploymentNoteType.DivineHoldStart:
                    NoteMaterialUtil.ReplaceKeepingColor(noteMeshRenderer, startMaterial);
                    break;
                case DeploymentNoteType.HoldRelay:
                case DeploymentNoteType.HoldMeshRelay:
                    NoteMaterialUtil.ReplaceKeepingColor(noteMeshRenderer, relayMaterial);
                    break;
                case DeploymentNoteType.HoldEnd:
                case DeploymentNoteType.HoldEndUnjudge:
                    NoteMaterialUtil.ReplaceKeepingColor(noteMeshRenderer, endMaterial);
                    break;
                default:
                    Debug.Log($"【Note】設定されていないパラメータです: {noteType}");
                    break;
            }

            // 中継点は、時間方向に縮めて細長くする
            // (短い間隔でいくつも並べても、前後のノーツや、その間の帯が隠れないように)
            bool isRelay = noteType == DeploymentNoteType.HoldRelay || noteType == DeploymentNoteType.HoldMeshRelay;
            depthScaler ??= new NoteDepthScaler(noteObject, noteMeshRenderer);
            depthScaler.SetScale(isRelay ? relayDepthScale : 1f);
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

            cts?.CancelAndDispose();
        }
    }

}