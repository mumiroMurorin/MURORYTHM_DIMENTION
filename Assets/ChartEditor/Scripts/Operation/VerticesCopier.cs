using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class VerticesCopier : MonoBehaviour
    {
        IChartEditorDataGetter chartEditorDataGetter;
        SpaceHoldVertices vertices;

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
            if (currentEditMode != EditMode.VertexDeploy && currentEditMode != EditMode.VertexMove) { return; }

            vertices = chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices;
            Debug.Log("【Vertices】頂点リストをコピー");
        }

        /// <summary>
        /// 頂点リストの張り付け
        /// </summary>
        private void PasteVertices()
        {
            EditMode currentEditMode = chartEditorDataGetter.CurrentEditMode.Value;
            if (currentEditMode != EditMode.VertexDeploy && currentEditMode != EditMode.VertexMove) { return; }
            if (vertices == null) { return; }

            // 現在編集中の頂点データを全て消して新たに代入する
            var currentEdit = chartEditorDataGetter.EditingVertices.Value.SpaceHoldVertices;
            var positionList = new Vector2[vertices.Vertices.Count];
            for(int i = 0;i< vertices.Vertices.Count; i++)
            {
                positionList[i] = vertices.Vertices[i].Position.Value;
            }

            currentEdit.SetVertices(positionList);
            Debug.Log("【Vertices】頂点リストを張り付け");
        }
    }

}