using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class LoopParticle : MonoBehaviour
{
    //[SerializeField] uint seed;

    //ParticleSystem particle;

    //void Awake()
    //{
    //    particle = GetComponent<ParticleSystem>();
    //    var main = particle.main;

    //    main.stopAction = ParticleSystemStopAction.Callback;
    //    main.loop = false;

    //    particle.useAutoRandomSeed = false;
    //    particle.randomSeed = seed;
    //}

    //void OnParticleSystemStopped()
    //{
    //    particle.Play();
    //}

    [SerializeField] ParticleSystem ps;
    [SerializeField] uint seed = 12345;

    IEnumerator LoopSameParticle()
    {
        while (true)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.randomSeed = seed;
            ps.Play();

            yield return new WaitForSeconds(ps.main.duration);
        }
    }
}
