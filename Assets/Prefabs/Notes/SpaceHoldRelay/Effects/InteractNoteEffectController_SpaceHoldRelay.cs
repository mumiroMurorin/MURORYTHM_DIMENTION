using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class InteractNoteEffectController_SpaceHoldRelay : InteractNoteEffectController
{
    [SerializeField] float distanceUnit = 1f;

    protected override void SetEffect(INoteData noteDataOrigin)
    {
        if (noteDataOrigin is not NoteData_SpaceHoldRelay noteData) { return; }

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
}

