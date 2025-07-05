using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class NoteMover : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;
        [SerializeField] MultiNoteSelector notesSelector;
        [SerializeField] NoteObjectsController notesController;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;

        Dictionary<IMovableObject, AddressInChart> movableAndDelta = new Dictionary<IMovableObject, AddressInChart>();
        AddressInChart baseAddress;
        IMovableObject baseNote;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        void Update()
        {
            // 配置モードのみ
            if (dataGetter.CurrentEditMode.Value != EditMode.Move &&
                dataGetter.CurrentEditMode.Value != EditMode.Moving) { return; }

            if (Input.GetMouseButtonDown(0)) { StartMovingNotes(); }
            else if (Input.GetMouseButton(0)) { MoveNotes(); } 
            else if (Input.GetMouseButtonUp(0)) { EndMoveNote(); }
        }

        /// <summary>
        /// クリックされた時
        /// </summary>
        private void StartMovingNotes()
        {
            Initialize();

            // 動かせるオブジェクトでなければ返す
            var collider = dataGetter.GetInteractableCollider<IMovableCollider>();
            var movableObject = collider?.Note;

            if (collider == null) { return; }
            if (movableObject == null) { return; }

            // 基準となるクリックされたノーツ情報を保存
            baseAddress = new AddressInChart(movableObject.Note.NoteData.Address);
            baseNote = movableObject;

            // 複数選択されたオブジェクトから動かせるやつを取り出す
            foreach (var data in notesSelector.SelectingNotes)
            {
                var obj = notesController.DataToObj.GetObject(data);
                if (!obj.TryGetComponent(out IMovableObject movable)) { continue; }

                // ノーツオブジェクト側の動作
                movable.OnMoveStart();

                // 差分を保存
                movableAndDelta.TryAdd(movable, new AddressInChart(movable.Note.NoteData.Address) - baseAddress);
            }

            dataSetter?.SetEditMode(EditMode.Moving);
        }
        
        /// <summary>
        /// クリック中(移動中)
        /// </summary>
        private void MoveNotes()
        {
            if (movableAndDelta == null || movableAndDelta.Count == 0) { return; }

            // カーソル下の親取得
            var deployable = dataGetter.GetInteractableCollider<IDeployableCollider>();
            if (deployable == null) { return; }
            //if (baseNote.Note.transform.position == deployable.deployParent.position) { return; }

            // アドレスの移動
            foreach (var pair in movableAndDelta)
            {
                dataGetter.ChartData.Value.ChangeNoteAddress(pair.Key.Note.NoteData, deployable.Address + pair.Value);
                pair.Key.OnMove();
            }
        }

        /// <summary>
        /// クリック終了時(移動終了時)
        /// </summary>
        private void EndMoveNote()
        {
            foreach (var pair in movableAndDelta)
            {
                pair.Key?.OnMoveEnd();
            }

            Initialize();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        private void Initialize()
        {
            baseAddress = null;
            baseNote = null;
            movableAndDelta.Clear();
        }
    }
}
