using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System.Linq;

namespace ChartEditor
{
    public class VerticesScaler : MonoBehaviour
    {
        [SerializeField] float firstMovingThreshold = 0.15f;
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        [SerializeField] VerticesController verticesController;
        [SerializeField] MultiVertexSelector multiSelector;

        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;
        Dictionary<IPointMovableObject, Vector2> scalableAndDelta = new Dictionary<IPointMovableObject, Vector2>();
        float magnitudeSum;
        Vector2 centerPos;
        Vector2 basePos;
        Vector3 cursorPos;
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
            if (currentEditMode != EditMode.VerticesScale && currentEditMode != EditMode.VerticesScaling) { return; }

            // カーソル位置が動いたかの判定
            var currentPos = cursorInteracter.Value.GetWorldPositionUnderCursor();
            var delta = currentPos - cursorPos;
            cursorPos = cursorInteracter.Value.GetWorldPositionUnderCursor();

            // 頂点オブジェクトを動かす
            if (Input.GetMouseButton(0) && delta.sqrMagnitude > 0)
            {
                // 初動
                if (magnitudeSum < firstMovingThreshold)
                {
                    targetCollider = chartEditorDataGetter.GetInteractableCollider<IPointMovableCollider>();
                    magnitudeSum += delta.magnitude;
                    return;
                }

                // 初クリック時＆カーソルを動かしたとき動作開始
                if (currentEditMode != EditMode.VerticesScaling)
                {
                    if(targetCollider == null) { return; }

                    StartScaleVertices(targetCollider);
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
                scalableAndDelta.TryAdd(movable, obj.VertexData.Position.Value);
                movable.OnMoveStart();
            }
        }

        private void ScaleVertices()
        {
            if (scalableAndDelta == null || scalableAndDelta.Count == 0) { return; }

            // カーソル下の親取得
            var deployable = chartEditorDataGetter.GetInteractableCollider<IPointDeployableCollider>();

            // 配置可能な場所でなければ返す
            if(deployable == null) { return; }

            // 移動量の検知
            Vector3 worldPos = cursorInteracter.Value.GetWorldPositionUnderCursor();
            if (worldPos == Vector3.one * -9999) { return; }

            // 拡大倍率の決定
            Vector2 currentPos = verticesController.WorldPosToNormalizedPos(worldPos);
            float magnitude = (basePos - centerPos).sqrMagnitude != 0 ? (currentPos - centerPos).magnitude / (basePos - centerPos).magnitude : 0f;

            foreach (var pair in scalableAndDelta)
            {
                var pos = pair.Value.ScalePoint(centerPos, magnitude);
                pair.Key.Vertex.VertexData.SetPosition(pos);
                pair.Key.OnMove();
            }
        }

        private void EndScaleVertices()
        {
            foreach (var scalable in scalableAndDelta)
            {
                scalable.Key?.OnMoveEnd();
            }

            scalableAndDelta.Clear();
            magnitudeSum = 0;
            targetCollider = null;

        }
    }
}
