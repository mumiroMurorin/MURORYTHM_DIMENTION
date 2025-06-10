using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteSpaceEditableCollider : MonoBehaviour, ISpaceEditableCollider
    {
        [SerializeField] SerializeInterface<ISpaceEditableObject> note;

        public ISpaceEditableObject Note => note.Value;
    }

}