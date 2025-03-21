using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring.TransitionerInSelectScene
{
    public class PhaseTransitionerInSelectScene : MonoBehaviour, IPhaseTransitionableInSelectScene, IPhaseStatusGetterInSelectScene
    {
        const PhaseStatusInSelectScene FIRST_STATUS = PhaseStatusInSelectScene.LoadData;

        [SerializeReference, SubclassSelector] List<IPhaseTransitionerInSelectScene> transitioners;

        ReactiveProperty<PhaseStatusInSelectScene> phaseStatus = new ReactiveProperty<PhaseStatusInSelectScene>(FIRST_STATUS);
        IReadOnlyReactiveProperty<PhaseStatusInSelectScene> IPhaseStatusGetterInSelectScene.PhaseStatus => phaseStatus;

        void Start()
        {
            Initialize();
            TransitionPhase(FIRST_STATUS);
        }

        private void Initialize()
        {

        }

        public void TransitionPhase(PhaseStatusInSelectScene phase)
        {
            phaseStatus.Value = phase;
            Transition(phase);
        }

        /// <summary>
        /// フェーズ遷移
        /// </summary>
        private bool Transition(PhaseStatusInSelectScene phase)
        {
            foreach (IPhaseTransitionerInSelectScene transitioner in transitioners)
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
