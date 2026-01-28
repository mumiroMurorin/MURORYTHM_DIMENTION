using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class InteractNoteEffect_Touch : InteractNoteEffect
{
    [SerializeField] MeshRenderer[] touchLigts;

    protected override void SetEffect(INoteData noteDataOrigin)
    {
        if (noteDataOrigin is not NoteData_Touch noteData) { return; }

        // 光彩のセット
        foreach (int index in noteData.Range)
        {
            if (index > touchLigts.Length) { continue; }
            touchLigts[index]?.gameObject.SetActive(true);
            touchLigts[index]?.material.DOFade(0, 0.4f).SetEase(Ease.InCubic);
        }

        // パーティクルのセット
        foreach (var particle in particles)
        {
            particle.ApplyGroundShapeSetting(noteData.Range[0], noteData.Range.Length);
            particle.ApplyGroundEmissionSetting(noteData.Range.Length);
        }
       
    }
}

