using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace TransitionerInSelectScene
{
    public class Transitioner_MusicSelect : IPhaseTransitionerInSelectScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitionable;

        readonly PhaseStatusInSelectScene status = PhaseStatusInSelectScene.MusicSelect;

        bool IPhaseTransitionerInSelectScene.ConditionChecker(PhaseStatusInSelectScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInSelectScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"MusicSelect\"");

        }
    }
}
