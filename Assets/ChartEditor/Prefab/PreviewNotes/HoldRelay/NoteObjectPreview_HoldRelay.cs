using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// プレビューノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_HoldRelay : NoteObject<NoteData_HoldRelay>
{
    NoteData_HoldRelay noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_HoldRelay data)
    {
        noteData = data;
    }
}
