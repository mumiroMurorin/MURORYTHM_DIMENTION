using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class VertexDestroyer : MonoBehaviour
    {
        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorDataSetter chartEditorDataSetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter, IChartEditorDataSetter chartEditorDataSetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
            this.chartEditorDataSetter = chartEditorDataSetter;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0)) { DestroyVertex(); }
            // 右クリック時、オートモードならタイプ変更モード解除
            else if (Input.GetMouseButtonDown(1)) { BackAutoMode(); }
        }

        private void DestroyVertex()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value != EditMode.VertexDestroy) { return; }

            var collider = chartEditorDataGetter.GetInteractableCollider<IDestroyableVertexCollider>();
            if (collider == null) { return; }

            IDestroyableVertex destroyableVertex = collider.Vertex;
            if (destroyableVertex == null) { return; }

            var currentEditVertices = chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices;

            // 3点以下だった場合消さない
            if (currentEditVertices.Vertices.Count <= 3) 
            {
                Debug.Log("【Vertices】これ以上頂点を消すことはできません。メッシュの生成には3点以上必要です");
                return;
            }

            // データから削除
            currentEditVertices.RemoveVertex(destroyableVertex.Vertex.VertexData);

            destroyableVertex.OnDestroy();
            destroyableVertex.Vertex.VertexData = null;
        }

        /// <summary>
        /// オートモードに戻す
        /// </summary>
        private void BackAutoMode()
        {
            if (!chartEditorDataGetter.AutoEditMode.Value) { return; }
            chartEditorDataSetter.SetEditMode(EditMode.None);
        }
    }

}
