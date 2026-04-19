using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Linq;
using MeshGenerate;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class RelayMeshController : MonoBehaviour
    {
        [SerializeField] GameObject colliderObject;
        [SerializeField] Transform vertexObjParent;
        [SerializeField] GameObject vertexObject;
        [SerializeField] Material centerMeshMaterial;

        [SerializeField] float leftLimit;
        [SerializeField] float rightLimit;
        [SerializeField] float upperLimit;
        [SerializeField] float lowerLimit;

        NoteObject noteObject;
        IVerticesControlableNoteData verticesData;
        MeshFilter centerMeshFilter;
        List<DataToVertexObject> dataToObj = new List<DataToVertexObject>();
        CancellationTokenSource cts = new CancellationTokenSource();

        private void Start()
        {
            noteObject = GetComponent<NoteObject>();
            Initialize();
            Bind(cts.Token).Forget();
        }

        private async UniTask Bind(CancellationToken token)
        {
            // ノートデータが存在するまで待つ
            await UniTask.WaitUntil(() => noteObject.NoteData != null, cancellationToken: token);

            // IVerticesControlableNoteDataに変換
            if (noteObject.NoteData is not IVerticesControlableNoteData) { return; }
            verticesData = (IVerticesControlableNoteData)noteObject.NoteData;

            // ObserveCountChanged()は初期化してくれないので、最初に購読
            for (int i = 0; i < verticesData.SpaceHoldVertices.Vertices.Count; i++)
            {
                var vertex = verticesData.SpaceHoldVertices.Vertices[i];
                OnAddVertex(vertex, i);
            }

            // 辺の変更通知に対してスケール更新
            // 追加されたとき
            verticesData.SpaceHoldVertices.Vertices.ObserveAdd()
                .Subscribe(vertex => OnAddVertex(vertex.Value, vertex.Index))
                .AddTo(this.gameObject);

            // 削除されたとき
            verticesData.SpaceHoldVertices.Vertices.ObserveRemove()
                .Subscribe(vertex => OnRemoveVertex(vertex.Value))
                .AddTo(this.gameObject);

            // クリアされたとき
            verticesData.SpaceHoldVertices.Vertices.ObserveReset()
                .Subscribe(_ => OnClearVertex())
                .AddTo(this.gameObject);
        }

        private void Initialize()
        {
            GenerateCenterMeshParent();
        }

        private void OnAddVertex(VertexData vertex, int index)
        {
            var obj = Instantiate(vertexObject);
            if (!obj.TryGetComponent(out VertexObject vertexObj))
            {
                Debug.LogWarning("【Vertex】オブジェクトにVertexObjectがアタッチされていません");
                return;
            }

            dataToObj.Insert(index, new DataToVertexObject(vertex, vertexObj));

            vertexObj.gameObject.transform.SetParent(vertexObjParent);
            vertexObj.gameObject.transform.localPosition = Vector3.zero;
            vertexObj.Initialize(
                vertex, 
                () => {
                    UpdateMesh();
                    UpdateColliderObjectScale();
                },
                ConvertPositionOnChartGround
                );
        }

        private void OnRemoveVertex(VertexData vertex)
        {
            var dto = dataToObj.Find(v => v.Data == vertex);
            if (dto == null)
            {
                Debug.LogWarning($"【Vertex】データに対応するオブジェクトが見つかりませんでした: {vertex.Position}");
                return;
            }

            dataToObj.Remove(dto);
            dto.Object.Destroy();

            UpdateMesh();
            UpdateColliderObjectScale();
        }

        private void OnClearVertex()
        {
            foreach (var pair in dataToObj)
            {
                pair.Object.Destroy();
            }

            dataToObj.Clear();

            UpdateMesh();
            UpdateColliderObjectScale();
        }

        /// <summary>
        /// センターメッシュの生成
        /// </summary>
        private void GenerateCenterMeshParent()
        {
            GameObject obj = new GameObject("CenterMesh");
            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            centerMeshFilter = obj.AddComponent<MeshFilter>();

            meshRenderer.material = centerMeshMaterial;

            obj.transform.SetParent(vertexObjParent);
            obj.transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// 形が変わった時などメッシュを更新する
        /// </summary>
        private void UpdateMesh()
        {
            // センターメッシュ
            List<Vector3> positions = new List<Vector3>();
            foreach(var pair in dataToObj)
            {
                Vector3 vertexPos = pair.Object.gameObject.transform.localPosition;
                positions.Add(new Vector3(vertexPos.x, vertexPos.y, vertexPos.z));
            }

            if(positions.Count < 3) { return; }

            Mesh centerMesh = MeshGenerator.GenerateMesh(positions);
            centerMeshFilter.mesh = centerMesh;
        }

        private void UpdateColliderObjectScale()
        {
            if(dataToObj.Count == 0) { return; }

            Vector3 rightPos = Vector3.negativeInfinity;
            Vector3 leftPos = Vector3.positiveInfinity;
            foreach (var pair in dataToObj)
            {
                Vector3 vertexPos = pair.Object.gameObject.transform.localPosition;
                if(rightPos.x < vertexPos.x) { rightPos = vertexPos; }
                if(leftPos.x > vertexPos.x) { leftPos = vertexPos; }
            }

            colliderObject.transform.localScale = 
                new Vector3(
                    rightPos.x - leftPos.x, 
                    colliderObject.transform.localScale.y, 
                    colliderObject.transform.localScale.z
                );

            colliderObject.transform.localPosition =
                new Vector3(
                    leftPos.x + (rightPos.x - leftPos.x) / 2f,
                    colliderObject.transform.localPosition.y,
                    colliderObject.transform.localPosition.z
                );
        }

        private Vector2 ConvertPositionOnChartGround(Vector2 normalizedPos)
        {
            float x = Mathf.Lerp(leftLimit, rightLimit, (normalizedPos.x + 1f) / 2f);
            float y = Mathf.Lerp(lowerLimit, upperLimit, (normalizedPos.y + 1f) / 2f);

            return new Vector2(x, y);
        }
    }
}

