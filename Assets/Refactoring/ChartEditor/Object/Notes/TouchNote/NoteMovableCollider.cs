using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteMovableCollider : MonoBehaviour, IInteractableCollider
    {
        EditMode editMode => EditMode.move;

        EditMode IInteractableCollider.GetEditMode()
        {
            return editMode;
        }
    }

}