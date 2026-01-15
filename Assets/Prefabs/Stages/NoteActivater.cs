using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteActivater : MonoBehaviour
{
    /// <summary>
    /// 範囲内に入ってきたノートをアクティブ化する
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.TryGetComponent(out INoteActivable activable)) { return; }

        activable.SetActive(true);
    }

    /// <summary>
    /// 範囲外に出て行ったノートを非アクティブ化する
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.TryGetComponent(out INoteActivable activable)) { return; }

        activable.SetActive(false);
    }
}