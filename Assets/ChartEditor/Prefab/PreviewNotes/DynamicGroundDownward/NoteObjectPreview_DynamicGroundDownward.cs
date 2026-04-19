using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// プレビューノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_DynamicGroundDownward : NoteObject<NoteData_DynamicGroundDownward>
{
    NoteData_DynamicGroundDownward noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_DynamicGroundDownward data)
    {
        noteData = data;
    }
}
