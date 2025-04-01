using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteScalableCollider : MonoBehaviour, IInteractableCollider, IScalableCollider
    {
        [SerializeField] SerializeInterface<IScalableObject> note;

        EditMode editMode => EditMode.Scale;

        IScalableObject IScalableCollider.Note => note.Value;

        EditMode IInteractableCollider.GetEditMode()
        {
            return editMode;
        }
    }

}