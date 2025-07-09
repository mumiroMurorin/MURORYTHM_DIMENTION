using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class SpaceMover : MonoBehaviour
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        INotesDataGetter notesGetter;

        Dictionary<IFreedomMovableObject, int> movableAndDelta = new Dictionary<IFreedomMovableObject, int>();
        IFreedomDeployableCollider lastLocation;
        AddressInChart baseAddress;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter, INotesDataGetter notesGetter)
        {
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
            this.notesGetter = notesGetter;
        }

        private void LateUpdate()
        {
            // 配置モードのみ
            if (dataGetter.CurrentEditMode.Value != EditMode.SpaceMove &&
                dataGetter.CurrentEditMode.Value != EditMode.SpaceMoving) { return; }

            if (Input.GetMouseButtonDown(0)) { StartMovingNotes(); }
            else if (Input.GetMouseButton(0)) { MoveNotesOnClick(); }
            else if (Input.GetMouseButtonUp(0)) { EndMoveNote(); }
        }

        /// <summary>
        /// クリックされた時
        /// </summary>
        private void StartMovingNotes()
        {
            Initialize();

            // 動かせるオブジェクトでなければ返す
            var collider = dataGetter.GetInteractableCollider<IFreedomMovableCollider>();
            var movableObject = collider?.Note;

            if (collider == null) { return; }
            if (movableObject == null) { return; }

            // 基準となるクリックされたノーツ情報を保存
            baseAddress = new AddressInChart(movableObject.Note.NoteData.Address);

            // 複数選択されたオブジェクトから動かせるやつを取り出す
            foreach (var data in notesGetter.SelectingNotes)
            {
                var obj = notesGetter.GetNoteObject(data);
                if (!obj.TryGetComponent(out IFreedomMovableObject movable)) { continue; }

                // ノーツオブジェクト側の動作
                movable.OnMoveStart();

                // 差分を保存
                var subDelta = dataGetter.ChartData.Value.GetAddressDelta(new AddressInChart(movable.Note.NoteData.Address), baseAddress);
                movableAndDelta.TryAdd(movable, subDelta);
            }

            dataSetter?.SetEditMode(EditMode.SpaceMoving);
        }

        private void MoveNotesOnClick()
        {
            if (movableAndDelta == null || movableAndDelta.Count == 0) { return; }

            // カーソル下の親取得
            var deployable = dataGetter.GetInteractableCollider<IFreedomDeployableCollider>();
            if (deployable == null) { return; }
            if (lastLocation == deployable) { return; }

            // 最新配置場所の更新
            lastLocation = deployable;

            // アドレスの移動
            foreach (var pair in movableAndDelta)
            {
                var newAddress = dataGetter.ChartData.Value.AddressAddition(deployable.Address, pair.Value);

                MoveNote(pair.Key, new AddressWithinRange(newAddress, 1));
            }
        }

        /// <summary>
        /// ノートを指定されたアドレスに移動させる
        /// </summary>
        /// <param name="movableObject"></param>
        /// <param name="newAddress"></param>
        public void MoveNote(IFreedomMovableObject movableObject, AddressWithinRange newAddress)
        {
            var noteData = movableObject.Note.NoteData;
            noteData.SetAddress(newAddress);

            movableObject.OnMove();
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

            dataSetter?.SetEditMode(EditMode.SpaceMove);

            Initialize();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        private void Initialize()
        {
            baseAddress = null;
            lastLocation = null;
            movableAndDelta.Clear();
        }
    }
}
