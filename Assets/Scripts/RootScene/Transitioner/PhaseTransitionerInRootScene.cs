using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TransitionerInRootScene
{
    public class PhaseTransitionerInRootScene : MonoBehaviour, IPhaseTransitionableInRootScene
    {
        const PhaseStatusInRootScene FIRST_STATUS = PhaseStatusInRootScene.LoadData;

        [SerializeReference,SubclassSelector] List<IPhaseTransitionerInRootScene> transitioners;

        void Start()
        {
            Initialize();
            TransitionPhase(FIRST_STATUS);
        }

        private void Initialize()
        {

        }

        public void TransitionPhase(PhaseStatusInRootScene phase)
        {
            Transition(phase);
        }

        /// <summary>
        /// フェーズ遷移
        /// </summary>
        private bool Transition(PhaseStatusInRootScene phase)
        {
            foreach (IPhaseTransitionerInRootScene transitioner in transitioners)
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
    public interface IPhaseTransitionableInRootScene
    {
        public void TransitionPhase(PhaseStatusInRootScene phase);
    }

    /// <summary>
    /// フェーズ遷移の際の処理を行う
    /// </summary>
    public interface IPhaseTransitionerInRootScene
    {
        public void Transition();

        /// <summary>
        /// 遷移条件のチェック
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool ConditionChecker(PhaseStatusInRootScene status);
    }
}
