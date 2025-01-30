using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Refactoring
{
    public class Transitioner_FadeOut : IPhaseTransitioner
    {
        [SerializeField] SerializeInterface<IPhaseTransitionable> phaseTransitionable;
        [SerializeField] SerializeInterface<ITimelinePlayer> timelinePlayer;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.FadeOut;

        bool IPhaseTransitioner.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitioner.Transition()
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
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInRhythmGame.TransitionResultScene);
        }
    }

}
