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
        [SerializeField] GameObject origin;
        [SerializeField] Transform meshRightEdge;
        [SerializeField] Transform meshLeftEdge;

        Transform IConnectableObject.MeshRightEdge => meshRightEdge;
        Transform IConnectableObject.MeshLeftEdge => meshLeftEdge;

        NoteObject noteObject;
        NoteObject IConnectableObject.Note => noteObject;

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

            if(noteObject.NoteData is not IGroundChainNoteData) { return; }
            var data = (IGroundChainNoteData)(noteObject.NoteData);

            // 次ノーツが変わった時
            data.NextNote?
                .Where(next => next != null)
                .Subscribe(next => GenerateMesh(next.NoteObject.MeshLeftEdge.position, next.NoteObject.MeshRightEdge.position))
                .AddTo(this.gameObject);
        }

        private void GenerateMesh(Vector3 nextRight, Vector3 nextLeft)
        {
            if(meshObject != null) { Destroy(meshObject); }

            Debug.Log("きちゃ～");

            meshObject = new GameObject("Mesh");
            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
            Debug.Log(string.Join(",", new List<Vector3>() { meshRightEdge.position, meshLeftEdge.position, nextLeft, nextRight }));
            Mesh mesh = MeshGenerator.GenerateMesh(new List<Vector3>() { meshRightEdge.position, meshLeftEdge.position, nextLeft, nextRight });
            meshFilter.mesh = mesh;

            meshRenderer.material = meshMaterial;
            meshObject.transform.SetParent(origin.transform);
            meshObject.transform.localScale = Vector3.one;
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