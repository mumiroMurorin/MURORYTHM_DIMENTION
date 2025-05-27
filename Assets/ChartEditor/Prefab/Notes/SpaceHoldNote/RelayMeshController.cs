using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class RelayMeshController : MonoBehaviour
    {
        [SerializeField] GameObject vertexObject;

        NoteObject noteObject;
        IVerticesControlableNoteData verticesData;
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

            // IGroundChainNoteDataに変換
            if (noteObject.NoteData is not IVerticesControlableNoteData) { return; }
            verticesData = (IVerticesControlableNoteData)noteObject.NoteData;

            // ObserveCountChanged()は初期化してくれないので、最初に購読
            foreach(var vertex in verticesData.SpaceHoldVertices.Vertices)
            {
                OnAddVertex(vertex);
            }

            // 辺の変更通知に対してスケール更新
            // 追加されたとき
            verticesData.SpaceHoldVertices.Vertices.ObserveAdd()
                .Subscribe(vertex => OnAddVertex(vertex.Value))
                .AddTo(this.gameObject);

            // 削除されたとき
            verticesData.SpaceHoldVertices.Vertices.ObserveRemove()
                .Subscribe(vertex => OnRemoveVertex(vertex.Value))
                .AddTo(this.gameObject);
        }

        private void OnAddVertex(SpaceHoldVertex vertex)
        {
            var obj = Instantiate(vertexObject);
            if(!obj.TryGetComponent(out VertexObject vertexObj))
            {
                Debug.LogWarning("【Vertex】オブジェクトにVertexObjectがアタッチされていません");
                return;
            }

            vertexObj.SetVertexData(vertex);
        }

        private void OnRemoveVertex(SpaceHoldVertex vertex)
        {

        }
    }
}

