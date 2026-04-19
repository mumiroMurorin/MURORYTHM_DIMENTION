using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// プレビューノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_HoldStart : NoteObject<NoteData_HoldStart>
{
    NoteData_HoldStart noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_HoldStart data)
    {
        noteData = data;
    }
}
