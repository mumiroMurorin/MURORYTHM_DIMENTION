using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class InteractNoteEffectController_DynamicUpward : MonoBehaviour , IInteractNoteEffectController<NoteData_DynamicGroundUpward>
    {
        [SerializeField] List<ParticleSystem> particleSystems;

        public void SetEffect(NoteData_DynamicGroundUpward noteData)
        {
            foreach(ParticleSystem particle in particleSystems)
            {
                // Shape���W���[��
                var shape = particle.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.rotation = new Vector3(0, 0, 180f + noteData.Range[0] * 11.25f);   // �p�x�̕ύX
                shape.arc = noteData.Range.Length * 11.25f; // �����̕ύX

                // Emission���W���[��
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
