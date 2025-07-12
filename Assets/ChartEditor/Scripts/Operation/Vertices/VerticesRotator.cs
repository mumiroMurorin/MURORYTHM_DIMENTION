using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using VContainer;
using static UndoRedo.Vertices.VerticesMoveRecord;

namespace ChartEditor
{
    public class VerticesMover : MonoBehaviour
    {
        [SerializeField] float firstMovingThreshold = 0.15f;
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        [SerializeField] VertexObjectsController verticesController;
        [SerializeField] MultiVertexSelector multiSelector;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        INotesDataGetter notesGetter;

        Dictionary<IPointMovableObject, Vector2> movableAndPos = new Dictionary<IPointMovableObject, Vector2>();
        List<VertexDataToPos> previousPos;
        float magnitudeSum;
        float rotateAngle;
        Vector2 centerPos;
        Vector2 basePos;
        Vector3 cursorPos;
        IPointMovableCollider targetCollider;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, INotesDataGetter notesGetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            this.notesGetter = notesGetter;
        }

        private void Start()
        {
            cursorPos = cursorInteracter.Value.GetWorldPositionUnderCursor();
        }

        void Update()
        {
            var currentEditMode = dataGetter.CurrentEditMode.Value;
            if (currentEditMode != EditMode.VerticesRotate && currentEditMode != EditMode.VerticesRotating) { return; }

            // カーソル位置が動いたかの判定
            var currentPos = cursorInteracter.Value.GetWorldPositionUnderCursor();
            var delta = currentPos - cursorPos;
            cursorPos = cursorInteracter.Value.GetWorldPositionUnderCursor();

            if (Input.GetMouseButtonDown(0))
            {
                Initialize();
            }

            // 頂点オブジェクトを動かす
            if (Input.GetMouseButton(0) && delta.sqrMagnitude > 0)
            {
                // 初動
                if (magnitudeSum < firstMovingThreshold)
                {
                    if (targetCollider == null) { targetCollider = dataGetter.GetInteractableCollider<IPointMovableCollider>(); }
                    magnitudeSum += delta.magnitude;
                    return;
                }

                // 初クリック時＆カーソルを動かしたとき動作開始
                if (currentEditMode != EditMode.VerticesRotating)
                {
                    if (targetCollider == null) { return; }

                    StartRotateVertices(targetCollider);
                    dataSetter.SetEditMode(EditMode.VerticesRotating);
                }

                RotateVertices();
            }
            // 頂点オブジェクトの移動終了
            else if (Input.GetMouseButtonUp(0) && currentEditMode == EditMode.VerticesRotating)
            {                
                EndRotateVertices();
                dataSetter.SetEditMode(EditMode.VerticesRotate);
            }
        }

        /// <summary>
        /// 複数移動開始
        /// </summary>
        private void StartRotateVertices(IPointMovableCollider movableCollider)
        {
            if (movableCollider == null) { return; }

            // 選択された点群の中心を導出
            centerPos = multiSelector.SelectingVertices.Select(x => x.Position.Value).ToArray().Center();

            // 基準となる点の座標(正規化済み)を保存
            basePos = movableCollider.Vertex.Vertex.VertexData.Position.Value;

            previousPos = new List<VertexDataToPos>();

            // 複数選択されたオブジェクトから動かせるやつを取り出す
            foreach (var data in multiSelector.SelectingVertices)
            {
                var obj = notesGetter.GetVertexObject(data);
                if (!obj.TryGetComponent(out IPointMovableObject movable)) { continue; }

                movable.OnMoveStart();
                movableAndPos.Add(movable, movable.Vertex.VertexData.Position.Value);

                previousPos.Add(new VertexDataToPos(data, data.Position.Value));
            }
        }

        /// <summary>
        /// 回転行列をもとに移動
        /// </summary>
        private void RotateVertices()
        {
            if (movableAndPos == null || movableAndPos.Count == 0) { return; }

            // カーソル下の親取得
            var deployable = dataGetter.GetInteractableCollider<IPointDeployableCollider>();

            // 配置可能な場所でなければ返す
            if(deployable == null) { return; }

            // カーソル位置の取得、角度の計算
            if (cursorPos == Vector3.one * -9999) { return; }

            Vector2 currentPos = verticesController.WorldPosToNormalizedPos(cursorPos);
            rotateAngle = Vector2Extensions.AngleBetweenVectors(basePos, currentPos, centerPos);

            // 回転
            foreach (var pair in movableAndPos)
            {
                RotateVertex(pair.Key.Vertex.VertexData, pair.Value, centerPos, rotateAngle);
                pair.Key.OnMove();
            }
        }

        private void RotateVertex(VertexData data, Vector2 originPos, Vector2 centerPos, float angle)
        {
            var pos = originPos.RotatePoint(centerPos, angle);
            data.SetPosition(pos);
        }

        private void Initialize()
        {
            movableAndPos.Clear();
            magnitudeSum = 0;
            targetCollider = null;
        }

        private void EndRotateVertices()
        {
            var currentPos = new List<VertexDataToPos>();
            foreach(var pair in movableAndPos)
            {
                pair.Key?.OnMoveEnd();

                var data = pair.Key.Vertex.VertexData;
                currentPos.Add(new VertexDataToPos(data, data.Position.Value));
            }

            RecordVertcesMoving(previousPos, currentPos);

            Initialize();
        }
    }
}
