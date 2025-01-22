using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Refactoring
{
    public class Transitioner_StartAnimation : IPhaseTransitioner
    {
        [SerializeField] SerializeInterface<IPhaseTransitionable> phaseTransitionable;
        [Header("スタート演出タイムライン")]
        [SerializeField] PlayableDirector startPlayable;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.StartAnimation;
        CancellationTokenSource cts;

        bool IPhaseTransitioner.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitioner.Transition()
        {
            Debug.Log("【Transition】Transition to \"StartAnimation\"");
            
            cts = new CancellationTokenSource();
            PlayAnimation(cts.Token, TransitionNextPhase).Forget();
        }

        /// <summary>
        /// スタートアニメーションの再生
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid PlayAnimation(CancellationToken token, Action callback)
        {
            if(startPlayable == null) 
            {
                callback.Invoke();
                return; 
            }
            startPlayable.Play();

            try
            {
                // タイムラインを再生
                startPlayable.Play();

                // タイムラインの再生が終了するかキャンセルされるまで待機
                await UniTask.WaitUntil(() => startPlayable.state != PlayState.Playing, cancellationToken: token);
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
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInRhythmGame.Play);
        }
    }

}
