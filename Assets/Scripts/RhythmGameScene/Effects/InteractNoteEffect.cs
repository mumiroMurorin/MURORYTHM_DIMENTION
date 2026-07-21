using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class InteractNoteEffect : MonoBehaviour
{
    [SerializeField] ParticleEndCallback[] particleEndCallback;
    [SerializeField] protected ParticleSystemToSetting[] particles;

    Action OnFinishParticle;
    bool isReturnedToPool;

    private void Awake()
    {
        if (particleEndCallback != null)
        {
            foreach (var c in particleEndCallback)
            {
                c.OnStopParticleListner += OnStopParticle;
            }
        }
    }

    private void OnDestroy()
    {
        if (particleEndCallback != null)
        {
            foreach (var c in particleEndCallback)
            {
                c.OnStopParticleListner -= OnStopParticle;
            }
        }
    }

    public void SetEffect(INoteData noteData, Action returnToPool)
    {
        OnFinishParticle = returnToPool;
        isReturnedToPool = false;

        SetEffect(noteData);
    }

    private void OnStopParticle()
    {
        // 複数のParticleEndCallbackがある場合でも、プールへの返却は一度だけにする
        if (isReturnedToPool) { return; }

        isReturnedToPool = true;
        OnFinishParticle?.Invoke();
    }

    protected abstract void SetEffect(INoteData noteDataOrigin);

    public void Play()
    {
        foreach (var p in particles)
        {
            p.Play();
        }

        AfterPlay();
    }

    protected virtual void AfterPlay() { }

    [System.Serializable]
    public class ParticleSystemToSetting
    {
        const float ANGLE_UNIT = 11.25f;

        [SerializeField] ParticleSystem particle;
        [SerializeField] float emissionConstantCount;
        [SerializeField] float distanceUnit = 1f;

        /// <summary>
        /// グラウンドノーツに対してのエフェクト設定 (emission)
        /// </summary>
        /// <param name="laneLength"></param>
        public void ApplyGroundEmissionSetting(int laneLength)
        {
            var emission = particle.emission;
            ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
            emission.GetBursts(bursts);

            for (int i = 0; i < bursts.Length; i++)
            {
                bursts[i].count = new ParticleSystem.MinMaxCurve(emissionConstantCount * laneLength);
            }

            emission.SetBursts(bursts);
        }

        /// <summary>
        /// グラウンドノーツに対してのエフェクト設定 (shape)
        /// </summary>
        /// <param name="laneLength"></param>

        public void ApplyGroundShapeSetting(int startIndex, int laneLength)
        {
            // Shapeモジュール
            var shape = particle.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.rotation = new Vector3(0, 0, 180f + startIndex * ANGLE_UNIT);   // 角度の変更
            shape.arc = laneLength * 11.25f; // 長さの変更
        }

        /// <summary>
        /// スペースノーツに対してのエフェクト設定 (emission)
        /// </summary>
        /// <param name="verticesLength"></param>
        public void ApplySpaceEmissionSetting(float verticesLength)
        {
            // Emissionモジュール
            var emission = particle.emission;
            ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
            emission.GetBursts(bursts);

            for (int i = 0; i < bursts.Length; i++)
            {
                bursts[i].count = new ParticleSystem.MinMaxCurve(emissionConstantCount * (verticesLength / distanceUnit));
            }

            emission.SetBursts(bursts);
        }

        /// <summary>
        /// スペースノーツに対してのエフェクト設定 (shape)
        /// </summary>
        /// <param name="mesh"></param>
        public void ApplySpaceShapeSetting(Mesh mesh)
        {
            // Shapeモジュール
            var shape = particle.shape;
            shape.shapeType = ParticleSystemShapeType.Mesh;
            shape.meshShapeType = ParticleSystemMeshShapeType.Edge;
            shape.mesh = mesh;
        }

        public void Play()
        {
            particle?.Play();
        }

        public void Stop()
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
