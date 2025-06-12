using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class SpaceNoteMovableCollider : MonoBehaviour, IFreedomMovableCollider
    {
        [SerializeField] SerializeInterface<IFreedomMovableObject> note;

        public EditMode EditMode => EditMode.SpaceMove;

        public IFreedomMovableObject Note => note.Value;

    }
}