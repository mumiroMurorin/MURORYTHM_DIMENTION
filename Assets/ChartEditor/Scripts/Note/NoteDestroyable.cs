using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class NoteDestroyable : MonoBehaviour, IDestroyableObject
    {
        NoteObject noteObject;

        private void Start()
        {
            noteObject = GetComponent<NoteObject>();
        }

        void IDestroyableObject.OnDestroy()
        {
            noteObject.NoteData = null;
            noteObject.Destroy();
        }
    }

}