using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// スペースノーツの移動 (分線方向にのみ動かせる)
    /// </summary>
    public class SpaceMover : NoteMoverBase
    {
        [SerializeField] SerializeInterface<ICursorInteracter> cursorInteracter;

        protected override EditMode IdleMode => EditMode.SpaceMove;

        protected override EditMode DraggingMode => EditMode.SpaceMoving;

        protected override bool TryGetClickedNote(out NoteObject note)
        {
            var collider = dataGetter.GetInteractableCollider<IFreedomMovableCollider>();
            note = collider?.Note?.Note;
            return note != null;
        }

        protected override bool TryCreateTarget(NoteObject note, out MoveTarget target)
        {
            target = null;
            if (!note.TryGetComponent(out IFreedomMovableObject movable)) { return false; }

            target = new MoveTarget(note, movable.OnMoveStart, movable.OnMove, movable.OnMoveEnd);
            return true;
        }

        protected override bool TryGetDropLocation(out object location, out AddressInChart address)
        {
            var deployable = dataGetter.GetInteractableCollider<IFreedomDeployableCollider>();
            location = deployable;
            address = deployable?.Address;
            return deployable != null;
        }

        protected override AddressWithinRange CalcAddress(MoveTarget target, AddressInChart dropAddress)
        {
            var newAddress = dataGetter.ChartData.Value.AddressAddition(dropAddress, target.SubDivisionDelta);

            return new AddressWithinRange(newAddress, 1);
        }
    }
}
