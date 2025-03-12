using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class InteractNoteEffectController_DynamicDownward : MonoBehaviour , IInteractNoteEffectController<NoteData_DynamicGroundDownward>
    {
        [SerializeField] List<ParticleSystem> particleSystems;

        public void SetEffect(NoteData_DynamicGroundDownward noteData)
        {
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

        public void Play()
        {
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                particleSystem.Play();
            }
        }
    }

}
