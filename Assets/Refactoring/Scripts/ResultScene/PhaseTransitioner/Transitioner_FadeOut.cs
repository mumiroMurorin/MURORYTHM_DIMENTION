using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Refactoring.TransitionerInResultScene
{
    public class Transitioner_FadeOut : IPhaseTransitionerInResultScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInResultScene> phaseTransitionable;
        [SerializeField] SerializeInterface<ITimelinePlayer> timelinePlayer;

        readonly PhaseStatusInResultScene status = PhaseStatusInResultScene.FadeOut;

        bool IPhaseTransitionerInResultScene.ConditionChecker(PhaseStatusInResultScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInResultScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"FadeOut\"");

            // アニメーションの再生
            timelinePlayer.Value.PlayAnimation(TransitionNextPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInResultScene.TransitionSelectScene);
        }
    }

}
