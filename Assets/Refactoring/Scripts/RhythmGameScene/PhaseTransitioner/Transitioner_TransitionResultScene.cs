using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring.TransitionerInRhythmGameScene
{
    public class Transitioner_TransitionResultScene : IPhaseTransitionerInRhythmGameScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInRhythmGameScene> phaseTransitionable;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.TransitionResultScene;

        bool IPhaseTransitionerInRhythmGameScene.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRhythmGameScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"TransitionResultScene\"");

        }
    }

}
