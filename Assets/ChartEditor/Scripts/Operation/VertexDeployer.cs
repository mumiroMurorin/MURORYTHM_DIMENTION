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
        [SerializeField] VerticesController verticesController;
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        
        IChartEditorDataGetter chartEditorDataGetter;
        IPointDeployableCollider deployableCollider;

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
            // 頂点配置可能に
            chartEditorDataGetter.InteractableColliders.ObserveAdd()
                .Subscribe(collider =>
                {
                    if (collider.Value is IPointDeployableCollider matched) 
                    {
                        deployableCollider = matched;
                    }
                }).AddTo(this.gameObject);

            // 頂点配置不可能に
            chartEditorDataGetter.InteractableColliders.ObserveReset()
                .Subscribe(_ => { deployableCollider = null; })
                .AddTo(this.gameObject);
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) { DeployVertex(); }
        }

        /// <summary>
        /// ノーツの配置
        /// </summary>
        private void DeployVertex()
        {
            // 配置モードでない際は返す
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.VertexDeploy) { return; }
            if (deployableCollider == null) { return; }

            // データ上の追加
            Vector3 worldPos = cursorInteracter.Value.GetWorldPositionUnderCursor();
            SpaceHoldVertex vertexData = new SpaceHoldVertex(verticesController.WorldPosToNormalizedPos(worldPos));

            chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices.AddVertex(vertexData);
        }
    }
}

