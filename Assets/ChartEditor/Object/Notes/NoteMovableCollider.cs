using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteMovableCollider : MonoBehaviour, IInteractableCollider, IMovableCollider
    {
        [SerializeField] SerializeInterface<IMovableObject> note;

        EditMode editMode => EditMode.Move;

        IMovableObject IMovableCollider.Note => note.Value;

        EditMode IInteractableCollider.GetEditMode()
        {
            return editMode;
        }
    }
}