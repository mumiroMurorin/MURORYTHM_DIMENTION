using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class MultiVertexSelector : MonoBehaviour
    {
        List<ISelectableVertexObject> selectingObjects = new List<ISelectableVertexObject>();
        public List<VertexObject> SelectingVertices { get; private set; } = new List<VertexObject>();

        IChartEditorDataGetter chartEditorDataGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        void Update()
        {
            // Ctrl押しながらクリックされた時
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(0)) { Select(); }
            // Ctrl押さずに左or右クリックしたとき
            //else if (!Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(0)) { DeselectAll(); }
            else if (!Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(1)) { DeselectAll(); }
        }

        /// <summary>
        /// 選択リストに追加
        /// </summary>
        private void Select()
        {
            var collider = chartEditorDataGetter.GetInteractableCollider<ISelectableVertexCollider>();
            if (collider == null) { return; }

            var obj = collider.SelectableObject;

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
        private void DeselectAll()
        {
            foreach(var obj in selectingObjects)
            {
                obj.OnDeselect();
            }

            selectingObjects.Clear();
            SelectingVertices.Clear();
        }
    }

}