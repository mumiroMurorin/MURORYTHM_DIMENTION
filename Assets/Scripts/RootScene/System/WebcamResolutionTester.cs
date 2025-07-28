using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class WebcamResolutionTester : MonoBehaviour
{
    public string deviceName = null; // null ならデフォルトのカメラ
    
    WebCamTexture currentTexture;
    CancellationTokenSource cts = new CancellationTokenSource();

    // よくある解像度リスト
    [SerializeField] Vector2Int[] testResolutions = new Vector2Int[]
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1280, 720),
        new Vector2Int(640, 480),
        new Vector2Int(320, 240)
    };

    void Start()
    {
        // CheckAllResolutionsAsync(cts.Token).Forget();
    }

    // 与えられたデバイス名と解像度リストで起動をチェック
    public async UniTask CheckAllResolutionsAsync(CancellationToken cancellationToken)
    {
        foreach (var resolution in testResolutions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await WebCamUtils.CheckIfTextureStartedAsync(
                deviceName,
                resolution.x,
                resolution.y,
                cancellationToken);

            Debug.Log($"【Camera】{resolution.x}x{resolution.y}: {result}");
        }
    }


    private void OnDestroy()
    {
        cts?.CancelAndDispose();
    }
}

public static class WebCamUtils
{
    public static async UniTask<bool> CheckIfTextureStartedAsync(string deviceName, int width, int height, CancellationToken token)
    {
        var webcamTexture = new WebCamTexture(deviceName, width, height);
        webcamTexture.Play();

        try
        {
            float timeout = 5f;
            float elapsed = 0f;
            const float pollInterval = 0.1f;

            while (elapsed < timeout)
            {
                token.ThrowIfCancellationRequested();

                if (webcamTexture.width > 16 && webcamTexture.height > 16)
                {
                    return true;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(pollInterval), cancellationToken: token);
                elapsed += pollInterval;
            }

            return false;
        }
        finally
        {
            webcamTexture.Stop();
            UnityEngine.Object.Destroy(webcamTexture);
        }
    }

}