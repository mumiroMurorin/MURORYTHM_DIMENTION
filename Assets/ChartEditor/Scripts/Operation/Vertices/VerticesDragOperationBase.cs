using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System.Linq;
using static UndoRedo.Vertices.VerticesMoveRecord;

namespace ChartEditor
{
    /// <summary>
    /// 頂点をドラッグして動かす操作(移動・回転・拡大縮小)の共通処理
    /// 派生クラスは「カーソル位置から各頂点の新しい座標を求める式」だけを実装する
    /// </summary>
    public abstract class VerticesDragOperationBase : MonoBehaviour
    {
        [SerializeField] float firstMovingThreshold = 0.15f;
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        [SerializeField] VertexObjectsController verticesController;
        [SerializeField] MultiVertexSelector multiSelector;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        INotesDataGetter notesGetter;

        readonly List<DragTarget> targets = new List<DragTarget>();
        float magnitudeSum;
        bool isDragging;
        // 中心点を掴んで、すべての頂点を動かしている最中か
        bool isDraggingCenterHandle;
        Vector3 cursorPos;
        IPointMovableCollider targetCollider;

        /// <summary>
        /// ドラッグ開始時の、掴んだ頂点の座標(正規化済み)
        /// </summary>
        protected Vector2 BasePos { get; private set; }

        /// <summary>
        /// ドラッグ開始時の、選択中の頂点群の中心(正規化済み)
        /// </summary>
        protected Vector2 CenterPos { get; private set; }

        /// <summary>
        /// 操作待ちのエディットモード
        /// </summary>
        protected abstract EditMode IdleMode { get; }

        /// <summary>
        /// ドラッグ中のエディットモード
        /// </summary>
        protected abstract EditMode DraggingMode { get; }

        /// <summary>
        /// 掴んだ頂点が選択されていなくても操作対象に含めるか
        /// </summary>
        protected virtual bool IncludesClickedVertex => false;

        /// <summary>
        /// 頂点の新しい座標を求める
        /// </summary>
        /// <param name="originPos">ドラッグ開始時の頂点座標(正規化済み)</param>
        /// <param name="cursorPos">現在のカーソル位置(正規化済み)</param>
        protected abstract Vector2 CalcPosition(Vector2 originPos, Vector2 cursorPos);

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
            if (currentEditMode != IdleMode && currentEditMode != DraggingMode)
            {
                // ドラッグの途中で別のモードに切り替えられたら、その場で確定する
                // (確定しないと、掴んでいた頂点のコライダーが無効のまま残る)
                if (isDragging) { EndDrag(); }
                return;
            }

            // カーソル位置が動いたかの判定
            var currentPos = cursorInteracter.Value.GetWorldPositionUnderCursor();
            var delta = currentPos - cursorPos;
            cursorPos = currentPos;

            if (Input.GetMouseButtonDown(0))
            {
                if (isDragging) { EndDrag(); }
                Initialize();
            }

            // 頂点オブジェクトを動かす
            if (Input.GetMouseButton(0) && delta.sqrMagnitude > 0)
            {
                // 初動、誤動作防止のためある程度カーソルが動くまで待つ
                if (magnitudeSum < firstMovingThreshold)
                {
                    if (targetCollider == null) { targetCollider = dataGetter.GetInteractableCollider<IPointMovableCollider>(); }
                    magnitudeSum += delta.magnitude;
                    return;
                }

                // 初クリック時＆カーソルを動かしたとき動作開始
                if (currentEditMode != DraggingMode)
                {
                    if (targetCollider == null) { return; }

                    StartDrag(targetCollider);
                    dataSetter.SetEditMode(DraggingMode);
                }

                Drag();
            }
            // 頂点オブジェクトの移動終了
            else if (Input.GetMouseButtonUp(0) && currentEditMode == DraggingMode)
            {
                EndDrag();
                dataSetter.SetEditMode(IdleMode);
            }
        }

        private void Initialize()
        {
            targets.Clear();
            magnitudeSum = 0;
            targetCollider = null;
            isDragging = false;
            isDraggingCenterHandle = false;
        }

        /// <summary>
        /// ドラッグ開始。操作対象の頂点と、開始時点の座標を記録する
        /// </summary>
        private void StartDrag(IPointMovableCollider movableCollider)
        {
            if (movableCollider == null) { return; }

            var clickedVertex = movableCollider.Vertex?.Vertex;
            if (clickedVertex == null) { return; }

            // 中心点を掴んで操作できるのは、掴んだ点ごと動かす操作(移動)だけ
            // (回転・拡大縮小は中心が基準なので、中心点を掴んでも動かしようがない)
            isDraggingCenterHandle = clickedVertex.IsCenterHandle;
            if (isDraggingCenterHandle && !IncludesClickedVertex)
            {
                isDraggingCenterHandle = false;
                return;
            }

            isDragging = true;

            // 基準となる点の座標(正規化済み)を保存
            BasePos = clickedVertex.VertexData.Position.Value;

            // 選択された点群の中心を導出
            var selectedPositions = multiSelector.SelectingVertices.Select(x => x.Position.Value).ToArray();
            CenterPos = selectedPositions.Length > 0 ? selectedPositions.Center() : BasePos;

            // 掴んだ頂点を先に登録
            if (IncludesClickedVertex && clickedVertex.TryGetComponent(out IPointMovableObject clickedMovable))
            {
                AddTarget(clickedMovable);
            }

            // 中心点を掴んだときは、選択に関係なくすべての頂点を一緒に動かす
            if (isDraggingCenterHandle)
            {
                foreach (var pair in notesGetter.DataToVertexObject)
                {
                    if (pair.Object == null) { continue; }
                    if (!pair.Object.TryGetComponent(out IPointMovableObject movable)) { continue; }

                    AddTarget(movable);
                }

                return;
            }

            // 複数選択されたオブジェクトから動かせるやつを取り出す
            foreach (var data in multiSelector.SelectingVertices)
            {
                // 頂点オブジェクトが作り直された直後など、対応するオブジェクトが無い頂点は飛ばす
                var obj = notesGetter.GetVertexObject(data);
                if (obj == null) { continue; }
                if (!obj.TryGetComponent(out IPointMovableObject movable)) { continue; }

                AddTarget(movable);
            }
        }

