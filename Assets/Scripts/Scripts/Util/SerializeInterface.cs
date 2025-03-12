using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializeInterface<T> where T : class
{
    [SerializeField] GameObject obj;
    T value;
    bool notAssinged = false;

    public T Value
    {
        get
        {
            if (notAssinged || obj == null)
            {
                return null;
            }
            else
            {
                if (obj.TryGetComponent(out value))
                {
                    return value;
                }
                else
                {
                    notAssinged = true;
                    return null;
                }
            }
        }
        set
        {
            this.value = value;
        }
    }
}
