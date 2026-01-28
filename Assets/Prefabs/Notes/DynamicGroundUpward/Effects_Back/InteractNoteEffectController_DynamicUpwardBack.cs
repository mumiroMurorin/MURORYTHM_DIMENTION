using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffectController_DynamicUpwardBack : InteractNoteEffectController
{
    protected override void SetEffect(INoteData noteDataOrigin)
    {
        if (noteDataOrigin is not NoteData_DynamicGroundUpward noteData) { return; }

    }

}

