using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteDeployableCollider : MonoBehaviour, IInteractableCollider, IDeployableCollider
    {
        EditMode editMode => EditMode.deploy;

        EditMode IInteractableCollider.GetEditMode()
        {
            return editMode;
        }
    }
}
