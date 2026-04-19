using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// プレビューノーツにアタッチされるクラス
/// </summary>
public class NoteObjectPreview_HoldEndUnjudge : NoteObject<NoteData_HoldEndUnjudge>
{
    NoteData_HoldEndUnjudge noteData;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_HoldEndUnjudge data)
    {
        noteData = data;
    }
}
