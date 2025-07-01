using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System.Linq;
using UniRx;
using static UndoRedo.History;

namespace ChartEditor
{
    public class VerticesCopier : MonoBehaviour
    {
        IChartEditorDataGetter chartEditorDataGetter;
        List<VertexData> copiedVertices;

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

        void Update()
        {
            // 頂点リストのコピー
            if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C)) { CopyVertices(); }
            // 頂点リストの貼り付け
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V)) { PasteVertices(); }
        }

        /// <summary>
        /// 頂点リストのコピー
        /// </summary>
        private void CopyVertices()
        {
            EditMode currentEditMode = chartEditorDataGetter.CurrentEditMode.Value;
            if (currentEditMode.IsInEditModeList(ignoreEditModes)) { return; }

            copiedVertices = chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices.Vertices.Select(v => new VertexData(v)).ToList();
            Debug.Log("【Vertices】頂点リストをコピー");
        }

        /// <summary>
        /// 頂点リストの張り付け
        /// </summary>
        private void PasteVertices()
        {
            EditMode currentEditMode = chartEditorDataGetter.CurrentEditMode.Value;
            if (currentEditMode.IsInEditModeList(ignoreEditModes)) { return; }
            if (copiedVertices == null) { return; }

            // 現在編集中の頂点データを全て消して新たに代入する
            var currentEdit = chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices;
            var copiedVerticesCopy = new List<VertexData>(copiedVertices);
            var originVertices = chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices.Vertices.Select(v => new VertexData(v)).ToList();

            Record(() =>
            // 張り付け
            {
                currentEdit.SetVertices(copiedVerticesCopy);
            }, () =>
            // 元に戻す
            {
                currentEdit.SetVertices(originVertices);
            });
           
            //Debug.Log("【Vertices】頂点リストを張り付け");
        }
    }

}