using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class InteractNoteEffectController_DivineTouch : MonoBehaviour, IInteractNoteEffectController<NoteData_DivineTouch>
{
    [SerializeField] List<ParticleSystem> particleSystems;
    [SerializeField] MeshRenderer[] touchLigts;
    [SerializeField] ParticleEndCallback particleEndCallback;

    public void SetEffect(NoteData_DivineTouch noteData)
    {
        // 光彩のセット
        foreach (int index in noteData.Range)
        {
            if (index > touchLigts.Length) { continue; }
            touchLigts[index]?.gameObject.SetActive(true);
            touchLigts[index]?.material.DOFade(0, 0.4f).SetEase(Ease.InCubic);
        }

        // パーティクルのセット
        foreach (ParticleSystem particle in particleSystems)
        {
            // Shapeモジュール
            var shape = particle.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.rotation = new Vector3(0, 0, 180f + noteData.Range[0] * 11.25f);   // 角度の変更
            shape.arc = noteData.Range.Length * 11.25f; // 長さの変更

            // Emissionモジュール
            var emission = particle.emission;
            ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
            emission.GetBursts(bursts);

            for (int i = 0; i < bursts.Length; i++)
            {
                bursts[i].count = new ParticleSystem.MinMaxCurve(bursts[i].count.constant * noteData.Range.Length);
            }

            emission.SetBursts(bursts);

        }

        if (particleEndCallback != null) { particleEndCallback.OnStopParticleListner += () => { Destroy(this.gameObject); }; }

    }

    public void Play()
    {
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Play();
        }
    }
}

