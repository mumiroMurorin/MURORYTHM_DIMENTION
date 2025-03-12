using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring.TransitionerInResultScene
{
    public class PhaseTransitionerInResultScene : MonoBehaviour, IPhaseTransitionableInResultScene
    {
        const PhaseStatusInResultScene FIRST_STATUS = PhaseStatusInResultScene.LoadData;

        [SerializeReference, SubclassSelector] List<IPhaseTransitionerInResultScene> transitioners;

        void Start()
        {
            Initialize();
            TransitionPhase(FIRST_STATUS);
        }

        private void Initialize()
        {

        }

        public void TransitionPhase(PhaseStatusInResultScene phase)
        {
            Transition(phase);
        }

        /// <summary>
        /// フェーズ遷移
        /// </summary>
        private bool Transition(PhaseStatusInResultScene phase)
        {
            foreach (IPhaseTransitionerInResultScene transitioner in transitioners)
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
