using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteDestroyableCollider : MonoBehaviour, IDestroyableCollider
    {
        [SerializeField] SerializeInterface<IDestroyableObject> note;

        IDestroyableObject IDestroyableCollider.Note => note.Value;
    }
}