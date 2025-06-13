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

        IChartEditorDataGetter chartEditorDataGetter;
        IPointMovableObject movingVertex;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) { StartMoveVertex(); }
            else if (Input.GetMouseButton(0)) { MoveVertex(); } 
            else if (Input.GetMouseButtonUp(0)) { EndMoveVertex(); }
        }

        private void StartMoveVertex()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.VertexMove) { return; }

            var collider = chartEditorDataGetter.GetInteractableCollider<IPointMovableCollider>();
            if (collider == null) { return; }

            IPointMovableObject movableObject = collider.Vertex;
            if (movableObject == null) { return; }

            movableObject.OnMoveStart();
            movingVertex = movableObject;
        }
        
        private void MoveVertex()
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.VertexMove) { return; }
            if (movingVertex == null) { return; }

            // カーソル下の親取得
            var deployable = chartEditorDataGetter.GetInteractableCollider<IPointDeployableCollider>();
            if (deployable == null) { return; }

            // 頂点の移動
            Vector2 normalized = verticesController.WorldPosToNormalizedPos(cursorInteracter.Value.GetWorldPositionUnderCursor());
            movingVertex.Vertex.VertexData.SetPosition(normalized);

            // オブジェクト側の行動
            movingVertex.OnMove();
        }

        private void EndMoveVertex()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.VertexMove) { return; }

            movingVertex?.OnMoveEnd();
            movingVertex = null;
        }
    }
}
