using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using UnityEngine.EventSystems;

namespace ChartEditor
{
    public class MultiNoteSelector : MonoBehaviour
    {
        IChartEditorDataGetter dataGetter;
        INotesDataGetter notesGetter;
        INotesDataSetter notesSetter;

        EditMode[] ignoreEditModes = new EditMode[] {
            EditMode.Connecting,
            EditMode.DisConnect,
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Moving,
            EditMode.Scaling,
            EditMode.SpaceMoving,
        };

        [Inject]
        public void Construct(INotesDataGetter notesGetter, INotesDataSetter notesSetter, IChartEditorDataGetter dataGetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 選択中ノーツにデータが追加された時の挙動
            notesGetter.SelectingNotes.ObserveAdd()
                .Subscribe(data => {
                    var obj = notesGetter.GetNoteObject(data.Value);
                    if (obj == null) { return; }
                    if (!obj.TryGetComponent(out ISelectableNoteObject selectable)) { return; }

                    // 選択
                    selectable.OnSelect();
                })
                .AddTo(this.gameObject);

            // 選択中ノーツからデータが削除された時の挙動
            notesGetter.SelectingNotes.ObserveRemove()
                .Subscribe(data => {
                    var obj = notesGetter.GetNoteObject(data.Value);
                    if (obj == null) { return; }
                    if (!obj.TryGetComponent(out ISelectableNoteObject selectable)) { return; }

                    // 選択解除
                    selectable.OnDeselect();
                })
                .AddTo(this.gameObject);
        }

        void Update()
        {
            if (dataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes)) { return; }

            // Ctrl+左クリックで複数選択
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(0))
            {
                var collider = dataGetter.GetInteractableCollider<ISelectableNoteCollider>();
                if (collider == null) { return; }

                SelectMulti(collider.SelectableObject);
            }
            // Ctrlが押されず左クリックされた場合
            else if (Input.GetMouseButtonDown(0))
            {
                // カーソルがUI上にあるときは返す
                if (EventSystem.current.IsPointerOverGameObject()) { return; }

                // カーソル先がノートオブジェクトでないなら選択全解除する
                var collider = dataGetter.GetInteractableCollider<ISelectableNoteCollider>();
                if (collider == null) 
                {
                    notesSetter.ClearSelectingNotes();
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
            foreach(var n in notesGetter.SelectingNotes) { if (n == obj.NoteObject.NoteData) { return; }; }

            // 既に選択されているオブジェクトを選択解除する
            notesSetter.ClearSelectingNotes();

            // 含まれていない場合はリストに追加
            notesSetter.TryAddSelectingNotes(obj.NoteObject.NoteData);
        }

        /// <summary>
        /// 選択リストに追加
        /// </summary>
        public void SelectMulti(ISelectableNoteObject obj)
        {
            // 追加
            if (notesSetter.TryAddSelectingNotes(obj.NoteObject.NoteData)) { return; }
            // 削除
            notesSetter.TryRemoveSelectingNotes(obj.NoteObject.NoteData);
        }
    }

}