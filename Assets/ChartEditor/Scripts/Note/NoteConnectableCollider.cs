using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteConnectableCollider : MonoBehaviour, IConnectableCollider
    {
        [SerializeField] SerializeInterface<IConnectableObject> note;

        public EditMode EditMode => EditMode.None;

        public IConnectableObject Note => note.Value;

    }
}