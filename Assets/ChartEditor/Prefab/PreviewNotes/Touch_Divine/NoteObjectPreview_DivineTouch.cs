using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// プレビューノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_DivineTouch : NoteObject<NoteData_DivineTouch>
{
    NoteData_DivineTouch noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_DivineTouch data)
    {
        noteData = data;
    }
}
