using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// プレビューノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_DynamicGroundUpward : NoteObject<NoteData_DynamicGroundUpward>
{
    NoteData_DynamicGroundUpward noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_DynamicGroundUpward data)
    {
        noteData = data;
    }
}
