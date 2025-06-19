using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System.Linq;

namespace ChartEditor
{
    public class VerticesScaler : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        [SerializeField] VerticesController verticesController;
        [SerializeField] MultiVertexSelector multiSelector;

        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;
        List<IPointMovableObject> scalableList = new List<IPointMovableObject>();
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
            if (currentEditMode != EditMode.VerticesScale && currentEditMode != EditMode.VerticesScaling) { return; }

            // カーソル位置が動いたかの判定
            var currentPos = cursorInteracter.Value.GetWorldPositionUnderCursor();
            var delta = currentPos - cursorPos;
            cursorPos = cursorInteracter.Value.GetWorldPositionUnderCursor();

            // 頂点オブジェクトを動かす
            if (Input.GetMouseButton(0) && delta.sqrMagnitude > 0)
            {
                // 初クリック時＆カーソルを動かしたとき動作開始
                if (currentEditMode != EditMode.VerticesScaling)
                {
                    var collider = chartEditorDataGetter.GetInteractableCollider<IPointMovableCollider>();
                    if(collider == null) { return; }

                    StartScaleVertices(collider);
                    chartEditorDataSetter.SetEditMode(EditMode.VerticesScaling);
                }

                ScaleVertices();
            }
            // 頂点オブジェクトの移動終了
            else if (Input.GetMouseButtonUp(0) && currentEditMode == EditMode.VerticesScaling)
            {                
                EndScaleVertices();
                chartEditorDataSetter.SetEditMode(EditMode.VerticesScale);
            }

        }

        /// <summary>
        /// 複数移動開始
        /// </summary>
        private void StartScaleVertices(IPointMovableCollider movableCollider)
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
                scalableList.Add(movable);
            }
        }

        private void ScaleVertices()
        {
            if (scalableList == null || scalableList.Count == 0) { return; }

            // カーソル下の親取得
            var deployable = chartEditorDataGetter.GetInteractableCollider<IPointDeployableCollider>();

            // 配置可能な場所でなければ返す
            if(deployable == null) { return; }

            // 移動量の検知
            Vector3 worldPos = cursorInteracter.Value.GetWorldPositionUnderCursor();
            if (worldPos == Vector3.one * -9999) { return; }

            // 拡大倍率の決定
            Vector2 currentPos = verticesController.WorldPosToNormalizedPos(worldPos);
            float deltaX = Mathf.Abs(currentPos.x - basePos.x);
            float deltaY = Mathf.Abs(currentPos.y - basePos.y);
            float magnitude;
            if (deltaX > deltaY) { magnitude = basePos.x != 0f ? currentPos.x / basePos.x : 0f; }
            else { magnitude = basePos.y != 0f ? currentPos.y / basePos.y : 0f; }

            basePos = currentPos;

            foreach (var scalable in scalableList)
            {
                var pos = scalable.Vertex.VertexData.Position.Value.ScalePoint(centerPos, magnitude);
                scalable.Vertex.VertexData.SetPosition(pos);
                scalable.OnMove();
            }
        }

        private void EndScaleVertices()
        {
            foreach(var scalable in scalableList)
            {
                scalable?.OnMoveEnd();
            }

            scalableList.Clear();
        }
    }
}
