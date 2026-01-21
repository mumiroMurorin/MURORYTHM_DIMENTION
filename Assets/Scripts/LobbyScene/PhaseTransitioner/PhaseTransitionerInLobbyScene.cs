using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Threading;

namespace TransitionerInLobbyScene
{
    public class PhaseTransitionerInLobbyScene : MonoBehaviour, IPhaseTransitionableInLobbyScene, IPhaseStatusGetterInLobbyScene
    {
        const PhaseStatusInLobbyScene FIRST_STATUS = PhaseStatusInLobbyScene.LoadData;

        [SerializeReference, SubclassSelector] List<IPhaseTransitionerInLobbyScene> transitioners;

        List<CancellationTokenSource> ctsList = new List<CancellationTokenSource>();

        ReactiveProperty<PhaseStatusInLobbyScene> phaseStatus = new ReactiveProperty<PhaseStatusInLobbyScene>(FIRST_STATUS);
        IReadOnlyReactiveProperty<PhaseStatusInLobbyScene> IPhaseStatusGetterInLobbyScene.PhaseStatus => phaseStatus;

        void Start()
        {
            Initialize();
            TransitionPhase(FIRST_STATUS);
        }

        private void Initialize()
        {

        }

        public void TransitionPhase(PhaseStatusInLobbyScene phase)
        {
            phaseStatus.Value = phase;
            Transition(phase);
        }

        /// <summary>
        /// フェーズ遷移
        /// </summary>
        private bool Transition(PhaseStatusInLobbyScene phase)
        {
            foreach (IPhaseTransitionerInLobbyScene transitioner in transitioners)
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
    public interface IPhaseTransitionableInLobbyScene
    {
        void TransitionPhase(PhaseStatusInLobbyScene phase);

        void RegisterCts(CancellationTokenSource cts);
    }

    public interface IPhaseStatusGetterInLobbyScene
    {
        IReadOnlyReactiveProperty<PhaseStatusInLobbyScene> PhaseStatus { get; }
    }

    /// <summary>
    /// フェーズ遷移の際の処理を行う
    /// </summary>
    public interface IPhaseTransitionerInLobbyScene
    {
        public void Transition();

        /// <summary>
        /// 遷移条件のチェック
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool ConditionChecker(PhaseStatusInLobbyScene status);
    }
}
