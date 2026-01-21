using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace TransitionerInSelectScene
{
    public class Transitioner_DetailSelect : IPhaseTransitionerInSelectScene
    {


        [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IInputHandler> inputHandler;

        readonly PhaseStatusInSelectScene status = PhaseStatusInSelectScene.DetailSelect;

        bool IPhaseTransitionerInSelectScene.ConditionChecker(PhaseStatusInSelectScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInSelectScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"DetailSelect\"");


        }
    }
}
