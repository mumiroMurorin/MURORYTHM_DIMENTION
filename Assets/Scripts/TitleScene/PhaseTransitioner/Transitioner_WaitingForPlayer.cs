using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;

namespace TransitionerInTitleScene
{
    public class Transitioner_WaitingForPlayer : IPhaseTransitionerInTitleScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInTitleScene> phaseTransitionable;

        readonly PhaseStatusInTitleScene status = PhaseStatusInTitleScene.WaitingForPlayer;

        CancellationTokenSource cts;

        bool IPhaseTransitionerInTitleScene.ConditionChecker(PhaseStatusInTitleScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTitleScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"WaitingForPlayer\"");
        }
    }
}
