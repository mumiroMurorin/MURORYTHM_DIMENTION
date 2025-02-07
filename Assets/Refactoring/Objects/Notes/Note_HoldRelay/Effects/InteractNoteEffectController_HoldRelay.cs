using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Refactoring
{
    public class InteractNoteEffectController_HoldRelay : MonoBehaviour , IInteractNoteEffectController<NoteData_HoldRelay>
    {
        [SerializeField] List<ParticleSystem> particleSystems;
        [SerializeField] MeshRenderer[] HoldRelayLights;

        public void SetEffect(NoteData_HoldRelay noteData)
        {
            // 光彩のセット
            foreach(int index in noteData.Range)
            {
                if (index > HoldRelayLights.Length) { continue; }
                HoldRelayLights[index]?.gameObject.SetActive(true);
                HoldRelayLights[index]?.material.DOFade(0, 0.4f).SetEase(Ease.InCubic);
            }
            
            // パーティクルのセット
            foreach(ParticleSystem particle in particleSystems)
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
                    bursts[i].count = new ParticleSystem.MinMaxCurve(
                         bursts[i].count.constantMin * noteData.Range.Length,
                         bursts[i].count.constantMax * noteData.Range.Length
                    );
                }

                emission.SetBursts(bursts);

            }
        }
    }

}
