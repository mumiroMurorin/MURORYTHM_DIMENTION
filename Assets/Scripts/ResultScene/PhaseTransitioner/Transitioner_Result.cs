using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring.TransitionerInResultScene
{
    public class Transitioner_Result : IPhaseTransitionerInResultScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInResultScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IInputHandler> inputHandler;

        readonly PhaseStatusInResultScene status = PhaseStatusInResultScene.Result;

        bool IPhaseTransitionerInResultScene.ConditionChecker(PhaseStatusInResultScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInResultScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"Result\"");

        }

    }
}
