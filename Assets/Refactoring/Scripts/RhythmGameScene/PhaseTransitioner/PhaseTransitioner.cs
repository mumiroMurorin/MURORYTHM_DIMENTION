using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class PhaseTransitioner : MonoBehaviour, IPhaseTransitionable
    {
        const PhaseStatusInRhythmGame FIRST_STATUS = PhaseStatusInRhythmGame.LoadData;

        [SerializeReference,SubclassSelector] List<IPhaseTransitioner> transitioners;

        private void Start()
        {
            Initialize();
            TransitionPhase(FIRST_STATUS);

            Transitioner_LoadData t = new Transitioner_LoadData();
        }

        private void Initialize()
        {

        }

        public void TransitionPhase(PhaseStatusInRhythmGame phase)
        {
            Transition(phase);
        }

        /// <summary>
        /// �t�F�[�Y�J��
        /// </summary>
        private bool Transition(PhaseStatusInRhythmGame phase)
        {
            foreach (IPhaseTransitioner transitioner in transitioners)
            {
                if (transitioner.ConditionChecker(phase))
                {
                    transitioner.Transition();
                    return true;
                }
            }

            Debug.LogWarning($"�yTransition�z�J�ڃX�e�[�^�X{phase}�ɑ΂���Transitioner���Z�b�g����Ă��܂���");
            return false;
        }
    }
}
