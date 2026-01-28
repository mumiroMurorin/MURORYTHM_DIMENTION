using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class InteractNoteEffect_HoldRelay : InteractNoteEffect
{
    [SerializeField] MeshRenderer[] HoldRelayLights;

    protected override void SetEffect(INoteData noteDataOrigin)
    {
        if (noteDataOrigin is not NoteData_HoldRelay noteData) { return; }

        // 光彩のセット
        foreach (int index in noteData.Range)
        {
            if (index > HoldRelayLights.Length) { continue; }
            HoldRelayLights[index]?.gameObject.SetActive(true);
            HoldRelayLights[index]?.material.DOFade(0, 0.4f).SetEase(Ease.InCubic);
        }

        // パーティクルのセット
        foreach (var particle in particles)
        {
            particle.ApplyGroundShapeSetting(noteData.Range[0], noteData.Range.Length);
            particle.ApplyGroundEmissionSetting(noteData.Range.Length);
        }
    }

}

