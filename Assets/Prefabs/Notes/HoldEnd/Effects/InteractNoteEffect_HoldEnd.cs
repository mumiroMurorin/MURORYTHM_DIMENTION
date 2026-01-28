using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class InteractNoteEffect_HoldEnd : InteractNoteEffect
{
    [SerializeField] MeshRenderer[] HoldEndLights;

    protected override void SetEffect(INoteData noteDataOrigin)
    {
        if (noteDataOrigin is not NoteData_HoldEnd noteData) { return; }

        // 光彩のセット
        foreach (int index in noteData.Range)
        {
            if (index > HoldEndLights.Length) { continue; }
            HoldEndLights[index]?.gameObject.SetActive(true);
            HoldEndLights[index]?.material.DOFade(0, 0.4f).SetEase(Ease.InCubic);
        }

        // パーティクルのセット
        foreach (var particle in particles)
        {
            particle.ApplyGroundShapeSetting(noteData.Range[0], noteData.Range.Length);
            particle.ApplyGroundEmissionSetting(noteData.Range.Length);
        }
    }
}

