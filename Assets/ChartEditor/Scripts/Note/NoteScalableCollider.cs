using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteScalableCollider : MonoBehaviour, IInteractableCollider, IScalableCollider
    {
        [SerializeField] SerializeInterface<IScalableObject> note;

        public EditMode EditMode => EditMode.Scale;

        public IScalableObject Note => note.Value;
    }

}