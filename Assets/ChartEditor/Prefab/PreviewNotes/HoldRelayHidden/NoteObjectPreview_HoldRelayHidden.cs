using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// プレビューノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_HoldRelayHidden : NoteObject<NoteData_HoldRelayHidden>
{
    NoteData_HoldRelayHidden noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_HoldRelayHidden data)
    {
        noteData = data;
    }
}
