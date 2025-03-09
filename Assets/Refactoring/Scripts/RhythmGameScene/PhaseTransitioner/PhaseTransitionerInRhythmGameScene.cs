using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring.TransitionerInRhythmGameScene
{
    public class PhaseTransitionerInRhythmGameScene : MonoBehaviour, IPhaseTransitionableInRhythmGameScene
    {
        const PhaseStatusInRhythmGame FIRST_STATUS = PhaseStatusInRhythmGame.LoadData;

        [SerializeReference,SubclassSelector] List<IPhaseTransitionerInRhythmGameScene> transitioners;

        void Start()
        {
            Initialize();
            TransitionPhase(FIRST_STATUS);
        }

        private void Initialize()
        {

        }

        public void TransitionPhase(PhaseStatusInRhythmGame phase)
        {
            Transition(phase);
        }

        /// <summary>
        /// フェーズ遷移
        /// </summary>
        private bool Transition(PhaseStatusInRhythmGame phase)
        {
            foreach (IPhaseTransitionerInRhythmGameScene transitioner in transitioners)
            {
                if (transitioner.ConditionChecker(phase))
                {
                    transitioner.Transition();
                    return true;
                }
            }

            Debug.LogWarning($"【Transition】遷移ステータス{phase}に対するTransitionerがセットされていません");
            return false;
        }
    }
}
