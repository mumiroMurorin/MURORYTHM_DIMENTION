using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using VContainer;

namespace ChartEditor
{
    public class VerticesMover : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        [SerializeField] VerticesController verticesController;
        [SerializeField] MultiVertexSelector multiSelector;

        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;
        Dictionary<IPointMovableObject, Vector2> rotatableAndDelta = new Dictionary<IPointMovableObject, Vector2>();
        Vector2 centerPos;
        Vector2 basePos;
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
            if (currentEditMode != EditMode.VerticesRotate && currentEditMode != EditMode.VerticesRotating) { return; }

            // カーソル位置が動いたかの判定
            var currentPos = cursorInteracter.Value.GetWorldPositionUnderCursor();
            var delta = currentPos - cursorPos;
            cursorPos = cursorInteracter.Value.GetWorldPositionUnderCursor();

            // 頂点オブジェクトを動かす
            if (Input.GetMouseButton(0) && delta.sqrMagnitude > 0)
            {
                // 初クリック時＆カーソルを動かしたとき動作開始
                if (currentEditMode != EditMode.VerticesRotating)
                {
                    var collider = chartEditorDataGetter.GetInteractableCollider<IPointMovableCollider>();
                    if(collider == null) { return; }

                    StartRotateVertices(collider);
                    chartEditorDataSetter.SetEditMode(EditMode.VerticesRotating);
                }

                RotateVertices();
            }
            // 頂点オブジェクトの移動終了
            else if (Input.GetMouseButtonUp(0) && currentEditMode == EditMode.VerticesRotating)
            {                
                EndRotateVertices();
                chartEditorDataSetter.SetEditMode(EditMode.VerticesRotate);
            }

        }

        /// <summary>
        /// 複数移動開始
        /// </summary>
        private void StartRotateVertices(IPointMovableCollider movableCollider)
        {
            if (movableCollider == null) { return; }

            // 選択された点群の中心を導出
            centerPos = multiSelector.SelectingVertices.Select(x => x.VertexData.Position.Value).ToArray().Center();

            // 基準となる点の座標(正規化済み)を保存
            basePos = movableCollider.Vertex.Vertex.VertexData.Position.Value;

            // 複数選択されたオブジェクトから動かせるやつを取り出す
            foreach (var obj in multiSelector.SelectingVertices)
            {
                if (!obj.TryGetComponent(out IPointMovableObject movable)) { continue; }
                movable.OnMoveStart();
                rotatableAndDelta.TryAdd(movable, obj.VertexData.Position.Value - centerPos);
            }
        }

        private void RotateVertices()
        {
            if (rotatableAndDelta == null || rotatableAndDelta.Count == 0) { return; }

            // カーソル下の親取得
            var deployable = chartEditorDataGetter.GetInteractableCollider<IPointDeployableCollider>();

            // 配置可能な場所でなければ返す
            if(deployable == null) { return; }

            // カーソル位置の取得、角度の計算
            Vector3 worldPos = cursorInteracter.Value.GetWorldPositionUnderCursor();
            if (worldPos == Vector3.one * -9999) { return; }

            Vector2 currentPos = verticesController.WorldPosToNormalizedPos(worldPos);
            var angle = Vector2Extensions.AngleBetweenVectors(basePos, currentPos, centerPos);

            // 回転
            foreach (var pair in rotatableAndDelta)
            {
                var pos = Vector2Extensions.RotatePoint(pair.Key.Vertex.VertexData.Position.Value, centerPos, angle);
                pair.Key.Vertex.VertexData.SetPosition(pos);
                pair.Key.OnMove();
            }
        }

        private void EndRotateVertices()
        {
            foreach(var pair in rotatableAndDelta)
            {
                pair.Key?.OnMoveEnd();
            }

            rotatableAndDelta.Clear();
        }
    }
}
