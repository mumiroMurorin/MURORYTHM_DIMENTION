using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffect_DynamicUpwardBack : InteractNoteEffect
{
    protected override void SetEffect(INoteData noteDataOrigin)
    {
        if (noteDataOrigin is not NoteData_DynamicGroundUpward noteData) { return; }

    }

}

