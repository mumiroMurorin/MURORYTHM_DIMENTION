using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteDestroyableCollider : MonoBehaviour, IDestroyableCollider
    {
        [SerializeField] SerializeInterface<IDestroyableObject> note;

        public EditMode EditMode => EditMode.Destroy;

        public IDestroyableObject Note => note.Value;
    }
}