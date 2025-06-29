using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System.Linq;
using static UndoRedo.History;

namespace ChartEditor
{
    public class VerticesRotator : MonoBehaviour
    {
        [SerializeField] float firstMovingThreshold = 0.15f;
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        [SerializeField] VerticesController verticesController;
        [SerializeField] MultiVertexSelector multiSelector;

        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;
        Dictionary<IPointMovableObject, Vector2> movableAndDelta = new Dictionary<IPointMovableObject, Vector2>();
        float magnitudeSum;
        Vector3 cursorPos;
        Vector2 basePos;
        Vector2 lastPos;
        IPointMovableCollider targetCollider;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter, IChartEditorDataSetter chartEditorDataSetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
            this.chartEditorDataSetter = chartEditorDataSetter;
        }

        private void Start()
        {
            cursorPos = cursorInteracter.Value.GetWorldPositionUnderCursor();
        }

        void Update()
        {
            var currentEditMode = chartEditorDataGetter.CurrentEditMode.Value;
            if (currentEditMode != EditMode.VertexMove && currentEditMode != EditMode.VertexMoving) { return; }

            // カーソル位置が動いたかの判定
            var currentPos = cursorInteracter.Value.GetWorldPositionUnderCursor();
            var delta = currentPos - cursorPos;
            cursorPos = cursorInteracter.Value.GetWorldPositionUnderCursor();

            if (Input.GetMouseButtonDown(0))
            {
                Initialize();
            }

            // 頂点オブジェクトを動かす
            if (Input.GetMouseButton(0) && delta.magnitude > 0)
            {
                // 初動、誤動作防止のためある程度カーソルが動くまで待つ
                if (magnitudeSum < firstMovingThreshold)
                {
                    if (targetCollider == null) { targetCollider = chartEditorDataGetter.GetInteractableCollider<IPointMovableCollider>(); }
                    magnitudeSum += delta.magnitude;
                    return;
                }

                // 初クリック時＆カーソルを動かしたとき動作開始
                if (currentEditMode != EditMode.VertexMoving)
                {
                    if (targetCollider == null) { return; }

                    StartMoveVertices(targetCollider);
                    chartEditorDataSetter.SetEditMode(EditMode.VertexMoving);
                }

                // 配置可能な場所でなければ返す
                var deployable = chartEditorDataGetter.GetInteractableCollider<IPointDeployableCollider>();
                if (deployable == null) { return; }

                MoveVertex();
            }
            // 頂点オブジェクトの移動終了
            else if (Input.GetMouseButtonUp(0) && currentEditMode == EditMode.VertexMoving)
            {                
                EndMoveVertex();
                chartEditorDataSetter.SetEditMode(EditMode.VertexMove);
            }
        }

        private void Initialize()
        {
            movableAndDelta.Clear();
            magnitudeSum = 0;
            basePos = Vector2.zero;
            targetCollider = null;
        }

        /// <summary>
        /// 複数移動開始
        /// </summary>
        private void StartMoveVertices(IPointMovableCollider movableCollider)
        {
            if (movableCollider == null) { return; }

            // 基準となる頂点オブジェクトを先に登録
            if (!movableCollider.Vertex.Vertex.TryGetComponent(out IPointMovableObject baseVertex)) { return; }
            movableAndDelta.Add(baseVertex, Vector2.zero);

            // 基準となる点の座標(正規化済み)を保存
            basePos = movableCollider.Vertex.Vertex.VertexData.Position.Value;

            // 複数選択されたオブジェクトから動かせるやつを取り出す
            foreach (var data in multiSelector.SelectingVertices)
            {
                var obj = verticesController.DataToObj.GetObject(data);
                if (!obj.TryGetComponent(out IPointMovableObject movable)) { continue; }

                movable.OnMoveStart();
                movableAndDelta.TryAdd(movable, obj.VertexData.Position.Value - basePos);
            }
        }

        /// <summary>
        /// 移動中はカーソル位置を取得しつつ位置を更新する
        /// </summary>
        private void MoveVertex()
        {
            if (movableAndDelta == null || movableAndDelta.Count == 0) { return; }

            // 頂点の移動
            if (cursorPos == Vector3.one * -9999) { return; }

            lastPos = verticesController.WorldPosToNormalizedPos(cursorPos);
            foreach (var pair in movableAndDelta)
            {
                pair.Key.Vertex.VertexData.SetPosition(lastPos + pair.Value);
                pair.Key.OnMove();
            }
        }

        private void MoveVertex(VertexData data, Vector2 toPos)
        {
            //var obj = verticesController.DataToObj.GetObject(data);
            // if (!obj.TryGetComponent(out IPointMovableObject movable)) { }

            data.SetPosition(toPos);
        }

        /// <summary>
        /// 移動終了
        /// </summary>
        private void EndMoveVertex()
        {
            foreach(var pair in movableAndDelta)
            {
                pair.Key?.OnMoveEnd();
            }

            // 頂点の動きを登録
            var movableAndDeltaCopy = new Dictionary<IPointMovableObject, Vector2>(movableAndDelta);
            Vector2 basePosCopy = basePos;
            Vector2 lastPosCopy = lastPos;
            Record(() =>
            // もう一度移動
            {
                foreach (var pair in movableAndDeltaCopy)
                {
                    MoveVertex(pair.Key.Vertex.VertexData, pair.Value + lastPosCopy);
                }
            }, () =>
            // 戻す
            {
                foreach (var pair in movableAndDeltaCopy)
                {
                    MoveVertex(pair.Key.Vertex.VertexData, pair.Value + basePosCopy);
                }
            });

            Initialize();
        }


    }
}
