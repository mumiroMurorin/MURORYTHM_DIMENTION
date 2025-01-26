using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;

namespace Refactoring
{
    public class TimelinePlayer : MonoBehaviour, ITimelinePlayer
    {
        [Header("演出タイムライン")]
        [SerializeField] PlayableDirector playableDirector;

        CancellationTokenSource cts;

        void ITimelinePlayer.PlayAnimation(Action callback)
        {
            cts = new CancellationTokenSource();
            PlayAnimation(cts.Token, callback).Forget();
        }

        /// <summary>
        /// アニメーションの再生
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid PlayAnimation(CancellationToken token, Action callback)
        {
            if (playableDirector == null)
            {
                callback.Invoke();
                return;
            }

            playableDirector.gameObject.SetActive(true);
            playableDirector.Play();

            try
            {
                playableDirector.Play();

                // タイムラインの再生が終了するかキャンセルされるまで待機
                await UniTask.WaitUntil(() => playableDirector.state != PlayState.Playing, cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("【System】タイムラインの再生がキャンセルされました");
            }

            callback.Invoke();
        }
    }

}