using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// プレビューノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_SpaceHoldRelay : NoteObject<NoteData_SpaceHoldRelay>
{
    NoteData_SpaceHoldRelay noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_SpaceHoldRelay data)
    {
        noteData = data;
    }
}
