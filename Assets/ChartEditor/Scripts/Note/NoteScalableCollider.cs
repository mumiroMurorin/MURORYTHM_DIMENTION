using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteScalableCollider : MonoBehaviour, IInteractableCollider, IScalableCollider
    {
        [SerializeField] SerializeInterface<IScalableObject> note;

        public EditMode EditMode => EditMode.Scale;

        IScalableObject IScalableCollider.Note => note.Value;
    }

}