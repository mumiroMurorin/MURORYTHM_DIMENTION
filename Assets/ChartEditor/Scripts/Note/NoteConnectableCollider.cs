using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteConnectableCollider : MonoBehaviour, IInteractableCollider, IConnectableCollider
    {
        [SerializeField] SerializeInterface<IConnectableObject> note;

        public EditMode EditMode => EditMode.Connect;

        public IConnectableObject Note => note.Value;

    }
}