using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class MultiNoteSelector : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        List<ISelectableNoteObject> selectingObjects = new List<ISelectableNoteObject>();
        public List<IDeployableNoteData> SelectingNotes { get; private set; } = new List<IDeployableNoteData>();

        IChartEditorDataGetter chartEditorDataGetter;
        EditMode[] ignoreEditModes = new EditMode[] {
            EditMode.Connecting,
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Moving,
            EditMode.Scaling,
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

            // Ctrl+左クリックで複数選択
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(0))
            {
                var collider = chartEditorDataGetter.GetInteractableCollider<ISelectableNoteCollider>();
                if (collider == null) { return; }

                SelectMulti(collider.SelectableObject);
            }
            // Ctrlが押されず左クリックされた場合
            else if (Input.GetMouseButtonDown(0))
            {
                var collider = chartEditorDataGetter.GetInteractableCollider<ISelectableNoteCollider>();
                
                // カーソル先が頂点オブジェクトでないなら選択解除する
                if (collider == null) 
                {
                    DeselectAll();
                }
                // ノートであれば単選択する
                else
                {
                    var obj = collider.SelectableObject;
                    SelectSingle(obj);
                }
            }
        }

        private void SelectSingle(ISelectableNoteObject obj)
        {
            // 既に含まれている場合は何もしない
            if (selectingObjects.Contains(obj)) { return; }

            // 既に選択されているオブジェクトを選択解除する
            DeselectAll();

            // 含まれていない場合はリストに追加
            selectingObjects.Add(obj);
            SelectingNotes.Add(obj.NoteObject.NoteData);
            obj.OnSelect();
        }

        /// <summary>
        /// 選択リストに追加
        /// </summary>
        public void SelectMulti(ISelectableNoteObject obj)
        {
            // 既に含まれている場合はそのオブジェクトをリストから削除
            if (selectingObjects.Contains(obj)) 
            {
                selectingObjects.Remove(obj);
                SelectingNotes.Remove(obj.NoteObject.NoteData);
                obj.OnDeselect();
            }
            // 含まれていない場合はリストに追加
            else
            {
                selectingObjects.Add(obj);
                SelectingNotes.Add(obj.NoteObject.NoteData);
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
            SelectingNotes.Clear();
        }
    }

}