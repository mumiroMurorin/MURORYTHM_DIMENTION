using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteDeployableCollider : MonoBehaviour, IInteractableCollider, IDeployableCollider
    {
        EditMode editMode => EditMode.Deploy;

        EditMode IInteractableCollider.GetEditMode()
        {
            return editMode;
        }
    }
}
