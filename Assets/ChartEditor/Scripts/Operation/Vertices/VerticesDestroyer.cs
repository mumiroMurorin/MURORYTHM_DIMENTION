using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System.Linq;

namespace ChartEditor
{
    public class VerticesDestroyer : MonoBehaviour
    {
        [SerializeField] VerticesController verticesController;
        [SerializeField] MultiVertexSelector vertexSelector;
        IChartEditorDataGetter chartEditorDataGetter;

        EditMode[] ignoreEditModes = new EditMode[] {
             EditMode.VertexMoving,
             EditMode.VerticesRotating,
             EditMode.VerticesScaling
        };

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        private void Update()
        {
            // Deleteキーで消す
            if (Input.GetKeyDown(KeyCode.Delete)) 
            {
                // 除外エディットモード中は返す
                if (chartEditorDataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes)) { return; }

                DestroyVertices();
            }
        }

        private void DestroyVertices()
        {
            var currentEditVertices = chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices;

            // 3点以下だった場合消さない
            if (currentEditVertices.Vertices.Count - vertexSelector.SelectingVertices.Count < 3)
            {
                Debug.Log("【Vertices】これ以上頂点を消すことはできません。メッシュの生成には3点以上必要です");
                return;
            }

            // 順番に消していく
            foreach (var data in vertexSelector.SelectingVertices)
            {
                DestroyVertex(data);
            }

            // 選択解除
            vertexSelector.DeselectAll();
        }

        /// <summary>
        /// 引数の頂点データをオブジェクトごと消す
        /// </summary>
        /// <param name="data"></param>
        public void DestroyVertex(VertexData data)
        {
            // オブジェクトの削除
            var obj = verticesController.DataToObj.GetObject(data);
            if (obj.gameObject.TryGetComponent(out IDestroyableVertex destroyable)) { destroyable.OnDestroy(); }

            // データの削除
            var currentEditVertices = chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices;
            currentEditVertices.RemoveVertex(data);

            data = null;
        }
    }

}
