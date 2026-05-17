using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TransitionerInLobbyScene
{
    public class Transitioner_ConfirmTutorial_Operation : IPhaseTransitionerInLobbyScene
    {
        readonly PhaseStatusInLobbyScene status = PhaseStatusInLobbyScene.ConfirmTutorial_Operation;

        bool IPhaseTransitionerInLobbyScene.ConditionChecker(PhaseStatusInLobbyScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInLobbyScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"ConfirmTutorial_Operation\"");

        }
    }

}
