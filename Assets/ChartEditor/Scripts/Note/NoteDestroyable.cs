using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class NoteDestroyable : MonoBehaviour, IDestroyableObject
    {
        [SerializeField] NoteObject noteObject;

        public NoteObject Note => noteObject;

        void IDestroyableObject.OnDestroy()
        {
            noteObject.Destroy();
        }
    }

}