using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring
{
    public class Transitioner_Play : IPhaseTransitioner
    {
        [SerializeField] SerializeInterface<IPhaseTransitionable> phaseTransitionable;
        [SerializeField] SerializeInterface<IGroundController> groundController;
        [SerializeField] SerializeInterface<ITimeController> timer;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.Play;

        bool IPhaseTransitioner.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitioner.Transition()
        {
            Debug.Log("�yTransition�zTransition to \"Play\"");
            StartRhythmGame();
        }

        /// <summary>
        /// ���Q�[�̊J�n
        /// </summary>
        private void StartRhythmGame()
        {
            // �O���E���h�𑖂点��
            groundController?.Value.StartGroundMove();
            // ����i�߂�
            timer?.Value.StartTimer();
        }
    }

}
