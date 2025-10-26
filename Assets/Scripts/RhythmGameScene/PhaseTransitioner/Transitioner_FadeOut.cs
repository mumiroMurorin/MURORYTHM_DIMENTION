using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TransitionerInRhythmGameScene
{
    public class Transitioner_FadeOut : IPhaseTransitionerInRhythmGameScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInRhythmGameScene> phaseTransitionable;
        [SerializeField] FadeController fadeController;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.FadeOut;

        bool IPhaseTransitionerInRhythmGameScene.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRhythmGameScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"FadeOut\"");

            // アニメーションの再生
            fadeController?.FadeOut(TransitionNextPhase);
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
