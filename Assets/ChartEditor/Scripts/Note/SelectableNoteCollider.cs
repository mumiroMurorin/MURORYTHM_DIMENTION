using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class SelectableNoteCollider : MonoBehaviour, ISelectableNoteCollider
    {
        [SerializeField] SerializeInterface<ISelectableNoteObject> selectableObject;

        ISelectableNoteObject ISelectableNoteCollider.SelectableObject => selectableObject.Value;

        EditMode IInteractableCollider.EditMode => EditMode.None;
    }

}