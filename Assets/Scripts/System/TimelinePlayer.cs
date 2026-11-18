using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;

public class TimelinePlayer : MonoBehaviour, ITimelinePlayer
{
    [Header("演出タイムライン")]
    [SerializeField] PlayableDirector playableDirector;
    [SerializeField] bool isResetAwake;

    CancellationTokenSource cts;

    private void Start()
    {
        if (isResetAwake && playableDirector != null) { playableDirector.time = 0; }
    }

    void ITimelinePlayer.PlayAnimation(Action callback)
    {
        cts?.CancelAndDispose();
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
            callback?.Invoke();
            return;
        }

        playableDirector.gameObject.SetActive(true);

        try
        {
            playableDirector.Play();

            // タイムラインの再生が終了するかキャンセルされるまで待機
            await UniTask.WaitUntil(() => playableDirector.state != PlayState.Playing, cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            // キャンセル (もう一度再生された / シーンが破棄された) のときはコールバックを呼ばない。
            // 呼ぶと、同じ次フェーズへの遷移が二重に走ったり、破棄後に遷移が走ったりする。
            Debug.Log("【System】タイムラインの再生がキャンセルされました");
            return;
        }

        callback?.Invoke();
    }

    private void OnDestroy()
    {
        cts?.CancelAndDispose();
    }
}
