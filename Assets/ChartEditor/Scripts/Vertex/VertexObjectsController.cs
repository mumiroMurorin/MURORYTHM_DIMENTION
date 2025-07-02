using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using VContainer;
using static UndoRedo.History;

namespace ChartEditor
{
    public class VertexObjectsController : MonoBehaviour
    {
        [SerializeField] Transform vertexObjParent;
        [SerializeField] GameObject vertexObject;
        [SerializeField] GameObject[] laneDivisionLines;

        [Space(20)]
        [SerializeField] Transform centerTransform;
        [SerializeField] float leftLimit;
        [SerializeField] float rightLimit;
        [SerializeField] float upperLimit;
        [SerializeField] float lowerLimit;

        [Space(20)]
        [SerializeField] Color startColor;
        [SerializeField] Color endColor;

        public Action OnAddVertexListner { get; set; }
        public Action OnChangePositionListner { get; set; }
        public Action OnRemoveVertexListner { get; set; }
        public Action OnClearVertexListner { get; set; }

        DataToVertexObjectList dataToObj = new DataToVertexObjectList();
        public DataToVertexObjectList DataToObj => dataToObj;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        IChartEditorOptionGetter optionGetter;
        CompositeDisposable disposableForBindForVertices = new CompositeDisposable();

        [Inject]
        public void Constructor(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, IChartEditorOptionGetter optionGetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            this.optionGetter = optionGetter;
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
                    ResetVerticesPreview();
                    BindForVerticesData(note?.SpaceHoldVertices);
                })
                .AddTo(this.gameObject);

            // レーン分割線の表示
            optionGetter?.LaneDivisionNum
                .Subscribe(SetLaneDivisionLine)
                .AddTo(this.gameObject);
        }

        private void BindForVerticesData(SpaceHoldVertices vertices)
        {
            if(vertices == null) { return; }

            // ObserveCountChanged()は初期化してくれないので、最初に購読
            for (int i = 0; i < vertices.Vertices.Count; i++) 
            {
                var vertex = vertices.Vertices[i];
                OnAddVertex(vertex, i);
            }

            // 頂点の変更通知に対してスケール更新
            // 追加されたとき
            vertices.Vertices.ObserveAdd()
                .Subscribe(vertex => OnAddVertex(vertex.Value, vertex.Index))
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

        private void OnAddVertex(VertexData vertex, int index)
        {
            var obj = Instantiate(vertexObject);
            if (!obj.TryGetComponent(out VertexObject vertexObj))
            {
                Debug.LogWarning("【Vertex】オブジェクトにVertexObjectがアタッチされていません");
                return;
            }

            dataToObj.List.Insert(index, new DataToVertexObject(vertex, vertexObj));
            
            vertexObj.gameObject.transform.SetParent(vertexObjParent);
            vertexObj.gameObject.transform.localPosition = Vector3.zero;
            vertexObj.Initialize(
                vertex,
                () => { OnChangePositionListner?.Invoke(); },
                ConvertPositionOnChartGround
                );

            OnAddVertexListner?.Invoke();
            UpdateVertexColor();
        }

        private void OnRemoveVertex(VertexData vertex)
        {
            var dto = dataToObj.List.Find(v => v.Data == vertex);
            if (dto == null)
            {
                Debug.LogWarning($"【Vertex】データに対応するオブジェクトが見つかりませんでした: {vertex.Position}");
                return;
            }

            dataToObj.List.Remove(dto);
            dto.Object.Destroy();

            OnRemoveVertexListner?.Invoke();
            UpdateVertexColor();
        }

        private void OnClearVertex()
        {
            dataToObj.Clear();
            OnClearVertexListner?.Invoke();
        }

        /// <summary>
        /// 頂点オブジェクトのインデックス順色を更新する
        /// </summary>
        private void UpdateVertexColor()
        {
            if(dataToObj.List.Count <= 1) { return; }

            for (int i = 0; i < dataToObj.List.Count; i++)
            {
                Color color = Color.Lerp(startColor, endColor, (float)i / (dataToObj.List.Count - 1));
                var vertexObj = dataToObj.List[i].Object;

                vertexObj.SetColor(color);
            }
        }

        /// <summary>
        /// 正規化された数値 → チャートエディタ上のWorldPos
        /// </summary>
        /// <param name="normalizedPos"></param>
        /// <returns></returns>
        private Vector2 ConvertPositionOnChartGround(Vector2 normalizedPos)
        {
            float x = Mathf.Lerp(leftLimit, rightLimit, (normalizedPos.x + 1f) / 2f);
            float y = Mathf.Lerp(lowerLimit, upperLimit, (normalizedPos.y + 1f) / 2f);

            return new Vector2(x, y);
        }

        /// <summary>
        /// チャートエディタ上のWorldPos → 正規化された数値
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public Vector2 WorldPosToNormalizedPos(Vector3 worldPos)
        {
            Vector2 deltaPos = worldPos - centerTransform.position;
            return new Vector2(deltaPos.x / (Mathf.Abs(leftLimit - rightLimit) / 2f), deltaPos.y / (Mathf.Abs(upperLimit - lowerLimit) / 2f));
        }

        /// <summary>
        /// 頂点オブジェクトとデータの削除、クリア
        /// </summary>
        private void ResetVerticesPreview()
        {
            DisposeBindForVerticesData();

            dataToObj.Clear();
        }

        /// <summary>
        /// 頂点データに購読されたコールバックを捨てる
        /// </summary>
        private void DisposeBindForVerticesData()
        {
            if (disposableForBindForVertices != null) { disposableForBindForVertices.Clear(); }
            disposableForBindForVertices = new CompositeDisposable();
        }

        /// <summary>
        /// 分割線の表示非表示
        /// </summary>
        /// <param name="divNum"></param>
        private void SetLaneDivisionLine(int divNum)
        {
            if (laneDivisionLines == null) { return; }
            if (laneDivisionLines.Length != 17) { return; }

            for (int i = 0; i < 16; i++)
            {
                laneDivisionLines[i].SetActive(i % (16 / divNum) == 0);
            }

            laneDivisionLines[16].SetActive(true);
        }
    }

    public class DataToVertexObjectList
    {
        List<DataToVertexObject> list = new List<DataToVertexObject>();

        public List<DataToVertexObject> List { get { return list; } }

        public VertexObject GetObject(VertexData data) { return list.Find(x => x.Data == data)?.Object; }

        public void Clear()
        {
            foreach (var pair in list)
            {
                pair.Object.Destroy();
            }

            list.Clear();
        }
    }

    public class DataToVertexObject
    {
        public DataToVertexObject(VertexData data, VertexObject obj)
        {
            this.Data = data;
            this.Object = obj;
        }

        public VertexData Data { get; set; }
        public VertexObject Object { get; set; }
    }
}