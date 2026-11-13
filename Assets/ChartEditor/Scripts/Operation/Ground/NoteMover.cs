using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// グラウンドノーツの移動 (分線とレーンの両方向に動かせる)
    /// </summary>
    public class NoteMover : NoteMoverBase
    {
        [Header("レーン(ノーツ)1マス分の横幅")]
        [SerializeField] float noteUnitWidth = 1f;
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        // 掴んだ位置とノートの左端とのレーン数の差
        int pointerToAxisDelta;

        protected override EditMode IdleMode => EditMode.Move;

        protected override EditMode DraggingMode => EditMode.Moving;

        protected override bool TryGetClickedNote(out NoteObject note)
        {
            var collider = dataGetter.GetInteractableCollider<IMovableCollider>();
            note = collider?.Note?.Note;
            return note != null;
        }

        protected override bool TryCreateTarget(NoteObject note, out MoveTarget target)
        {
            target = null;
            if (!note.TryGetComponent(out IMovableObject movable)) { return false; }

            target = new MoveTarget(note, movable.OnMoveStart, movable.OnMove, movable.OnMoveEnd);
            return true;
        }

        protected override bool TryGetDropLocation(out object location, out AddressInChart address)
        {
            var deployable = dataGetter.GetInteractableCollider<IDeployableCollider>();
            location = deployable;
            address = deployable?.Address;
            return deployable != null;
        }

        protected override void OnStartDrag(NoteObject clickedNote)
        {
            // マウスカーソルからノーツの軸までの差を算出
            float deltaX = cursorInteracter.Value.GetWorldPositionUnderCursor().x - clickedNote.transform.position.x;
            pointerToAxisDelta = (int)((deltaX + noteUnitWidth / 2f) / noteUnitWidth);
        }

        protected override AddressWithinRange CalcAddress(MoveTarget target, AddressInChart dropAddress)
        {
            var newAddress = dataGetter.ChartData.Value.AddressAddition(dropAddress, target.SubDivisionDelta);
            newAddress.SetSliderIndex(dropAddress.SliderIndex + target.SliderDelta - pointerToAxisDelta);

            return new AddressWithinRange(newAddress, target.RangeCount);
        }

        /// <summary>
        /// ノートを指定されたアドレスに移動させる
        /// </summary>
        /// <param name="movableObject"></param>
        /// <param name="newAddress"></param>
        public void MoveNote(IMovableObject movableObject, AddressWithinRange newAddress)
        {
            var noteData = movableObject.Note.NoteData;
            noteData.SetAddress(newAddress);
            movableObject.OnMove();
        }
    }
}
