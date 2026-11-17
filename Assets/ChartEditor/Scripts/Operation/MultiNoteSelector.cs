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
            EditMode.Preview,
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
            // ホールドを曲げる制御点の印を押したときは、選択を変えない (選択が外れると、制御点が消えてしまう)
            if (HoldCurveEditor.IsCapturingPointer) { return; }

            // 編集ツールで Shift を押しながらのドラッグは、範囲選択 (NoteRangeSelector)。押した所のノーツは、ここでは選ばない
            // (ここで選ぶと、範囲選択の結果と二重になって、そのノーツの選択が外れてしまう)
            // Ctrl も押しているときは、今の選択に足す・外すので、選択を残す
            if (RangeSelectGesture.IsForced(dataGetter))
            {
                if (Input.GetMouseButtonDown(0) && !ModifierKey.Ctrl && !EventSystem.current.IsPointerOverGameObject())
                {
                    notesSetter.ClearSelectingNotes();
                }
                return;
            }

            // ダイナミックノーツをノーツに載せるクリックでは、下のノーツを選ばない (何も無い所に置くときと同じく、選択を外す)
            // (選んだままだと、続けて Delete を押したときに、下のノーツまで消えてしまう)
            if (dataGetter.CurrentEditMode.Value == EditMode.Deploy && DynamicNoteAttachments.IsPlacingDynamic(dataGetter))
            {
                if (Input.GetMouseButtonDown(0) && !ModifierKey.Ctrl && !EventSystem.current.IsPointerOverGameObject())
                {
                    notesSetter.ClearSelectingNotes();
                }
                return;
            }

            // Ctrl+左クリックで複数選択
            if (ModifierKey.Ctrl && Input.GetMouseButtonDown(0))
            {
                // カーソルがUI上にあるときは返す (UIの裏にあるノーツを選択してしまわないように)
                if (EventSystem.current.IsPointerOverGameObject()) { return; }

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
