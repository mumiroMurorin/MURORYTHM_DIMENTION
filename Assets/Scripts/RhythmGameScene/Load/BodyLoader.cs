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
    [SerializeField] float loadTimeoutSeconds = 10f;
    [SerializeField] SerializeInterface<ISpaceInputHub> spaceInputHandler;

    ISpaceInputGetter spaceInputGetter;
    IOptionGetter optionGetter;
    INoteSpawnDataOptionGetter spawnDataGetter;
    CancellationTokenSource cts;

    [Inject]
    public void Constructor(ISpaceInputGetter spaceInputGetter, IOptionGetter optionGetter, INoteSpawnDataOptionGetter spawnDataGetter)
    {
        this.spaceInputGetter = spaceInputGetter;
        this.optionGetter = optionGetter;
        this.spawnDataGetter = spawnDataGetter;
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
        if (spaceInputHandler?.Value == null)
        {
            callback.Invoke();
            return;
        }

        if (!spaceInputHandler.Value.IsExistCamera())
        {
            callback.Invoke();
            return;
        }

        spaceInputHandler?.Value.InitializeBodyTracking();
        spaceInputHandler?.Value.StartTracking();

        cts = new CancellationTokenSource();
        LoadBodyAsync(callback, cts.Token).Forget();
    }

    async UniTaskVoid LoadBodyAsync(Action callback, CancellationToken token)
    {
        if (spaceInputGetter == null)
        {
            callback.Invoke();
            return;
        }

        try
        {
            var waitTrackingTask = UniTask.WaitUntil(() =>
                IsTrackingReady() || IsAutoModeOn(),
                cancellationToken: token);

            if (loadTimeoutSeconds > 0f)
            {
                var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(loadTimeoutSeconds), cancellationToken: token);
                int completedTaskIndex = await UniTask.WhenAny(waitTrackingTask, timeoutTask);

                if (completedTaskIndex == 1)
                {
                    Debug.LogWarning($"【BodyLoader】Tracking load timed out after {loadTimeoutSeconds} seconds.");
                }
            }
            else
            {
                await waitTrackingTask;
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        callback.Invoke();
    }

    private bool IsTrackingReady()
    {
        return spaceInputGetter.GetCanGetSpaceInputReactiveProperty(SpaceTrackingTag.RightHand).Value
            || spaceInputGetter.GetCanGetSpaceInputReactiveProperty(SpaceTrackingTag.LeftHand).Value;
    }

    private bool IsAutoModeOn()
    {
        return optionGetter != null && spawnDataGetter.IsAutoMode;
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}
