using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class VertexMover : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        [SerializeField] VerticesController verticesController;
        [SerializeField] MultiVertexSelector multiSelector;

        IChartEditorDataGetter chartEditorDataGetter;
        Dictionary<IPointMovableObject, Vector2> mobableAndDelta = new Dictionary<IPointMovableObject, Vector2>();

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        void Update()
        {
            // 複数選択されていない場合
            if(multiSelector.SelectingVertices.Count == 0)
            {
                if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftControl)) { StartMoveVertex(); }
                else if (Input.GetMouseButton(0)) { MoveVertex(); }
                else if (Input.GetMouseButtonUp(0)) { EndMoveVertex(); }
            }
            // 複数選択されている場合
            else if (multiSelector.SelectingVertices.Count > 0)
            {
                if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftControl)) { StartMoveVertices(); }
                else if (Input.GetMouseButton(0)) { MoveVertex(); }
                else if (Input.GetMouseButtonUp(0)) { EndMoveVertex(); }
            }
        }

        /// <summary>
        /// 単独移動開始
        /// </summary>
        private void StartMoveVertex()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.VertexMove) { return; }

            var collider = chartEditorDataGetter.GetInteractableCollider<IPointMovableCollider>();
            if (collider == null) { return; }

            IPointMovableObject movableObject = collider.Vertex;
            if (movableObject == null) { return; }

            movableObject.OnMoveStart();
            mobableAndDelta = new Dictionary<IPointMovableObject, Vector2> { [movableObject] = Vector2.zero };
        }

        /// <summary>
        /// 複数移動開始
        /// </summary>
        private void StartMoveVertices()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.VertexMove) { return; }

            var collider = chartEditorDataGetter.GetInteractableCollider<IPointMovableCollider>();
            if (collider == null) { return; }

            // 基準となる頂点オブジェクトを先に登録
            if (!collider.Vertex.Vertex.TryGetComponent(out IPointMovableObject baseVertex)) { return; }
            mobableAndDelta.Add(baseVertex, Vector2.zero);

            // 基準となる点の座標(正規化済み)を保存
            Vector2 basePos = collider.Vertex.Vertex.VertexData.Position.Value;

            // 複数選択されたオブジェクトから動かせるやつを取り出す
            foreach (var obj in multiSelector.SelectingVertices)
            {
                if (!obj.TryGetComponent(out IPointMovableObject movable)) { continue; }
                movable.OnMoveStart();
                mobableAndDelta.TryAdd(movable, obj.VertexData.Position.Value - basePos);
            }
        }

        private void MoveVertex()
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.VertexMove) { return; }
            if (mobableAndDelta == null || mobableAndDelta.Count == 0) { return; }

            // カーソル下の親取得
            var deployable = chartEditorDataGetter.GetInteractableCollider<IPointDeployableCollider>();
            if (deployable == null) { return; }

            // 頂点の移動
            Vector2 normalized = verticesController.WorldPosToNormalizedPos(cursorInteracter.Value.GetWorldPositionUnderCursor());

            foreach(var pair in mobableAndDelta)
            {
                pair.Key.Vertex.VertexData.SetPosition(normalized + pair.Value);
                pair.Key.OnMove();
            }
        }

        private void EndMoveVertex()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.VertexMove) { return; }

            foreach(var pair in mobableAndDelta)
            {
                pair.Key?.OnMoveEnd();
            }

            mobableAndDelta.Clear();
        }
    }
}
