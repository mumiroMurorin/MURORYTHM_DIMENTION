using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// プレビューノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_DynamicGroundLeftward : NoteObject<NoteData_DynamicGroundLeftward>
{
    NoteData_DynamicGroundLeftward noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_DynamicGroundLeftward data)
    {
        noteData = data;
    }
}
