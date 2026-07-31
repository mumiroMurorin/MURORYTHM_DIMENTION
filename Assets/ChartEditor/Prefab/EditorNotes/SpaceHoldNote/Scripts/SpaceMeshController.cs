using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;
using MeshGenerate;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class SpaceMeshController : MonoBehaviour
    {
        [SerializeField] NoteObject noteObject;
        [SerializeField] GameObject colliderObject;
        [SerializeField] Transform vertexObjParent;
        [SerializeField] GameObject vertexObject;
        [SerializeField] Material centerMeshMaterial;

        [SerializeField] float leftLimit;
        [SerializeField] float rightLimit;
        [SerializeField] float upperLimit;
        [SerializeField] float lowerLimit;

        IVerticesControlableNoteData verticesData;
        MeshFilter centerMeshFilter;
        readonly List<DataToVertexObject> dataToObj = new List<DataToVertexObject>();
        CancellationTokenSource cts = new CancellationTokenSource();

        Vector3 appliedOriginOffset;
        Vector3 vertexObjParentBaseLocalPosition;
        Vector3 colliderObjectBaseLocalPosition;
        bool hasBaseLocalPosition;

        private void Start()
        {
            Initialize();
            Bind(cts.Token).Forget();
        }

        private async UniTask Bind(CancellationToken token)
        {
            await UniTask.WaitUntil(() => noteObject.NoteData != null, cancellationToken: token);
            await UniTask.WaitUntil(() => noteObject.NoteData.Address != null, cancellationToken: token);

            if (noteObject.NoteData is not IVerticesControlableNoteData controlableNoteData) { return; }
            verticesData = controlableNoteData;

            for (int i = 0; i < verticesData.SpaceVertices.Vertices.Count; i++)
            {
                OnAddVertex(verticesData.SpaceVertices.Vertices[i], i);
            }

            verticesData.SpaceVertices.Vertices.ObserveAdd()
                .Subscribe(vertex => OnAddVertex(vertex.Value, vertex.Index))
                .AddTo(this.gameObject);

            verticesData.SpaceVertices.Vertices.ObserveRemove()
                .Subscribe(vertex => OnRemoveVertex(vertex.Value))
                .AddTo(this.gameObject);

            verticesData.SpaceVertices.Vertices.ObserveReset()
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
                Debug.LogWarning("【Vertex】Object does not have VertexObject attached.");
                return;
            }

            dataToObj.Insert(index, new DataToVertexObject(vertex, vertexObj));

            vertexObj.gameObject.transform.SetParent(vertexObjParent);
            vertexObj.gameObject.transform.localPosition = Vector3.zero;
            SetVertexObjectPosition(vertexObj, vertex.Position.Value);
            vertexObj.Initialize(
                vertex,
                RefreshShape,
                ConvertPositionOnChartGround
            );

            RefreshShape(false);
        }

        private void OnRemoveVertex(VertexData vertex)
        {
            var dto = dataToObj.Find(v => v.Data == vertex);
            if (dto == null)
            {
                Debug.LogWarning($"【Vertex】Object matching data was not found: {vertex.Position}");
                return;
            }

            dataToObj.Remove(dto);
            dto.Object.Destroy();

            RefreshShape();
        }

        private void OnClearVertex()
        {
            foreach (var pair in dataToObj)
            {
                pair.Object.Destroy();
            }

            dataToObj.Clear();
            RefreshShape(false);
        }

        private void GenerateCenterMeshParent()
        {
            GameObject obj = new GameObject("CenterMesh");
            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            centerMeshFilter = obj.AddComponent<MeshFilter>();

            meshRenderer.material = centerMeshMaterial;

            obj.transform.SetParent(vertexObjParent);
            obj.transform.localPosition = Vector3.zero;
        }

        public void RefreshVisualOriginFromAddress()
        {
            RefreshVisualOriginFromAddress(Vector3.zero);
        }

        public void RefreshVisualOriginFromAddress(Vector3 extraLocalOffset)
        {
            UpdateVisualOrigin(false, extraLocalOffset);
            UpdateColliderObjectScale();
        }

        private void RefreshShape()
        {
            RefreshShape(true);
        }

        private void RefreshShape(bool preserveCurrentExtraOffset)
        {
            UpdateMesh();
            UpdateVisualOrigin(preserveCurrentExtraOffset);
            UpdateColliderObjectScale();
        }

        private void UpdateMesh()
        {
            List<Vector3> positions = new List<Vector3>();
            foreach (var pair in dataToObj)
            {
                Vector3 vertexPos = pair.Object.gameObject.transform.localPosition;
                positions.Add(new Vector3(vertexPos.x, vertexPos.y, vertexPos.z));
            }

            if (positions.Count < 3) { return; }

            Mesh centerMesh = MeshGenerator.GenerateMesh(positions);
            centerMeshFilter.mesh = centerMesh;
        }

        private void UpdateColliderObjectScale()
        {
            if (dataToObj.Count == 0 || colliderObject == null) { return; }

            EnsureBaseLocalPosition();

            Vector3 rightPos = Vector3.negativeInfinity;
            Vector3 leftPos = Vector3.positiveInfinity;
            foreach (var pair in dataToObj)
            {
                Vector3 vertexPos = pair.Object.gameObject.transform.localPosition;
                if (rightPos.x < vertexPos.x) { rightPos = vertexPos; }
                if (leftPos.x > vertexPos.x) { leftPos = vertexPos; }
            }

            float width = rightPos.x - leftPos.x;
            float centerX = leftPos.x + width / 2f;

            colliderObject.transform.localScale =
                new Vector3(
                    width,
                    colliderObject.transform.localScale.y,
                    colliderObject.transform.localScale.z
                );

            colliderObject.transform.localPosition =
                new Vector3(
                    centerX,
                    colliderObjectBaseLocalPosition.y,
                    colliderObjectBaseLocalPosition.z
                ) - appliedOriginOffset;
        }

        private void UpdateVisualOrigin(bool preserveCurrentExtraOffset)
        {
            UpdateVisualOrigin(preserveCurrentExtraOffset, Vector3.zero);
        }

        private void UpdateVisualOrigin(bool preserveCurrentExtraOffset, Vector3 fallbackExtraOffset)
        {
            EnsureBaseLocalPosition();

            Vector3 originOffset = CalculateVerticesBoundsCenter();
            Vector3 extraOffset = fallbackExtraOffset;

            if (preserveCurrentExtraOffset)
            {
                extraOffset = noteObject.transform.localPosition - appliedOriginOffset;
            }

            noteObject.transform.localPosition = extraOffset + originOffset;
            vertexObjParent.localPosition = vertexObjParentBaseLocalPosition - originOffset;
            appliedOriginOffset = originOffset;
        }

        private Vector3 CalculateVerticesBoundsCenter()
        {
            if (dataToObj.Count == 0) { return Vector3.zero; }

            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            foreach (var pair in dataToObj)
            {
                Vector3 vertexPos = pair.Object.gameObject.transform.localPosition;
                min = Vector3.Min(min, vertexPos);
                max = Vector3.Max(max, vertexPos);
            }

            Vector3 center = (min + max) / 2f;
            center.z = 0f;
            return center;
        }

        private void SetVertexObjectPosition(VertexObject vertexObj, Vector2 normalizedPos)
        {
            Vector2 converted = ConvertPositionOnChartGround(normalizedPos);
            vertexObj.transform.localPosition = new Vector3(converted.x, converted.y, vertexObj.transform.localPosition.z);
        }

        private void EnsureBaseLocalPosition()
        {
            if (hasBaseLocalPosition) { return; }

            vertexObjParentBaseLocalPosition = vertexObjParent != null ? vertexObjParent.localPosition : Vector3.zero;
            colliderObjectBaseLocalPosition = colliderObject != null ? colliderObject.transform.localPosition : Vector3.zero;
            hasBaseLocalPosition = true;
        }

        private Vector2 ConvertPositionOnChartGround(Vector2 normalizedPos)
        {
            float x = Mathf.Lerp(leftLimit, rightLimit, (normalizedPos.x + 1f) / 2f);
            float y = Mathf.Lerp(lowerLimit, upperLimit, (normalizedPos.y + 1f) / 2f);

            return new Vector2(x, y);
        }

        private void OnDestroy()
        {
            cts?.CancelAndDispose();
        }
    }
}
