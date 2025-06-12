using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteChangableCollider : MonoBehaviour, IChangableCollider
    {
        [SerializeField] SerializeInterface<IChangableObject> note;

        public EditMode EditMode => EditMode.ChangeType;

        public IChangableObject Note => note.Value;

    }
}