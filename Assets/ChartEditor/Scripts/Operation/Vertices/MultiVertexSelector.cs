using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class MultiVertexSelector : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        List<ISelectableVertexObject> selectingObjects = new List<ISelectableVertexObject>();
        public List<VertexObject> SelectingVertices { get; private set; } = new List<VertexObject>();

        IChartEditorDataGetter chartEditorDataGetter;
        EditMode[] ignoreEditModes = new EditMode[] {
             EditMode.VertexMoving,
             EditMode.VerticesRotating,
             EditMode.VerticesScaling
        };
        Vector3 cursorPos;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        void Update()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes)) { return; }

            //// カーソル位置が動いたかの判定
            //var currentPos = cursorInteracter.Value.GetWorldPositionUnderCursor();
            //var delta = currentPos - cursorPos;
            //cursorPos = cursorInteracter.Value.GetWorldPositionUnderCursor();

            //// 左クリック+カーソル動作で範囲選択
            //if (Input.GetMouseButton(0) && delta.magnitude > 0)
            //{

            //}
            // Ctrl+左クリックで複数選択
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(0))
            {
                var collider = chartEditorDataGetter.GetInteractableCollider<ISelectableVertexCollider>();
                if (collider == null) { return; }

                SelectMulti(collider.SelectableObject);
            }
            // Ctrlが押されず左クリックされた場合
            else if (Input.GetMouseButtonDown(0))
            {
                var collider = chartEditorDataGetter.GetInteractableCollider<ISelectableVertexCollider>();
                
                // カーソル先が頂点オブジェクトでないなら選択解除する
                if (collider == null) 
                {
                    DeselectAll();
                }
                // 頂点オブジェクトであれば単選択する
                else
                {
                    var obj = collider.SelectableObject;
                    SelectSingle(obj);
                }
            }
        }

        private void SelectSingle(ISelectableVertexObject obj)
        {
            // 既に含まれている場合は何もしない
            if (selectingObjects.Contains(obj)) { return; }

            // 既に選択されているオブジェクトを選択解除する
            DeselectAll();

            // 含まれていない場合はリストに追加
            selectingObjects.Add(obj);
            SelectingVertices.Add(obj.VertexObject);
            obj.OnSelect();
        }

        /// <summary>
        /// 選択リストに追加
        /// </summary>
        private void SelectMulti(ISelectableVertexObject obj)
        {
            // 既に含まれている場合はそのオブジェクトをリストから削除
            if (selectingObjects.Contains(obj)) 
            {
                selectingObjects.Remove(obj);
                SelectingVertices.Remove(obj.VertexObject);
                obj.OnDeselect();
            }
            // 含まれていない場合はリストに追加
            else
            {
                selectingObjects.Add(obj);
                SelectingVertices.Add(obj.VertexObject);
                obj.OnSelect();
            }
        }

        /// <summary>
        /// 複数選択解除
        /// </summary>
        public void DeselectAll()
        {
            foreach(var obj in selectingObjects)
            {
                obj?.OnDeselect();
            }

            selectingObjects.Clear();
            SelectingVertices.Clear();
        }
    }

}