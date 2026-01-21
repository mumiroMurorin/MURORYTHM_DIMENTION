using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TransitionerInTitleScene
{
    public class Transitioner_GameStart : IPhaseTransitionerInTitleScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInTitleScene> phaseTransitionable;
        [SerializeField] SerializeInterface<ITimelinePlayer> timeLinePlayer;

        readonly PhaseStatusInTitleScene status = PhaseStatusInTitleScene.GameStart;

        bool IPhaseTransitionerInTitleScene.ConditionChecker(PhaseStatusInTitleScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTitleScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"GameStart\"");

            // アニメーションの再生
            timeLinePlayer.Value?.PlayAnimation(TransitionNextPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInTitleScene.FadeOut);
        }
    }

}
