using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using static UndoRedo.History;

namespace ChartEditor
{
    public class VerticesDestroyer : MonoBehaviour
    {
        [SerializeField] VertexDeployer vertexDeployer;
        [SerializeField] VertexObjectsController verticesController;
        [SerializeField] MultiVertexSelector vertexSelector;

        INotesDataGetter notesGetter;
        IChartEditorDataGetter dataGetter;

        EditMode[] ignoreEditModes = new EditMode[] {
             EditMode.VertexMoving,
             EditMode.VerticesRotating,
             EditMode.VerticesScaling
        };

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter)
        {
            this.notesGetter = notesGetter;
            this.dataGetter = dataGetter;
        }

        private void Update()
        {
            if (dataGetter.EditNoteType.Value != EditNoteType.Vertices) { return; }

            // Deleteキーで消す
            if (Input.GetKeyDown(KeyCode.Delete)) 
            {
                // 除外エディットモード中は返す
                if (dataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes)) { return; }

                DestroyVertices();
            }
        }

        private void DestroyVertices()
        {
            var currentEditVertices = notesGetter.EditingVertices.Value.SpaceHoldVertices;

            // 3点以下だった場合消さない
            if (currentEditVertices.Vertices.Count - vertexSelector.SelectingVertices.Count < 3)
            {
                Debug.Log("【Vertices】これ以上頂点を消すことはできません。メッシュの生成には3点以上必要です");
                return;
            }

            var vertices = new List<VertexData>(vertexSelector.SelectingVertices);

            // RedoUndoに対応
            Record(() =>
            // 削除
            {
                foreach (var data in vertices) { DestroyVertex(currentEditVertices, data); }

                // 選択解除
                vertexSelector.DeselectAll();

            }, () =>
            // 削除取り消し
            {
                foreach (var data in vertices) { vertexDeployer.DeployVertex(currentEditVertices, data); }
            });

            
        }

        /// <summary>
        /// 引数の頂点データをオブジェクトごと消す
        /// </summary>
        /// <param name="data"></param>
        public void DestroyVertex(SpaceHoldVertices vertices, VertexData data)
        {
            // オブジェクトの削除
            var obj = verticesController.DataToObj.GetObject(data);
            if (obj != null && obj.gameObject.TryGetComponent(out IDestroyableVertex destroyable)) { destroyable.OnDestroy(); }

            // データの削除
            vertices.RemoveVertex(data);
        }
    }

}
