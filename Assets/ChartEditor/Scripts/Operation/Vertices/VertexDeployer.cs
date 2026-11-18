using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;
using UniRx;
using static UndoRedo.History;

namespace ChartEditor
{
    /// <summary>
    /// 頂点の配置
    /// 頂点の編集ツールで、頂点も中心点も無い所 (円の中) を押すと、そこに頂点を置く
    /// (頂点の上を押したときは、選択と移動になる。MultiVertexSelector / VerticesMover)
    /// </summary>
    public class VertexDeployer : MonoBehaviour
    {
        [SerializeField] VertexObjectsController verticesController;
        [SerializeField] VerticesDestroyer verticesDestroyer;
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        
        IChartEditorDataGetter dataGetter;
        INotesDataGetter notesGetter;
        IPointDeployableCollider deployableCollider;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter)
        {
            this.notesGetter = notesGetter;
            this.dataGetter = dataGetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 頂点配置可能に
            dataGetter.InteractableColliders.ObserveAdd()
                .Subscribe(collider =>
                {
                    if (collider.Value is IPointDeployableCollider matched) 
                    {
                        deployableCollider = matched;
                    }
                }).AddTo(this.gameObject);

            // 頂点配置不可能に
            dataGetter.InteractableColliders.ObserveReset()
                .Subscribe(_ => { deployableCollider = null; })
                .AddTo(this.gameObject);
        }

        // 押したまま、この距離 (画面のピクセル) より動かしたら、クリックではない (範囲選択の始まりと同じ)
        const float CLICK_MOVE_LIMIT = 5f;
        // 置く操作として押したか (離したときに、動かしていなければ置く)
        bool isClickPending;
        Vector2 pressScreenPosition;

        void Update()
        {
            // 頂点の配置ツールの、操作待ちのときだけ (編集ツールでは置かない。録音 20261118_030551)
            var currentEditMode = dataGetter.CurrentEditMode.Value;
            if (currentEditMode != EditMode.VertexPlace)
            {
                isClickPending = false;
                return;
            }

            // 頂点オブジェクトの配置: 押したまま動かさずに離したとき (録音 20261118_201736: 押したときに置くと、
            // 範囲選択のドラッグでも置かれてしまう。ドラッグは範囲選択・頂点の移動にだけ使う)
            if (Input.GetMouseButtonDown(0))
            {
                // 範囲選択 (Shift を押しながらドラッグ) と、複数選択 (Ctrl を押しながらクリック) の操作では置かない
                // ボタンなどの画面部品を押したときは、裏に置かない
                bool isOnUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                isClickPending = !ModifierKey.Shift && !ModifierKey.Ctrl && !isOnUI &&
                                 dataGetter.GetInteractableCollider<IPointDeployableCollider>() != null;
                pressScreenPosition = Input.mousePosition;
            }
            if (!isClickPending) { return; }

            if (((Vector2)Input.mousePosition - pressScreenPosition).magnitude > CLICK_MOVE_LIMIT)
            {
                isClickPending = false;
                return;
            }
            if (Input.GetMouseButtonUp(0))
            {
                isClickPending = false;
                DeployVertex();
            }
        }

        /// <summary>
        /// ノーツの配置
        /// </summary>
        private void DeployVertex()
        {
            // カーソルの下が「頂点を置ける所」のときだけ (頂点や中心点の上では、置ける所は取れない)
            if (deployableCollider == null) { return; }
            // 今カーソルの下にあるものでも確かめる
            // (スペースノートのダブルクリックで頂点のレイヤーに入った、そのクリックで置いてしまわないように)
            if (dataGetter.GetInteractableCollider<IPointDeployableCollider>() == null) { return; }
            if (notesGetter.EditingVertices.Value == null) { return; }

            Vector3 worldPos = cursorInteracter.Value.GetWorldPositionUnderCursor();

            // データ上の追加
            VertexData vertexData = new VertexData(verticesController.WorldPosToNormalizedPos(worldPos));

            var vertices = notesGetter.EditingVertices.Value.SpaceVertices;
            // RedoUndoに対応
            Record(() =>
            // 配置
            {
                DeployVertex(vertices, vertexData);
            }, () =>
            // 配置取り消し
            {
                verticesDestroyer.DestroyVertex(vertices, vertexData);
            }); 
        }

        /// <summary>
        /// 引数の頂点データを配置する
        /// </summary>
        /// <param name="data"></param>
        public void DeployVertex(SpaceVertices vertices, VertexData data)
        {
            vertices.AddVertex(data);
        }
    }
}

