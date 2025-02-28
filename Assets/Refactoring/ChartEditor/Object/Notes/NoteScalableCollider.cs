using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteScalableCollider : MonoBehaviour, IInteractableCollider
    {
        EditMode editMode => EditMode.scale;

        EditMode IInteractableCollider.GetEditMode()
        {
            return editMode;
        }
    }

}