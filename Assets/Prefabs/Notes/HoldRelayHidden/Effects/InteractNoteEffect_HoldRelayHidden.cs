using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class InteractNoteEffectr_HoldRelayHidden : InteractNoteEffect
{
    [SerializeField] MeshRenderer[] HoldRelayHiddenLights;

    protected override void SetEffect(INoteData noteDataOrigin)
    {
        if (noteDataOrigin is not NoteData_HoldRelayHidden noteData) { return; }

        // 光彩のセット
        foreach (int index in noteData.Range)
        {
            if (index > HoldRelayHiddenLights.Length) { continue; }
            HoldRelayHiddenLights[index]?.gameObject.SetActive(true);
            HoldRelayHiddenLights[index]?.material.DOFade(0, 0.4f).SetEase(Ease.InCubic);
        }

        // パーティクルのセット
        foreach (var particle in particles)
        {
            particle.ApplyGroundShapeSetting(noteData.Range[0], noteData.Range.Length);
            particle.ApplyGroundEmissionSetting(noteData.Range.Length);
        }
    }
}

