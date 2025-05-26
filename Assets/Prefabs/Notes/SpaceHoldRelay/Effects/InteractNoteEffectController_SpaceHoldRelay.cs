using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class InteractNoteEffectController_SpaceHoldRelay : MonoBehaviour, IInteractNoteEffectController<NoteData_SpaceHoldRelay>
{
    [SerializeField] float distanceUnit = 1f;
    [SerializeField] List<ParticleSystem> particleSystems;

    public void SetEffect(NoteData_SpaceHoldRelay noteData)
    {
        // パーティクルのセット
        foreach (ParticleSystem particle in particleSystems)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Shapeモジュール
            var shape = particle.shape;
            shape.shapeType = ParticleSystemShapeType.Mesh;
            shape.meshShapeType = ParticleSystemMeshShapeType.Edge;
            shape.mesh = noteData.Mesh;

            // Emissionモジュール
            var emission = particle.emission;
            ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
            emission.GetBursts(bursts);

            for (int i = 0; i < bursts.Length; i++)
            {
                bursts[i].count = new ParticleSystem.MinMaxCurve(bursts[i].count.constant * (noteData.Vertices.Distance() / distanceUnit));
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

