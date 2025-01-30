using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Refactoring
{
    public class BodyLoader : MonoBehaviour, IBodyLoader
    {
        [SerializeField] bool isUseKinectInput;

        ISpaceInputGetter spaceInputGetter;
        CancellationTokenSource cts;

        [Inject]
        public void Constructor(ISpaceInputGetter spaceInputGetter)
        {
            this.spaceInputGetter = spaceInputGetter;
        }

        void IBodyLoader.WaitForLoadBody(Action callback)
        {
            if (!isUseKinectInput)
            {
                callback.Invoke();
                return;
            }

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
}
