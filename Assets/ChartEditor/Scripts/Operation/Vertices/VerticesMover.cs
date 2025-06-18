using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class VerticesRotator : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        [SerializeField] VerticesController verticesController;
        [SerializeField] MultiVertexSelector multiSelector;

        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;
        Dictionary<IPointMovableObject, Vector2> movableAndDelta = new Dictionary<IPointMovableObject, Vector2>();
        Vector3 cursorPos;

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

            // 頂点オブジェクトを動かす
            if (Input.GetMouseButton(0) && delta.sqrMagnitude > 0)
            {
                // 初クリック時＆カーソルを動かしたとき動作開始
                if (currentEditMode != EditMode.VertexMoving)
                {
                    var collider = chartEditorDataGetter.GetInteractableCollider<IPointMovableCollider>();
                    if(collider == null) { return; }

                    StartMoveVertices(collider);
                    chartEditorDataSetter.SetEditMode(EditMode.VertexMoving);
                }

                MoveVertex();
            }
            // 頂点オブジェクトの移動終了
            else if (Input.GetMouseButtonUp(0) && currentEditMode == EditMode.VertexMoving)
            {                
                EndMoveVertex();
                chartEditorDataSetter.SetEditMode(EditMode.VertexMove);
            }

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
            Vector2 basePos = movableCollider.Vertex.Vertex.VertexData.Position.Value;

            // 複数選択されたオブジェクトから動かせるやつを取り出す
            foreach (var obj in multiSelector.SelectingVertices)
            {
                if (!obj.TryGetComponent(out IPointMovableObject movable)) { continue; }
                movable.OnMoveStart();
                movableAndDelta.TryAdd(movable, obj.VertexData.Position.Value - basePos);
            }
        }

        private void MoveVertex()
        {
            if (movableAndDelta == null || movableAndDelta.Count == 0) { return; }

            // カーソル下の親取得
            var deployable = chartEditorDataGetter.GetInteractableCollider<IPointDeployableCollider>();

            // 配置可能な場所でなければ返す
            if(deployable == null) { return; }

            // 頂点の移動
            Vector3 worldPos = cursorInteracter.Value.GetWorldPositionUnderCursor();
            if (worldPos == Vector3.one * -9999) { return; }

            Vector2 normalized = verticesController.WorldPosToNormalizedPos(worldPos);

            foreach (var pair in movableAndDelta)
            {
                pair.Key.Vertex.VertexData.SetPosition(normalized + pair.Value);
                pair.Key.OnMove();
            }
        }

        private void EndMoveVertex()
        {
            foreach(var pair in movableAndDelta)
            {
                pair.Key?.OnMoveEnd();
            }

            movableAndDelta.Clear();
        }
    }
}
