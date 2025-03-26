using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Refactoring.TransitionerInResultScene
{
    public class PhaseTransitionerInResultScene : MonoBehaviour, IPhaseTransitionableInResultScene, IPhaseStatusGetterInResultScene
    {
        const PhaseStatusInResultScene FIRST_STATUS = PhaseStatusInResultScene.LoadData;

        [SerializeReference, SubclassSelector] List<IPhaseTransitionerInResultScene> transitioners;

        ReactiveProperty<PhaseStatusInResultScene> phaseStatus = new ReactiveProperty<PhaseStatusInResultScene>(FIRST_STATUS);
        IReadOnlyReactiveProperty<PhaseStatusInResultScene> IPhaseStatusGetterInResultScene.PhaseStatus => phaseStatus;

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
            phaseStatus.Value = phase;

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

    /// <summary>
    /// フェーズ遷移を行うことが出来る
    /// </summary>
    public interface IPhaseTransitionableInResultScene
    {
        public void TransitionPhase(PhaseStatusInResultScene phase);
    }

    public interface IPhaseStatusGetterInResultScene
    {
        IReadOnlyReactiveProperty<PhaseStatusInResultScene> PhaseStatus { get; }
    }

    /// <summary>
    /// フェーズ遷移の際の処理を行う
    /// </summary>
    public interface IPhaseTransitionerInResultScene
    {
        public void Transition();

        /// <summary>
        /// 遷移条件のチェック
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool ConditionChecker(PhaseStatusInResultScene status);
    }
}
