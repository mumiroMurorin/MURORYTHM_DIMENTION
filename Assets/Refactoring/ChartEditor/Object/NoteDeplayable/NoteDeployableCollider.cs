using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteDeployableCollider : MonoBehaviour, IInteractableCollider
    {
        EditMode editMode => EditMode.modify;

        EditMode IInteractableCollider.GetEditMode()
        {
            return editMode;
        }
    }
}
