using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace TransitionerInSelectScene
{
    public class Transitioner_DetailSelectUnstartable : IPhaseTransitionerInSelectScene
    {
        readonly PhaseStatusInSelectScene status = PhaseStatusInSelectScene.DetailSelect_UnStartable;

        bool IPhaseTransitionerInSelectScene.ConditionChecker(PhaseStatusInSelectScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInSelectScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"DetailSelectUnstartable\"");


        }
    }
}
