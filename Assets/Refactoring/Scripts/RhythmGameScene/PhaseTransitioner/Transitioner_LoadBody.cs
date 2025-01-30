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
            Debug.Log("�yTransition�zTransition to \"LoadBody\"");

            openLodingBodyUI?.Value.PlayAnimation(() => {
                bodyLoader.Value.WaitForLoadBody(CloseUI);
            });
        }

        /// <summary>
        /// �̂̔F��UI�����
        /// </summary>
        private void CloseUI()
        {
            closeLodingBodyUI?.Value.PlayAnimation(TransitionNextPhase);
        }

        /// <summary>
        /// ���̃t�F�[�Y�ւ̈ړ�
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInRhythmGame.StartAnimation);
        }
    }

}
