using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// タッチノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_Touch : NoteObject<NoteData_Touch>
{
    NoteData_Touch noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_Touch data)
    {
        noteData = data;
    }
}