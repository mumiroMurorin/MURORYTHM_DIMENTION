using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Threading;

namespace TransitionerInTutorialScene
{
    public class PhaseTransitionerInTutorialScene : MonoBehaviour, IPhaseTransitionableInTutorialScene, IPhaseStatusGetterInTutorialScene
    {
        const PhaseStatusInTutorialScene FIRST_STATUS = PhaseStatusInTutorialScene.LoadData;

        [SerializeReference, SubclassSelector] List<IPhaseTransitionerInTutorialScene> transitioners;

        List<CancellationTokenSource> ctsList = new List<CancellationTokenSource>();

        ReactiveProperty<PhaseStatusInTutorialScene> phaseStatus = new ReactiveProperty<PhaseStatusInTutorialScene>(FIRST_STATUS);
        IReadOnlyReactiveProperty<PhaseStatusInTutorialScene> IPhaseStatusGetterInTutorialScene.PhaseStatus => phaseStatus;

        void Start()
        {
            Initialize();
            TransitionPhase(FIRST_STATUS);
        }

        private void Initialize()
        {

        }

        public void TransitionPhase(PhaseStatusInTutorialScene phase)
        {
            phaseStatus.Value = phase;
            Transition(phase);
        }

        /// <summary>
        /// フェーズ遷移
        /// </summary>
        private bool Transition(PhaseStatusInTutorialScene phase)
        {
            foreach (IPhaseTransitionerInTutorialScene transitioner in transitioners)
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
    public interface IPhaseTransitionableInTutorialScene
    {
        void TransitionPhase(PhaseStatusInTutorialScene phase);

        void RegisterCts(CancellationTokenSource cts);
    }

    public interface IPhaseStatusGetterInTutorialScene
    {
        IReadOnlyReactiveProperty<PhaseStatusInTutorialScene> PhaseStatus { get; }
    }

    /// <summary>
    /// フェーズ遷移の際の処理を行う
    /// </summary>
    public interface IPhaseTransitionerInTutorialScene
    {
        public void Transition();

        /// <summary>
        /// 遷移条件のチェック
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool ConditionChecker(PhaseStatusInTutorialScene status);
    }
}
