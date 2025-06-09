using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteVisibler : MonoBehaviour
{
    /// <summary>
    /// レンジに入ってきたノーツを可視化する
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if(!other.gameObject.TryGetComponent(out INoteVisibleSettable settable)) { return; }

        Debug.Log("Visible!");
        settable.SetVisible(true);
    }

    /// <summary>
    /// レンジから出たノーツを不可視化する
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.TryGetComponent(out INoteVisibleSettable settable)) { return; }

        Debug.Log("Invisible!");
        settable.SetVisible(false);
    }
}
