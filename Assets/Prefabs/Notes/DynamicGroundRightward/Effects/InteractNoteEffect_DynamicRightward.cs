using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffect_DynamicRightward : InteractNoteEffect
{
    protected override void SetEffect(INoteData noteDataOrigin)
    {
        if (noteDataOrigin is not NoteData_DynamicGroundRightward noteData) { return; }

        // パーティクルのセット
        foreach (var particle in particles)
        {
            particle.ApplyGroundShapeSetting(noteData.Range[0], noteData.Range.Length);
            particle.ApplyGroundEmissionSetting(noteData.Range.Length);
        }
    }
}

