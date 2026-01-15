using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace TransitionerInTitleScene
{
    public class PhaseTransitionerInTitleScene : MonoBehaviour, IPhaseTransitionableInTitleScene, IPhaseStatusGetterInTitleScene
    {
        const PhaseStatusInTitleScene FIRST_STATUS = PhaseStatusInTitleScene.LoadData;

        [SerializeReference, SubclassSelector] List<IPhaseTransitionerInTitleScene> transitioners;

        ReactiveProperty<PhaseStatusInTitleScene> phaseStatus = new ReactiveProperty<PhaseStatusInTitleScene>(FIRST_STATUS);
        IReadOnlyReactiveProperty<PhaseStatusInTitleScene> IPhaseStatusGetterInTitleScene.PhaseStatus => phaseStatus;

        void Start()
        {
            Initialize();
            TransitionPhase(FIRST_STATUS);
        }

        private void Initialize()
        {

        }

        public void TransitionPhase(PhaseStatusInTitleScene phase)
        {
            phaseStatus.Value = phase;
            Transition(phase);
        }

        /// <summary>
        /// フェーズ遷移
        /// </summary>
        private bool Transition(PhaseStatusInTitleScene phase)
        {
            foreach (IPhaseTransitionerInTitleScene transitioner in transitioners)
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
    public interface IPhaseTransitionableInTitleScene
    {
        public void TransitionPhase(PhaseStatusInTitleScene phase);
    }

    public interface IPhaseStatusGetterInTitleScene
    {
        IReadOnlyReactiveProperty<PhaseStatusInTitleScene> PhaseStatus { get; }
    }

    /// <summary>
    /// フェーズ遷移の際の処理を行う
    /// </summary>
    public interface IPhaseTransitionerInTitleScene
    {
        public void Transition();

        /// <summary>
        /// 遷移条件のチェック
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool ConditionChecker(PhaseStatusInTitleScene status);
    }
}
