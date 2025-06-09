using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NoteObject<T> : MonoBehaviour, INoteVisibleSettable where T : INoteData
{
    public void Start()
    {
        SetVisible(false);
    }

    abstract public void Initialize(T data);

    virtual public void SetVisible(bool isVisible)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(isVisible);
        }
    }
}

