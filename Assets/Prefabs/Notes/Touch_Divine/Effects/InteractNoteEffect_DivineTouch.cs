using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class InteractNoteEffect_DivineTouch : InteractNoteEffect
{
    [SerializeField] MeshRenderer[] touchLigts;

    protected override void SetEffect(INoteData noteDataOrigin)
    {
        if (noteDataOrigin is not NoteData_DivineTouch noteData) { return; }

        // 光彩のセット
        foreach (int index in noteData.Range)
        {
            if (index < 0 || index >= touchLigts.Length) { continue; }
            MeshRenderer light = touchLigts[index];
            if (light == null) { continue; }

            light.gameObject.SetActive(true);
            Material material = light.material;
            DOTween.Kill(material);
            Color color = material.color;
            color.a = 1f;
            material.color = color;
            material.DOFade(0, 0.4f).SetEase(Ease.InCubic);
        }

        // パーティクルのセット
        foreach (var particle in particles)
        {
            particle.ApplyGroundShapeSetting(noteData.Range[0], noteData.Range.Length);
            particle.ApplyGroundEmissionSetting(noteData.Range.Length);
        }
    }
}

