using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using ChartEditor;

namespace ChartEditor
{
    public class VertexDeployer : MonoBehaviour
    {
        [SerializeField] GameObject vertexPrefab;
        [SerializeField] VerticesController verticesController;
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        
        IChartEditorDataGetter chartEditorDataGetter;
        IPointDeployableObject deployingVertex;
        IVerticesControlableNoteData verticesData;
        bool isDeployedTentative;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // ノーツの削除
            chartEditorDataGetter.CurrentEditMode
                .Where(editMode => editMode != EditMode.VertexDeploy)
                .Subscribe(editMode => DestroyVertex())
                .AddTo(this.gameObject);

            // ノーツの仮配置
            chartEditorDataGetter.InteractableColliders.ObserveAdd()
                .Subscribe(collider =>
                {
                    if (collider.Value is IPointDeployableCollider matched) { UpdateVertexPosition(matched); }
                }).AddTo(this.gameObject);
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) { DeployVertex(); }
        }

        /// <summary>
        /// 配置中のノーツの位置を更新する
        /// </summary>
        private void UpdateVertexPosition(IPointDeployableCollider deployable)
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.VertexDeploy) { return; }
            if (deployable == null) { return; }

            // 配置ノーツが無かったら新規インスタンス化
            if (deployingVertex == null) { InstantiateVertex(); }
            // それでもなかったら返す
            if (deployingVertex == null) { return; }

            deployingVertex.OnMove();
            isDeployedTentative = true;
        }

        /// <summary>
        /// ノーツの配置
        /// </summary>
        private void DeployVertex()
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.VertexDeploy) { return; }
            var collider = chartEditorDataGetter.GetInteractableCollider<IPointDeployableCollider>();

            if (collider == null) { return; }
            if (!isDeployedTentative) { return; }
            // 謎のヌルリファによりこの条件も追加
            if (deployingVertex == null) { return; }

            // データ上の追加
            // 頂点の移動
            Vector2 normalized = verticesController.WorldPosToNormalizedPos(cursorInteracter.Value.GetWorldPositionUnderCursor());
            deployingVertex..SetPosition(normalized);

            // オブジェクトの設置
            deployingVertex.OnDeploy();
            InstantiateVertex();
        }

        /// <summary>
        /// ノートの生成
        /// </summary>
        private void InstantiateVertex()
        {
            GameObject obj = Instantiate(vertexPrefab);

            if (!obj.TryGetComponent(out IPointDeployableObject deployable))
            {
                Debug.LogWarning("頂点にIPointDeployableObjectがくっついてねぇぞ！");
                return;
            }

            verticesData = new ;

            deployable.OnInstantiate(deployingNoteData, GetNoteParentTransform);
            deployable.OnDestroyListner += () => OnDestroyDeployingNote();

            deployingVertex = deployable;
            isDeployedTentative = false;
        }

        /// <summary>
        /// ノートの削除
        /// </summary>
        private void DestroyVertex()
        {
            if (deployingVertex == null) { return; }
            deployingVertex.OnDisable();
        }

        private void OnDestroyDeployingNote()
        {
            deployingVertex = null;
            verticesData = null;
        }
    }
}

