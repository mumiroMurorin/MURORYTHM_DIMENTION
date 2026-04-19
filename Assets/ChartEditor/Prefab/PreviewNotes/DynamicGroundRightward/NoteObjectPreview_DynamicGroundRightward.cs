using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// プレビューノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_DynamicGroundRightward : NoteObject<NoteData_DynamicGroundRightward>
{
    NoteData_DynamicGroundRightward noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_DynamicGroundRightward data)
    {
        noteData = data;
    }
}
