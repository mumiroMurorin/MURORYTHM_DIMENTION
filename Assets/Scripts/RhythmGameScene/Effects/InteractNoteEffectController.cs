using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class InteractNoteEffectController : MonoBehaviour, IInteractNoteEffectController
{
    [SerializeField] ParticleEndCallback particleEndCallback;
    [SerializeField] protected List<ParticleSystem> particleSystems;

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

  Å@public void SetTransform(Vector3 pos, Quaternion rotation)
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
}
