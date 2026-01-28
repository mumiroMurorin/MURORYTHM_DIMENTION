using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffectController_DynamicRightwardBack : InteractNoteEffectController
{
    [SerializeField] GameObject rightObj;
    [SerializeField] GameObject leftObj;

    protected override void SetEffect(INoteData noteDataOrigin)
    {
        if (noteDataOrigin is not NoteData_DynamicGroundRightward noteData) { return; }

        bool isRight = false;
        bool isLeft = false;

        foreach(var i in noteData.Range)
        {
            if (i < 8) { isLeft = true; }
            if (i >= 8) { isRight = true; }
        }

        rightObj.SetActive(isRight);
        leftObj.SetActive(isLeft);
    }
}

