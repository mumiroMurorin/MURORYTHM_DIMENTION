using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NoteObject<T> : MonoBehaviour, INoteActivable where T : INoteData
{
    public void Start()
    {
        SetActive(false);
    }

    virtual public void SetActive(bool isVisible)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(isVisible);
        }
    }

    abstract public void Initialize(T data);

    /// <summary>
    /// エディタ中にノートスピードが変更された際など
    /// </summary>
    /// <param name="z"></param>
    public void SetPosition(float z)
    {
        this.transform.localPosition = new Vector3(
            this.transform.position.x,
            this.transform.position.y,
            z
            );
    }
}

