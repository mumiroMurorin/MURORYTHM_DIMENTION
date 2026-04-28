using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class InteractNoteEffect_SpaceHoldRelayHidden : InteractNoteEffect
{
    protected override void SetEffect(INoteData noteDataOrigin)
    {
        if (noteDataOrigin is not NoteData_SpaceHoldRelayHidden noteData) { return; }

        // パーティクルのセット
        foreach (var particle in particles)
        {
            particle.Stop();

            particle.ApplySpaceShapeSetting(noteData.Mesh);
            particle.ApplySpaceEmissionSetting(noteData.Vertices.Distance());
        }
    }
}

