using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Threading;

namespace TransitionerInGameOverScene
{
    public class PhaseTransitionerInGameOverScene : MonoBehaviour, IPhaseTransitionableInGameOverScene, IPhaseStatusGetterInGameOverScene
    {
        const PhaseStatusInGameOverScene FIRST_STATUS = PhaseStatusInGameOverScene.LoadData;

        [SerializeReference, SubclassSelector] List<IPhaseTransitionerInGameOverScene> transitioners;

        List<CancellationTokenSource> ctsList = new List<CancellationTokenSource>();

        ReactiveProperty<PhaseStatusInGameOverScene> phaseStatus = new ReactiveProperty<PhaseStatusInGameOverScene>(FIRST_STATUS);
        IReadOnlyReactiveProperty<PhaseStatusInGameOverScene> IPhaseStatusGetterInGameOverScene.PhaseStatus => phaseStatus;

        void Start()
        {
            Initialize();
            TransitionPhase(FIRST_STATUS);
        }

        private void Initialize()
        {

        }

        public void TransitionPhase(PhaseStatusInGameOverScene phase)
        {
            phaseStatus.Value = phase;
            Transition(phase);
        }

        /// <summary>
        /// フェーズ遷移
        /// </summary>
        private bool Transition(PhaseStatusInGameOverScene phase)
        {
            foreach (IPhaseTransitionerInGameOverScene transitioner in transitioners)
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

        public void RegisterCts(CancellationTokenSource cts)
        {
            ctsList.Add(cts);
        }

        private void OnDestroy()
        {
            foreach (var cts in ctsList)
            {
                cts?.CancelAndDispose();
            }
        }
    }

    /// <summary>
    /// フェーズ遷移を行うことが出来る
    /// </summary>
    public interface IPhaseTransitionableInGameOverScene
    {
        void TransitionPhase(PhaseStatusInGameOverScene phase);

        void RegisterCts(CancellationTokenSource cts);
    }

    public interface IPhaseStatusGetterInGameOverScene
    {
        IReadOnlyReactiveProperty<PhaseStatusInGameOverScene> PhaseStatus { get; }
    }

    /// <summary>
    /// フェーズ遷移の際の処理を行う
    /// </summary>
    public interface IPhaseTransitionerInGameOverScene
    {
        public void Transition();

        /// <summary>
        /// 遷移条件のチェック
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool ConditionChecker(PhaseStatusInGameOverScene status);
    }
}
