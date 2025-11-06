using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffectController_DynamicRightwardBack : MonoBehaviour, IInteractNoteEffectController<NoteData_DynamicGroundRightward>
{
    [SerializeField] GameObject rightObj;
    [SerializeField] GameObject leftObj;
    [SerializeField] List<ParticleSystem> particleSystems;
    [SerializeField] ParticleEndCallback rightParticleEndCallback;
    [SerializeField] ParticleEndCallback leftParticleEndCallback;

    public void SetEffect(NoteData_DynamicGroundRightward noteData)
    {
        bool isRight = false;
        bool isLeft = false;

        foreach(var i in noteData.Range)
        {
            if (i < 8) { isLeft = true; }
            if (i >= 8) { isRight = true; }
        }

        rightObj.SetActive(isRight);
        leftObj.SetActive(isLeft);

        if (isRight && rightParticleEndCallback != null) { rightParticleEndCallback.OnStopParticleListner += () => { Destroy(this.gameObject); }; }
        if (isLeft && leftParticleEndCallback != null) { leftParticleEndCallback.OnStopParticleListner += () => { Destroy(this.gameObject); }; }
    }

    public void Play()
    {
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Play();
        }
    }
}

