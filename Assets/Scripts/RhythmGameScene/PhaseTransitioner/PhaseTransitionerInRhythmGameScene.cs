using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace TransitionerInRhythmGameScene
{
    public class PhaseTransitionerInRhythmGameScene : MonoBehaviour, IPhaseTransitionableInRhythmGameScene, IPhaseStatusGetterInRhythmGameScene
    {
        const PhaseStatusInRhythmGame FIRST_STATUS = PhaseStatusInRhythmGame.LoadData;

        [SerializeReference,SubclassSelector] List<IPhaseTransitionerInRhythmGameScene> transitioners;

        // エラー時に戻る先のフェーズ (譜面・音源の読み込みやノーツの生成に失敗したとき)
        const PhaseStatusInRhythmGame PHASE_ON_ERROR = PhaseStatusInRhythmGame.TransitionSelectScene;

        ReactiveProperty<PhaseStatusInRhythmGame> phaseStatus = new ReactiveProperty<PhaseStatusInRhythmGame>(FIRST_STATUS);
        IReadOnlyReactiveProperty<PhaseStatusInRhythmGame> IPhaseStatusGetterInRhythmGameScene.PhaseStatus => phaseStatus;

        bool isReturningOnError;

        void Start()
        {
            Initialize();
            TransitionPhase(FIRST_STATUS);
        }

        void OnEnable()
        {
            GameFlowError.OnRaised += OnGameFlowError;
        }

        void OnDisable()
        {
            GameFlowError.OnRaised -= OnGameFlowError;
        }

        /// <summary>
        /// 先へ進めないエラーが起きたときは、選曲画面へ戻る
        /// </summary>
        private void OnGameFlowError(string message)
        {
            if (isReturningOnError) { return; }
            isReturningOnError = true;

            Debug.LogWarning($"【Transition】エラーが起きたため {PHASE_ON_ERROR} へ移ります: {message}");
            TransitionPhase(PHASE_ON_ERROR);
        }

        private void Initialize()
        {

        }

        public void TransitionPhase(PhaseStatusInRhythmGame phase)
        {
            phaseStatus.Value = phase;
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

    public interface IPhaseStatusGetterInRhythmGameScene
    {
        IReadOnlyReactiveProperty<PhaseStatusInRhythmGame> PhaseStatus { get; }
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
