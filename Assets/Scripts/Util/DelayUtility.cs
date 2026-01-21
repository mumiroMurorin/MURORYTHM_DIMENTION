using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class DelayUtility
{
    /// <summary>
    /// n秒後に callback を実行する（キャンセル可能）
    /// </summary>
    /// <param name="seconds">待機秒数</param>
    /// <param name="callback">呼び出す処理</param>
    /// <param name="token">キャンセルトークン</param>
    public static async UniTask InvokeAfterSeconds(float seconds, Action callback, CancellationToken token, bool ignoreTimeScale)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(seconds), ignoreTimeScale: ignoreTimeScale, cancellationToken: token);
            callback?.Invoke();
        }
        catch (OperationCanceledException)
        {
            Debug.Log("InvokeAfterSeconds: キャンセルされました");
        }
    }

    /// <summary>
    /// n秒後に callback を実行するタスクを開始し、そのキャンセルトークンソースを返す
    /// </summary>
    public static CancellationTokenSource Run(float seconds, Action callback, bool ignoreTimeScale = false)
    {
        var cts = new CancellationTokenSource();
        InvokeAfterSeconds(seconds, callback, cts.Token, ignoreTimeScale).Forget();
        return cts;
    }
}
