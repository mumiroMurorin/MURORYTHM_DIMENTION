using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring
{
    public class Transitioner_LoadBody : IPhaseTransitioner
    {
        [SerializeField] SerializeInterface<IPhaseTransitionable> phaseTransitionable;
        [SerializeField] SerializeInterface<IBodyLoader> bodyLoader;
        [SerializeField] SerializeInterface<ITimelinePlayer> openLodingBodyUI; 
        [SerializeField] SerializeInterface<ITimelinePlayer> closeLodingBodyUI; 

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.LoadBody;

        bool IPhaseTransitioner.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitioner.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadBody\"");

            openLodingBodyUI?.Value.PlayAnimation(() => {
                bodyLoader.Value.WaitForLoadBody(CloseUI);
            });
        }

        /// <summary>
        /// 体の認識UIを閉じる
        /// </summary>
        private void CloseUI()
        {
            closeLodingBodyUI?.Value.PlayAnimation(TransitionNextPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInRhythmGame.StartAnimation);
        }
    }

}
