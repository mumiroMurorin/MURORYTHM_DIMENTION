using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TransitionerInLobbyScene
{
    public class Transitioner_FadeOut : IPhaseTransitionerInLobbyScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInLobbyScene> phaseTransitionable;
        [SerializeField] FadeController fadeController;
        [SerializeField] LobbySceneDataController dataController;

        readonly PhaseStatusInLobbyScene status = PhaseStatusInLobbyScene.FadeOut;

        bool IPhaseTransitionerInLobbyScene.ConditionChecker(PhaseStatusInLobbyScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInLobbyScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"FadeOut\"");

            // アニメーションの再生
            fadeController?.FadeOut(() => { 
                if (dataController.DataGetter.IsPlayTutorial.Value) { TransitionNextPhase(PhaseStatusInLobbyScene.TransitionTutorialScene); }
                else { TransitionNextPhase(PhaseStatusInLobbyScene.TransitionSelectScene); }
            });
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase(PhaseStatusInLobbyScene phase)
        {
            phaseTransitionable.Value.TransitionPhase(phase);
        }
    }

}
