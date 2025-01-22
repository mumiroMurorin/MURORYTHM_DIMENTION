using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Refactoring
{
    public class Transitioner_EndAnimation : IPhaseTransitioner
    {
        [SerializeField] SerializeInterface<IPhaseTransitionable> phaseTransitionable;
        [Header("終了演出タイムライン")]
        [SerializeField] PlayableDirector endPlayable;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.EndAnimation;
        CancellationTokenSource cts;

        bool IPhaseTransitioner.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitioner.Transition()
        {
            Debug.Log("【Transition】Transition to \"EndAnimation\"");

            cts = new CancellationTokenSource();
            PlayAnimation(cts.Token, TransitionNextPhase).Forget();
        }

        /// <summary>
        /// スタートアニメーションの再生
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid PlayAnimation(CancellationToken token, Action callback)
        {
            if (endPlayable == null)
            {
                callback.Invoke();
                return;
            }
            endPlayable.Play();

            try
            {
                // タイムラインを再生
                endPlayable.Play();

                // タイムラインの再生が終了するかキャンセルされるまで待機
                await UniTask.WaitUntil(() => endPlayable.state != PlayState.Playing, cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("【Transition】タイムラインの再生がキャンセルされました");
            }

            callback.Invoke();
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInRhythmGame.TransitionResultScene);
        }
    }

}
