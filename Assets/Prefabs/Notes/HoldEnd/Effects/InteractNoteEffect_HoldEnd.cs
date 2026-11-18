using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class InteractNoteEffect_HoldEnd : InteractNoteEffect
{
    [SerializeField] MeshRenderer[] HoldEndLights;

    // 光ごとの元の不透明度。
    // フェードで 0 にした後、プールから再利用したときに元の明るさへ戻すために覚えておく
    readonly Dictionary<MeshRenderer, float> originalAlphas = new Dictionary<MeshRenderer, float>();

    protected override void SetEffect(INoteData noteDataOrigin)
    {
        if (noteDataOrigin is not NoteData_HoldEnd noteData) { return; }

        // 光彩のセット
        foreach (int index in noteData.Range)
        {
            if (index < 0 || index >= HoldEndLights.Length) { continue; }
            MeshRenderer light = HoldEndLights[index];
            if (light == null) { continue; }

            light.gameObject.SetActive(true);

            Material material = light.material;
            DOTween.Kill(material);

            // 初回はマテリアル本来の不透明度を覚え、2回目以降はその値に戻してからフェードする
            if (!originalAlphas.TryGetValue(light, out float originalAlpha))
            {
                originalAlpha = material.color.a;
                originalAlphas[light] = originalAlpha;
            }

            Color color = material.color;
            color.a = originalAlpha;
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
