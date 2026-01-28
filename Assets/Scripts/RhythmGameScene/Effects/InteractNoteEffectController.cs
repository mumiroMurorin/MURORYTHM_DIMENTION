using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class InteractNoteEffectController : MonoBehaviour, IInteractNoteEffectController
{
    [SerializeField] ParticleEndCallback particleEndCallback;
    [SerializeField] protected List<ParticleSystem> particleSystems;
    [SerializeField] protected ParticleSystemToSetting[] particles;

    public void SetEffect(INoteData noteData, Action<IInteractNoteEffectController> returnToPool)
    {
        if (particleEndCallback != null)
        {
            particleEndCallback.OnStopParticleListner += () =>
            {
                this.gameObject.SetActive(false);
                returnToPool(this);
            };
        }

        SetEffect(noteData);
    }

    protected abstract void SetEffect(INoteData noteDataOrigin);

  　public void SetTransform(Vector3 pos, Quaternion rotation)
    {
        this.gameObject.transform.position = pos;
        this.gameObject.transform.rotation = rotation;
    }

    public void Play()
    {
        this.gameObject.SetActive(true);
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Play();
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
    }

}
