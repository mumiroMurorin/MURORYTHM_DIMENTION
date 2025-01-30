using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Refactoring
{
    public class Transitioner_FadeIn : IPhaseTransitioner
    {
        [SerializeField] SerializeInterface<IPhaseTransitionable> phaseTransitionable;
        [SerializeField] SerializeInterface<ITimelinePlayer> timelinePlayer;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.FadeIn;

        bool IPhaseTransitioner.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitioner.Transition()
        {
            Debug.Log("�yTransition�zTransition to \"FadeIn\"");

            // �A�j���[�V�����̍Đ�
            timelinePlayer.Value.PlayAnimation(TransitionNextPhase);
        }

        /// <summary>
        /// ���̃t�F�[�Y�ւ̈ړ�
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInRhythmGame.LoadBody);
        }
    }

}
