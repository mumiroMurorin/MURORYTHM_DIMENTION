using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ParticleEndCallback : MonoBehaviour
{
    public Action OnStopParticleListner { get; set; }

    void OnParticleSystemStopped()
    {
        OnStopParticleListner?.Invoke();
    }
}
