using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffect_DynamicDownward : InteractNoteEffect
{
    protected override void SetEffect(INoteData noteDataOrigin)
    {
        if (noteDataOrigin is not NoteData_DynamicGroundDownward noteData) { return; }

        foreach (var particle in particles)
        {
            particle.ApplyGroundShapeSetting(noteData.Range[0], noteData.Range.Length);
            particle.ApplyGroundEmissionSetting(noteData.Range.Length);
        }
    }
}

