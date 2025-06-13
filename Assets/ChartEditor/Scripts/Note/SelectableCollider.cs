using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class SelectableCollider : MonoBehaviour, ISelectableVertexCollider
    {
        [SerializeField] SerializeInterface<ISelectableVertexObject> selectableObject;

        ISelectableVertexObject ISelectableVertexCollider.SelectableObject => selectableObject.Value;

        EditMode IInteractableCollider.EditMode => EditMode.Select;
    }

}