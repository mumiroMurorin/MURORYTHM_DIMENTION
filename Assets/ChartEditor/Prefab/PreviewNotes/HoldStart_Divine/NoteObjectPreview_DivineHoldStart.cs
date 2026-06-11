using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// Preview note for divine hold start.
/// </summary>
public class NoteObjectPreview_DivineHoldStart : NoteObject<NoteData_DivineHoldStart>
{
    NoteData_DivineHoldStart noteData;

    public override void Initialize(NoteData_DivineHoldStart data)
    {
        noteData = data;
    }
}
