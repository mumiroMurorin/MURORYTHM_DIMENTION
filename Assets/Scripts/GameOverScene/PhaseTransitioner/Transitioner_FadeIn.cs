using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TransitionerInGameOverScene
{
    public class Transitioner_FadeIn : IPhaseTransitionerInGameOverScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInGameOverScene> phaseTransitionable;
        [SerializeField] MusicDataGetter musicDataGetter;
        [SerializeField] FadeController fadeController;

        readonly PhaseStatusInGameOverScene status = PhaseStatusInGameOverScene.FadeIn;

        bool IPhaseTransitionerInGameOverScene.ConditionChecker(PhaseStatusInGameOverScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInGameOverScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"FadeIn\"");

            // アニメーションの再生
            fadeController?.FadeIn(musicDataGetter?.DataGetter, TransitionNextPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInGameOverScene.ConfirmContinue);
        }
    }

}
