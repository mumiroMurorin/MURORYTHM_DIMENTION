using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class MultiVertexSelector : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        List<ISelectableVertexObject> selectingObjects = new List<ISelectableVertexObject>();
        public List<VertexData> SelectingVertices { get; private set; } = new List<VertexData>();

        IChartEditorDataGetter dataGetter;
        EditMode[] ignoreEditModes = new EditMode[] {
             EditMode.VertexMoving,
             EditMode.VerticesRotating,
             EditMode.VerticesScaling,
             EditMode.Preview,
        };

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter)
        {
            this.dataGetter = dataGetter;
        }

        void Update()
        {
            if (dataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes)) { return; }
            if (dataGetter.EditNoteType.Value != EditNoteType.Vertices) { return; }

            // Ctrl+左クリックで複数選択
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(0))
            {
                var collider = dataGetter.GetInteractableCollider<ISelectableVertexCollider>();
                if (collider == null) { return; }

                SelectMulti(collider.SelectableObject);
            }
            // Ctrlが押されず左クリックされた場合
            else if (Input.GetMouseButtonDown(0))
            {
                // カーソルがUI上にあるときは返す
                if (EventSystem.current.IsPointerOverGameObject()) { return; }

                // カーソル先が頂点オブジェクトでないなら選択解除する
                var collider = dataGetter.GetInteractableCollider<ISelectableVertexCollider>();
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
            SelectingVertices.Add(obj.VertexObject.VertexData);
            obj.OnSelect();
        }

        /// <summary>
        /// 選択リストに追加
        /// </summary>
        public void SelectMulti(ISelectableVertexObject obj)
        {
            // 既に含まれている場合はそのオブジェクトをリストから削除
            if (selectingObjects.Contains(obj)) 
            {
                selectingObjects.Remove(obj);
                SelectingVertices.Remove(obj.VertexObject.VertexData);
                obj.OnDeselect();
            }
            // 含まれていない場合はリストに追加
            else
            {
                selectingObjects.Add(obj);
                SelectingVertices.Add(obj.VertexObject.VertexData);
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
