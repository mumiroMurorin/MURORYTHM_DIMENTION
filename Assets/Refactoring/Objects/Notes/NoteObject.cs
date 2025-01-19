using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public abstract class NoteObject<T> : MonoBehaviour where T : INoteData
    {
        abstract public void Initialize(T data);
    }

}
