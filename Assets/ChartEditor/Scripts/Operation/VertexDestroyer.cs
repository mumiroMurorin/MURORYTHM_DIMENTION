using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class VertexDestroyer : MonoBehaviour
    {
        [SerializeField] MultiVertexSelector vertexSelector;
        IChartEditorDataGetter chartEditorDataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        private void Update()
        {
            // Deleteキーで消す
            if (Input.GetKeyDown(KeyCode.Delete)) { DestroyVertex(); }
        }

        private void DestroyVertex()
        {
            var currentEditVertices = chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices;

            // 3点以下だった場合消さない
            if (currentEditVertices.Vertices.Count - vertexSelector.SelectingVertices.Count < 3)
            {
                Debug.Log("【Vertices】これ以上頂点を消すことはできません。メッシュの生成には3点以上必要です");
                return;
            }

            // 順番に消していく
            foreach (var obj in vertexSelector.SelectingVertices)
            {
                // 消せるやつだけ消す
                if(!obj.gameObject.TryGetComponent(out IDestroyableVertex destroyable)) { continue; }

                // データから削除
                currentEditVertices.RemoveVertex(obj.VertexData);

                destroyable.OnDestroy();
                obj.VertexData = null;
            }
        }
    }

}
