using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// プレビューノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_SpaceHoldRelayHidden : NoteObject<NoteData_SpaceHoldRelayHidden>
{
    NoteData_SpaceHoldRelayHidden noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_SpaceHoldRelayHidden data)
    {
        noteData = data;
    }
}
