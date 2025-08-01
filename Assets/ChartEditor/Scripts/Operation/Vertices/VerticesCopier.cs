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
        INotesDataGetter notesGetter;
        IChartEditorDataGetter dataGetter;
        List<VertexData> copiedVertices;

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

        void Update()
        {
            if(dataGetter.EditNoteType.Value != EditNoteType.Vertices) { return; }

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
            EditMode currentEditMode = dataGetter.CurrentEditMode.Value;
            if (currentEditMode.IsInEditModeList(ignoreEditModes)) { return; }

            copiedVertices = notesGetter.EditingVertices.Value.SpaceHoldVertices.Vertices.Select(v => new VertexData(v)).ToList();
            Debug.Log("【Vertices】頂点リストをコピー");
        }

        /// <summary>
        /// 頂点リストの張り付け
        /// </summary>
        private void PasteVertices()
        {
            EditMode currentEditMode = dataGetter.CurrentEditMode.Value;
            if (currentEditMode.IsInEditModeList(ignoreEditModes)) { return; }
            if (copiedVertices == null) { return; }

            // 現在編集中の頂点データを全て消して新たに代入する
            var currentEdit = notesGetter.EditingVertices.Value.SpaceHoldVertices;
            var copiedVerticesCopy = copiedVertices.Select(v => new VertexData(v)).ToList();
            var originVertices = notesGetter.EditingVertices.Value.SpaceHoldVertices.Vertices.Select(v => new VertexData(v)).ToList();

            Record(() =>
            // 張り付け
            {
                currentEdit.SetVertices(copiedVerticesCopy);
            }, () =>
            // 元に戻す
            {
                currentEdit.SetVertices(originVertices);
            });
           
            Debug.Log("【Vertices】頂点リストを張り付け");
        }
    }

}