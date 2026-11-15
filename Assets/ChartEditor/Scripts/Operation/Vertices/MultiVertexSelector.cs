using System;
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
        readonly Subject<Unit> selectionChanged = new Subject<Unit>();
        public IObservable<Unit> OnSelectionChanged => selectionChanged;

        IChartEditorDataGetter dataGetter;
        INotesDataGetter notesGetter;
        bool needsRelink;
        EditMode[] ignoreEditModes = new EditMode[] {
             EditMode.VertexMoving,
             EditMode.VerticesRotating,
             EditMode.VerticesScaling,
             EditMode.Preview,
        };

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
        }

        private void Start()
        {
            Bind();
        }

        void Update()
        {
            // 頂点オブジェクトが作り直されていたら、選択を新しいオブジェクトに付け替える
            if (needsRelink) { RelinkSelection(); }

            if (dataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes)) { return; }
            if (dataGetter.EditNoteType.Value != EditNoteType.Vertices) { return; }

            // 編集ツールで Shift を押しながらのドラッグは、範囲選択 (VerticesRangeSelector)。押した所の頂点は、ここでは選ばない
            // (ここで選ぶと、範囲選択の結果と二重になって、その頂点の選択が外れてしまう)
            // Ctrl も押しているときは、今の選択に足す・外すので、選択を残す
            if (RangeSelectGesture.IsForced(dataGetter))
            {
                if (Input.GetMouseButtonDown(0) && !ModifierKey.Ctrl && !EventSystem.current.IsPointerOverGameObject())
                {
                    DeselectAll();
                }
                return;
            }

            // Ctrl+左クリックで複数選択
            if (ModifierKey.Ctrl && Input.GetMouseButtonDown(0))
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
                    // 中心点を掴んだときは、選択を変えない
                    var movableCollider = dataGetter.GetInteractableCollider<IPointMovableCollider>();
                    var clickedVertex = movableCollider?.Vertex?.Vertex;
                    if (clickedVertex != null && clickedVertex.IsCenterHandle) { return; }

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

        private void Bind()
        {
            dataGetter.EditNoteType
                    .Subscribe(editNoteType =>
                    {
                        if (editNoteType != EditNoteType.Vertices)
                        {
                            DeselectAll();
                        }
                    })
                    .AddTo(this.gameObject);

            // 頂点オブジェクトが破棄されたとき、選択が古いオブジェクトを指したままにしない
            // 反転・並べ替えでは同じ頂点のオブジェクトがすぐ作り直されるので、次のフレームで付け替える
            notesGetter.DataToVertexObject.ObserveRemove()
                    .Subscribe(removed =>
                    {
                        if (SelectingVertices.Contains(removed.Value.Data)) { needsRelink = true; }
                    })
                    .AddTo(this.gameObject);

            // 編集対象の切り替え・貼り付けなどで頂点が総入れ替えになったときは選択を解除する
            notesGetter.DataToVertexObject.ObserveReset()
                    .Subscribe(_ => DeselectAll())
                    .AddTo(this.gameObject);
        }

        /// <summary>
        /// 選択中の頂点を、現在の頂点オブジェクトに付け替える
        /// 対応するオブジェクトが無くなった頂点(削除された頂点)は選択から外す
        /// </summary>
        private void RelinkSelection()
        {
            needsRelink = false;
            bool isChanged = false;

            for (int i = SelectingVertices.Count - 1; i >= 0; i--)
            {
                var currentObject = notesGetter.GetVertexObject(SelectingVertices[i]);

                // 頂点が削除されていた
                if (currentObject == null || !currentObject.TryGetComponent(out ISelectableVertexObject selectable))
                {
                    selectingObjects.RemoveAt(i);
                    SelectingVertices.RemoveAt(i);
                    isChanged = true;
                    continue;
                }

                // オブジェクトが作り直されていた
                if (!ReferenceEquals(selectable, selectingObjects[i]))
                {
                    selectingObjects[i] = selectable;
                    selectable.OnSelect();
                }
            }

            if (isChanged) { NotifySelectionChanged(); }
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
            NotifySelectionChanged();
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
                NotifySelectionChanged();
            }
            // 含まれていない場合はリストに追加
            else
            {
                selectingObjects.Add(obj);
                SelectingVertices.Add(obj.VertexObject.VertexData);
                obj.OnSelect();
                NotifySelectionChanged();
            }
        }

        /// <summary>
        /// 複数選択解除
        /// </summary>
        public void DeselectAll()
        {
            if (selectingObjects.Count == 0 && SelectingVertices.Count == 0) { return; }

            foreach(var obj in selectingObjects)
            {
                // 破棄済みのオブジェクトには触らない
                if (obj is UnityEngine.Object unityObject && unityObject != null) { obj.OnDeselect(); }
            }

            selectingObjects.Clear();
            SelectingVertices.Clear();
            NotifySelectionChanged();
        }

        void NotifySelectionChanged()
        {
            selectionChanged?.OnNext(Unit.Default);
        }
    }

}
