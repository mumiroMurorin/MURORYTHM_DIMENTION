using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using System.Threading;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(VideoPlayer))]
public class VideoLoader : MonoBehaviour
{
    VideoPlayer player;
    CancellationTokenSource cts;

    private void Start()
    {
        player = GetComponent<VideoPlayer>();
    }

    public void Play()
    {
        player?.Play();
    }

    /// <summary>
    /// ìÆâÊÇÃÉçÅ[ÉhèIóπÇ‹Ç≈ë“Çø
    /// </summary>
    /// <param name="callBack"></param>
    public void LoadVideo(System.Action callBack = null)
    {
        cts?.CancelAndDispose();
        cts = new CancellationTokenSource();

        LoadVideoAsync(callBack, cts.Token).Forget();
    }

    private async UniTask LoadVideoAsync(System.Action callBack, CancellationToken token)
    {
        player?.Prepare();

        await UniTask.WaitUntil(() => player.isPrepared, cancellationToken: token);

        callBack?.Invoke();
    }
}
