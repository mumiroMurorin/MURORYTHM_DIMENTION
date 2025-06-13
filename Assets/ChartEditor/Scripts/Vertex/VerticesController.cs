using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using VContainer;

namespace ChartEditor
{
    public class VerticesController : MonoBehaviour
    {
        [SerializeField] Transform vertexObjParent;
        [SerializeField] GameObject vertexObject;

        [Space(20)]
        [SerializeField] Transform centerTransform;
        [SerializeField] float leftLimit;
        [SerializeField] float rightLimit;
        [SerializeField] float upperLimit;
        [SerializeField] float lowerLimit;

        public Action OnAddVertexListner;
        public Action OnChangePositionListner;
        public Action OnRemoveVertexListner;
        public Action OnClearVertexListner;

        Dictionary<SpaceHoldVertex, VertexObject> dataToObj = new Dictionary<SpaceHoldVertex, VertexObject>();
        public IReadOnlyDictionary<SpaceHoldVertex, VertexObject> DataToObj => dataToObj;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        CompositeDisposable disposableForBindForVertices = new CompositeDisposable();

        [Inject]
        public void Constructor(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 編集中メッシュオブジェクトが変わったとき
            dataGetter?.EditingVertices
                .Subscribe(note =>
                {
                    DisposeBindForVerticesData();
                    BindForVerticesData(note?.SpaceHoldVertices);
                })
                .AddTo(this.gameObject);
        }

        private void BindForVerticesData(SpaceHoldVertices vertices)
        {
            if(vertices == null) { return; }

            // ObserveCountChanged()は初期化してくれないので、最初に購読
            foreach (var vertex in vertices.Vertices)
            {
                OnAddVertex(vertex);
            }

            // 辺の変更通知に対してスケール更新
            // 追加されたとき
            vertices.Vertices.ObserveAdd()
                .Subscribe(vertex => OnAddVertex(vertex.Value))
                .AddTo(this.gameObject)
                .AddTo(disposableForBindForVertices);
            

            // 削除されたとき
            vertices.Vertices.ObserveRemove()
                .Subscribe(vertex => OnRemoveVertex(vertex.Value))
                .AddTo(this.gameObject)
                .AddTo(disposableForBindForVertices);

            // クリアされたとき
            vertices.Vertices.ObserveReset()
                .Subscribe(_ => OnClearVertex())
                .AddTo(this.gameObject)
                .AddTo(disposableForBindForVertices);
        }

        private void OnAddVertex(SpaceHoldVertex vertex)
        {
            var obj = Instantiate(vertexObject);
            if (!obj.TryGetComponent(out VertexObject vertexObj))
            {
                Debug.LogWarning("【Vertex】オブジェクトにVertexObjectがアタッチされていません");
                return;
            }

            dataToObj.Add(vertex, vertexObj);

            vertexObj.gameObject.transform.SetParent(vertexObjParent);
            vertexObj.gameObject.transform.localPosition = Vector3.zero;
            vertexObj.Initialize(
                vertex,
                () => { OnChangePositionListner?.Invoke(); },
                ConvertPositionOnChartGround
                );

            OnAddVertexListner?.Invoke();
        }

        private void OnRemoveVertex(SpaceHoldVertex vertex)
        {
            if (!dataToObj.TryGetValue(vertex, out VertexObject obj))
            {
                Debug.LogWarning($"【Vertex】データに対応するオブジェクトが見つかりませんでした: {vertex.Position}");
                return;
            }

            dataToObj.Remove(vertex);
            obj.Destroy();

            OnRemoveVertexListner?.Invoke();
        }

        private void OnClearVertex()
        {
            foreach (var pair in dataToObj)
            {
                pair.Value.Destroy();
            }

            dataToObj.Clear();
            OnClearVertexListner?.Invoke();
        }

        private Vector2 ConvertPositionOnChartGround(Vector2 normalizedPos)
        {
            float x = Mathf.Lerp(leftLimit, rightLimit, (normalizedPos.x + 1f) / 2f);
            float y = Mathf.Lerp(lowerLimit, upperLimit, (normalizedPos.y + 1f) / 2f);

            return new Vector2(x, y);
        }

        public Vector2 WorldPosToNormalizedPos(Vector3 worldPos)
        {
            Vector2 deltaPos = worldPos - centerTransform.position;
            return new Vector2(deltaPos.x, deltaPos.y);
        }

        private void DisposeBindForVerticesData()
        {
            if (disposableForBindForVertices != null) { disposableForBindForVertices.Clear(); }
            disposableForBindForVertices = new CompositeDisposable();
        }
    }

}