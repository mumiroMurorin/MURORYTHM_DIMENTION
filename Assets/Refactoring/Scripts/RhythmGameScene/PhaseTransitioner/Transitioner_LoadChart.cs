using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring
{
    public class Transitioner_LoadChart : IPhaseTransitioner
    {
        [SerializeField] SerializeInterface<IChartGenerator> chartGenerator;
        [SerializeField] SerializeInterface<IPhaseTransitionable> phaseTransitionable;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.LoadChart;

        bool IPhaseTransitioner.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitioner.Transition()
        {
            Debug.Log("�yTransition�zTransition to \"LoadChart\"");

            //chartGenerator.Value.Generate(, TransitionNextPhase);
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
