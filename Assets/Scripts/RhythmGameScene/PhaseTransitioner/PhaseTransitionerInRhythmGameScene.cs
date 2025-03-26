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

    /// <summary>
    /// フェーズ遷移を行うことが出来る
    /// </summary>
    public interface IPhaseTransitionableInRhythmGameScene
    {
        public void TransitionPhase(PhaseStatusInRhythmGame phase);
    }

    /// <summary>
    /// フェーズ遷移の際の処理を行う
    /// </summary>
    public interface IPhaseTransitionerInRhythmGameScene
    {
        public void Transition();

        /// <summary>
        /// 遷移条件のチェック
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool ConditionChecker(PhaseStatusInRhythmGame status);
    }
}
