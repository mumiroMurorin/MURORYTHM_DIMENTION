using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Refactoring
{
    public class Assessment_GreatCombo : MonoBehaviour, IAssessmentPlayer
    {
        [Header("終了演出タイムライン")]
        [SerializeField] PlayableDirector endPlayable;

        readonly ComboRank rank = ComboRank.GreatCombo;
        CancellationTokenSource cts;

        bool IAssessmentPlayer.ConditionChecker(ComboRank rank)
        {
            return rank == this.rank;
        }

        void IAssessmentPlayer.PlayAnimation(Action callback)
        {
            cts = new CancellationTokenSource();
            PlayAnimation(cts.Token, callback).Forget();
        }

        /// <summary>
        /// エンドアニメーションの再生
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid PlayAnimation(CancellationToken token, Action callback)
        {
            if (endPlayable == null)
            {
                callback.Invoke();
                return;
            }

            endPlayable.gameObject.SetActive(true);
            endPlayable.Play();

            try
            {
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
    }

}
