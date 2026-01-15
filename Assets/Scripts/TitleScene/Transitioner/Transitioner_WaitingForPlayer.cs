using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;

namespace TransitionerInTitleScene
{
    public class Transitioner_WaitingForPlayer : IPhaseTransitionerInTitleScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInTitleScene> phaseTransitionable;

        readonly PhaseStatusInTitleScene status = PhaseStatusInTitleScene.WaitingForPlayer;

        CancellationTokenSource cts;

        bool IPhaseTransitionerInTitleScene.ConditionChecker(PhaseStatusInTitleScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTitleScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"WaitingForPlayer\"");

            cts?.CancelAndDispose();
            cts = new CancellationTokenSource();

            WaitForPlayerInput(TransitionNextPhase, cts.Token).Forget();
        }

        private async UniTask WaitForPlayerInput(System.Action callback, CancellationToken token)
        {
            await UniTask.WaitUntil(() => Input.anyKeyDown, cancellationToken: token);

            callback?.Invoke();
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInTitleScene.TransitionSelectScene);
        }
    }
}
