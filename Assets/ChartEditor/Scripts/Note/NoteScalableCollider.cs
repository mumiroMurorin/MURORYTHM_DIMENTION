using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteScalableCollider : MonoBehaviour, IInteractableCollider, IScalableCollider
    {
        [SerializeField] SerializeInterface<IScalableObject> note;
        [SerializeField] bool isRightEdge;

        public EditMode EditMode => EditMode.Scale;

        public IScalableObject Note => note.Value;

        public bool IsRightEdge => isRightEdge;
    }

}