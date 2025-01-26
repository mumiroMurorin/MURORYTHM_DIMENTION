using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Refactoring
{
    public class Transitioner_StartAnimation : IPhaseTransitioner
    {
        [SerializeField] SerializeInterface<IPhaseTransitionable> phaseTransitionable;
        [Header("スタート演出タイムライン")]
        [SerializeField] SerializeInterface<ITimelinePlayer> timelinePlayer;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.StartAnimation;

        bool IPhaseTransitioner.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitioner.Transition()
        {
            Debug.Log("【Transition】Transition to \"StartAnimation\"");

            timelinePlayer?.Value.PlayAnimation(TransitionNextPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInRhythmGame.Play);
        }
    }

}
