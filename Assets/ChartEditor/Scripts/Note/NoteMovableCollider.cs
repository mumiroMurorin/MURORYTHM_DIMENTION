using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteMovableCollider : MonoBehaviour, IInteractableCollider, IMovableCollider
    {
        [SerializeField] SerializeInterface<IMovableObject> note;

        public EditMode EditMode => EditMode.Move;

        public IMovableObject Note => note.Value;

    }
}