using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System.Linq;
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
             EditMode.VerticesScaling,
             EditMode.Preview,
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

            // 削除後に3頂点未満になる場合は削除しない
            if (currentEditVertices.Vertices.Count - vertexSelector.SelectingVertices.Count < 3)
            {
                Debug.Log("【Vertices】これ以上頂点を削除できません。メッシュの生成には3頂点以上必要です。");
                return;
            }

            // indexの大きい順で削除するため
            var verticesReverse = new List<VertexData>(vertexSelector.SelectingVertices.OrderByDescending(x => x.VertexIndex));
            // indexの小さい順で復元するため
            var verticesSorted = new List<VertexData>(vertexSelector.SelectingVertices.OrderBy(x => x.VertexIndex));

            // Redo / Undo対応
            Record(() =>
            // 削除
            {
                foreach (var data in verticesReverse) { DestroyVertex(currentEditVertices, data); }

                // 選択解除
                vertexSelector.DeselectAll();

            }, () =>
            // 削除取り消し
            {
                foreach (var data in verticesSorted) { vertexDeployer.DeployVertex(currentEditVertices, data); }
            });
        }

        /// <summary>
        /// 指定した頂点データをオブジェクトごと削除する
        /// </summary>
        public void DestroyVertex(SpaceHoldVertices vertices, VertexData data)
        {
            // データの削除
            vertices.RemoveVertex(data);
        }
    }

}