        private void AddTarget(IPointMovableObject movable)
        {
            foreach (var target in targets)
            {
                if (target.Movable == movable) { return; }
            }

            movable.OnMoveStart();
            targets.Add(new DragTarget(movable, movable.Vertex.VertexData.Position.Value));
        }

        /// <summary>
        /// ドラッグ中。カーソル位置を取得しつつ頂点の座標を更新する
        /// </summary>
        private void Drag()
        {
            if (targets.Count == 0) { return; }

            // 配置可能な場所でなければ返す
            var deployable = dataGetter.GetInteractableCollider<IPointDeployableCollider>();
            if (deployable == null) { return; }

            // カーソルが何にも当たっていないときは返す
            if (cursorPos == Vector3.one * -9999) { return; }

            Vector2 normalizedCursorPos = verticesController.WorldPosToNormalizedPos(cursorPos);

            // 中心点で全体を動かすときは、形が崩れないよう、どの頂点も範囲の外に出ない位置で止める
            if (isDraggingCenterHandle)
            {
                normalizedCursorPos = BasePos + LimitTranslation(normalizedCursorPos - BasePos);
            }

            foreach (var target in targets)
            {
                target.Movable.Vertex.VertexData.SetPosition(CalcPosition(target.OriginPos, normalizedCursorPos));
                target.Movable.OnMove();
            }
        }

        /// <summary>
        /// すべての頂点が配置できる範囲(単位円)に収まるよう、移動量を縮める
        /// (頂点は範囲の外に出ると個別に縁へ押し戻されるので、そのまま動かすと形が崩れる)
        /// </summary>
        private Vector2 LimitTranslation(Vector2 translation)
        {
            if (IsAllTargetsInsideAfter(translation)) { return translation; }

            // 収まる最大の割合を二分探索で求める
            float low = 0f;
            float high = 1f;
            for (int i = 0; i < 16; i++)
            {
                float middle = (low + high) * 0.5f;
                if (IsAllTargetsInsideAfter(translation * middle)) { low = middle; }
                else { high = middle; }
            }

            return translation * low;
        }

        private bool IsAllTargetsInsideAfter(Vector2 translation)
        {
            foreach (var target in targets)
            {
                // 中心点そのものは頂点ではないので対象外
                if (target.IsCenterHandle) { continue; }

                // 縁の上にある頂点は、計算の誤差でわずかに範囲の外にいることがある
                // 「今より外へは出さない」という条件にして、縁に接した多角形でも内側へは動かせるようにする
                float limit = Mathf.Max(1f, target.OriginPos.sqrMagnitude) + 0.0001f;
                if ((target.OriginPos + translation).sqrMagnitude > limit) { return false; }
            }

            return true;
        }

        /// <summary>
        /// ドラッグ終了。座標が変わっていればUndo履歴に登録する
        /// </summary>
        private void EndDrag()
        {
            var previousPos = new List<VertexDataToPos>();
            var currentPos = new List<VertexDataToPos>();
            bool isChanged = false;

            foreach (var target in targets)
            {
                // ドラッグ中に破棄されたオブジェクトには触らない
                if (target.Movable is Object unityObject && unityObject == null) { continue; }

                target.Movable.OnMoveEnd();

                // 中心点は譜面のデータではないので、Undo履歴には含めない
                if (target.IsCenterHandle) { continue; }

                var data = target.Movable.Vertex.VertexData;
                previousPos.Add(new VertexDataToPos(data, target.OriginPos));
                currentPos.Add(new VertexDataToPos(data, data.Position.Value));

                if (data.Position.Value != target.OriginPos) { isChanged = true; }
            }

            // 動かした頂点が無いときはUndo履歴に積まない
            if (isChanged) { RecordVertcesMoving(previousPos, currentPos); }

            Initialize();
        }

        private class DragTarget
        {
            public DragTarget(IPointMovableObject movable, Vector2 originPos)
            {
                Movable = movable;
                OriginPos = originPos;
            }

            public IPointMovableObject Movable { get; }

            /// <summary>
            /// 多角形の頂点ではなく、中心点か
            /// </summary>
            public bool IsCenterHandle => Movable.Vertex != null && Movable.Vertex.IsCenterHandle;

            /// <summary>
            /// ドラッグ開始時の座標(正規化済み)
            /// </summary>
            public Vector2 OriginPos { get; }
        }
    }
}
