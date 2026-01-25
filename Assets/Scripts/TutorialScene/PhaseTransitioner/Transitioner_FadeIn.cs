using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TransitionerInTutorialScene
{
    public class Transitioner_FadeIn : IPhaseTransitionerInTutorialScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInTutorialScene> phaseTransitionable;
        [SerializeField] MusicDataGetter musicDataGetter;
        [SerializeField] FadeController fadeController;

        readonly PhaseStatusInTutorialScene status = PhaseStatusInTutorialScene.FadeIn;

        bool IPhaseTransitionerInTutorialScene.ConditionChecker(PhaseStatusInTutorialScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTutorialScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"FadeIn\"");

            // アニメーションの再生
            fadeController?.FadeIn(musicDataGetter.DataGetter, TransitionNextPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInTutorialScene.LoadBody);
        }
    }

}
