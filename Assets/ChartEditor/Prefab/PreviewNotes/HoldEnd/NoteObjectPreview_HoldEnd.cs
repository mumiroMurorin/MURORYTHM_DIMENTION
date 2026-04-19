using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// プレビューノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_HoldEnd : NoteObject<NoteData_HoldEnd>
{
    NoteData_HoldEnd noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_HoldEnd data)
    {
        noteData = data;
    }
}
