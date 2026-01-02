using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractNoteEffectController_DynamicDownwardBack : MonoBehaviour, IInteractNoteEffectController<NoteData_DynamicGroundDownward>
{
    [SerializeField] List<ParticleSystem> particleSystems;
    [SerializeField] ParticleEndCallback particleEndCallback;

    public void SetEffect(NoteData_DynamicGroundDownward noteData)
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

