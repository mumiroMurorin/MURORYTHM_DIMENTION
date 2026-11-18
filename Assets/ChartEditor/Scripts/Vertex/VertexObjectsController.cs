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

        [Header("中心点 (掴んで動かすと多角形全体が動く)")]
        [SerializeField] Color centerHandleColor = Color.white;
        [Tooltip("頂点に対する中心点の大きさの倍率")]
        [SerializeField] float centerHandleScale = 1.4f;

        // 中心点と、その対象の頂点リスト
        VertexObject centerHandle;
        SpaceVertices centerHandleVertices;

        ReactiveProperty<DeploymentNoteType> editingNoteType = new();
        public IObservable<DeploymentNoteType> EditingNoteType => editingNoteType;

        Subject<Unit> onAddVertex = new();
        public IObservable<Unit> OnAddVertexListener => onAddVertex;

        Subject<Unit> onChangePosition = new();
        public IObservable<Unit> OnChangePositionListener => onChangePosition;

        Subject<Unit> onRemoveVertex = new();
        public IObservable<Unit> OnRemoveVertexListener => onRemoveVertex;

        Subject<Unit> onClearVertex = new();
        public IObservable<Unit> OnClearVertexListener => onClearVertex;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        INotesDataGetter notesGetter;
        INotesDataSetter notesSetter;
        IChartEditorOptionGetter optionGetter;
        CompositeDisposable disposableForBindForVertices = new CompositeDisposable();

        [Inject]
        public void Constructor(IChartEditorOptionGetter optionGetter, INotesDataGetter notesGetter, INotesDataSetter notesSetter, IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter)
        {
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
            this.optionGetter = optionGetter;
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        // 前後の節の形を薄く重ねて出す (オニオンスキン)
        VerticesOnionSkin onionSkin;

        private void Start()
        {
            onionSkin = new VerticesOnionSkin(vertexObjParent, ConvertPositionOnChartGround, rightLimit - leftLimit);
            Bind();
        }

        private void OnDestroy()
        {
            onionSkin?.Dispose();
            onionSkin = null;
        }

        private void Bind()
        {
            // 編集レイヤーが変更された時、モードを変更する
            dataGetter?.EditNoteType
                .Subscribe(type => {
                    switch (type)
                    {
                        // 頂点のレイヤーに入ったときは、編集ツール (配置・移動・選択) にする
                        case EditNoteType.Vertices:
                            dataSetter.SetEditMode(EditMode.VertexEdit);
                            break;
                    }
                })
                .AddTo(this.gameObject);

            // 編集中メッシュオブジェクトが変わったとき
            notesGetter?.EditingVertices
                .Subscribe(note =>
                {
                    ResetVerticesPreview();
                    BindForVerticesData(note?.SpaceVertices);
                    RebuildCenterHandle(note?.SpaceVertices);

                    if (note is IDeployableNoteData deployable)
                    {
                        var noteType = deployable.NoteType;
                        editingNoteType.Value = noteType;
                    }
                })
                .AddTo(this.gameObject);

            // レーン分割線の表示
            optionGetter?.LaneDivisionNum
                .Subscribe(SetLaneDivisionLine)
                .AddTo(this.gameObject);
        }

        private void BindForVerticesData(SpaceVertices vertices)
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

        #region 中心点 (多角形全体を動かすためのハンドル)

        /// <summary>
        /// 中心点を作り直す。編集対象が無いときは消すだけ
        /// </summary>
        private void RebuildCenterHandle(SpaceVertices vertices)
        {
            if (centerHandle != null)
            {
                centerHandle.Destroy();
                centerHandle = null;
            }

            centerHandleVertices = vertices;
            if (vertices == null) { return; }

            // 頂点と同じプレハブから作る (掴んで動かす仕組みをそのまま使うため)
            var obj = Instantiate(vertexObject);
            if (!obj.TryGetComponent(out VertexObject vertexObj))
            {
                Destroy(obj);
                return;
            }

            // 多角形の頂点ではないので、選択の対象から外す
            foreach (var selectableCollider in obj.GetComponentsInChildren<SelectableVertexCollider>(true))
            {
                Destroy(selectableCollider);
            }

            obj.name = "VertexCenterHandle";
            obj.transform.SetParent(vertexObjParent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale *= centerHandleScale;

            vertexObj.Initialize(new VertexData(CalcCenter(vertices)), null, ConvertPositionOnChartGround);
            vertexObj.SetAsCenterHandle();
            vertexObj.SetColor(centerHandleColor);
            vertexObj.SetOutlineCamera(EditorViewCameras.VerticesView);

            centerHandle = vertexObj;
        }

        private void LateUpdate()
        {
            // 前後の節の形 (オニオンスキン) を、今の頂点の位置に合わせて描き直す
            if (onionSkin != null && notesGetter != null && dataGetter != null && optionGetter != null)
            {
                onionSkin.Refresh(
                    notesGetter,
                    notesGetter.EditingVertices.Value,
                    optionGetter.OnionSkinCount.Value,
                    dataGetter.EditNoteType.Value == EditNoteType.Vertices);
            }

            // 頂点が動いたら、中心点も追従させる (頂点を1個だけ動かしたとき・追加や削除をしたときも含む)
            if (centerHandle == null || centerHandleVertices == null) { return; }

            centerHandle.VertexData.SetPosition(CalcCenter(centerHandleVertices));
        }

        /// <summary>
        /// 中心点の位置 = 形の重心 (面積の中心。3D編集の「節の中心」と同じ: SpaceHoldSections.GetCenter)
        /// (中心点の位置を数値で決める窓も、この位置を出す: VertexCenterPanel)
        ///
        /// 以前は頂点の平均だった。頂点の平均は、辺の途中に頂点を足すと、そちらへずれる (同じ形でも、3D編集の中心と値が合わなかった)。
        /// 面積がほとんど無い形・辺が交わっている形は、頂点の平均になる (GetCenter)。
        /// </summary>
        public static Vector2 CalcCenter(SpaceVertices vertices)
        {
            if (vertices == null || vertices.Vertices.Count == 0) { return Vector2.zero; }

            return SpaceHoldSections.GetCenter(SpaceHoldSections.GetVertices(vertices));
        }

        #endregion

        private void OnAddVertex(VertexData vertex, int index)
        {
            var obj = Instantiate(vertexObject);
            if (!obj.TryGetComponent(out VertexObject vertexObj))
            {
                Debug.LogWarning("【Vertex】オブジェクトにVertexObjectがアタッチされていません");
                return;
            }

            notesSetter.InsertVertex(index, new DataToVertexObject(vertex, vertexObj));
            
            vertexObj.gameObject.transform.SetParent(vertexObjParent);
            // すぐに、頂点の位置に置く (位置を追いかけるのは、次のフレームから (VertexMovable)。それまで原点にあると、
            // そのフレームに作る形のメッシュが、1点に重なった頂点からになり、「三角形に分けられない形」の警告が出る)
            Vector2 placed = ConvertPositionOnChartGround(vertex.Position.Value);
            vertexObj.gameObject.transform.localPosition = new Vector3(placed.x, placed.y, 0f);
            vertexObj.Initialize(
                vertex,
                () => { onChangePosition?.OnNext(Unit.Default); },
                ConvertPositionOnChartGround
                );

            // アウトラインは頂点の編集画面を映しているカメラにだけ描く
            vertexObj.SetOutlineCamera(EditorViewCameras.VerticesView);

            onAddVertex?.OnNext(Unit.Default);
            UpdateVertexColor();
        }

        private void OnRemoveVertex(VertexData vertex)
        {
            var obj = notesGetter.GetVertexObject(vertex);
            
            if (!notesSetter.RemoveVertexDataToObject(vertex)) { return; }
            if (obj == null) { return; }

            // オブジェクトの削除
            if (obj != null && obj.gameObject.TryGetComponent(out IDestroyableVertex destroyable)) { destroyable.OnDestroy(); }

            onRemoveVertex?.OnNext(Unit.Default);
            UpdateVertexColor();
        }

        private void OnClearVertex()
        {
            notesSetter.ClearDataToVertexObjectList();
            onRemoveVertex?.OnNext(Unit.Default);
        }

        /// <summary>
        /// 頂点オブジェクトのインデックス順色を更新する
        /// </summary>
        private void UpdateVertexColor()
        {
            int dtoCount = notesGetter.DataToVertexObject.Count;
            if (dtoCount <= 1) { return; }

            for (int i = 0; i < dtoCount; i++)
            {
                Color color = Color.Lerp(startColor, endColor, (float)i / (dtoCount - 1));
                var vertexObj = notesGetter.GetVertexObject(i);

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

            notesSetter.ClearDataToVertexObjectList();
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
}