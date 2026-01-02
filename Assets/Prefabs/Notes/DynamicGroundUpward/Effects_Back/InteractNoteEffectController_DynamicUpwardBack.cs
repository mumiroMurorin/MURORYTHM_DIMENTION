using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffectController_DynamicUpwardBack : MonoBehaviour, IInteractNoteEffectController<NoteData_DynamicGroundUpward>
{
    [SerializeField] List<ParticleSystem> particleSystems;
    [SerializeField] ParticleEndCallback particleEndCallback;

    public void SetEffect(NoteData_DynamicGroundUpward noteData)
    {

    }

    public void Play()
    {
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Play();
        }
    }
}

