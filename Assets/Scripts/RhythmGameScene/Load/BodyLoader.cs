using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public class BodyLoader : MonoBehaviour, IBodyLoader
{
    [SerializeField] bool isUseSpaceInput;
    [SerializeField] SerializeInterface<ISpaceInputHandler> spaceInputHandler;

    ISpaceInputGetter spaceInputGetter;
    CancellationTokenSource cts;

    [Inject]
    public void Constructor(ISpaceInputGetter spaceInputGetter)
    {
        this.spaceInputGetter = spaceInputGetter;
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (!isUseSpaceInput) { return; }
#endif

        // トラッキングの初期化
        spaceInputHandler?.Value.InitializeBodyTracking();
    }

    void IBodyLoader.WaitForLoadBody(Action callback)
    {
#if UNITY_EDITOR
        if (!isUseSpaceInput)
        {
            callback.Invoke();
            return;
        }
#endif

        // トラッキング開始
        spaceInputHandler?.Value.StartTracking();

        cts = new CancellationTokenSource();
        LoadBodyAsync(callback, cts.Token).Forget();
    }

    async UniTaskVoid LoadBodyAsync(Action callback, CancellationToken token)
    {
        await UniTask.WaitUntil(() => spaceInputGetter.CanGetSpaceInputReactiveProperty.Value, cancellationToken: token);
        callback.Invoke();
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}
