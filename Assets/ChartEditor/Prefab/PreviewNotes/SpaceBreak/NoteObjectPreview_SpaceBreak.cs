using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using static JudgementUtil.SpacaHold.SpaceHoldJudgement;

public class NoteObjectPreview_SpaceBreak : NoteObject<NoteData_SpaceBreak>
{
    NoteData_SpaceBreak noteData;

    /// <summary>
    /// èâä˙âª
    /// </summary>
    /// <param name="data"></param>
    public override void Initialize(NoteData_SpaceBreak data)
    {
        noteData = data;
    }
}
