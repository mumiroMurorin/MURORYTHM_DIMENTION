using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Tutorial
{
    /// <summary>
    /// コールバックを呼ぶ
    /// </summary>
    [System.Serializable]
    public class Callback : TutorialActionNode
    {
        System.Action callback;

        public Callback(System.Action callback)
        {
            this.callback = callback;
        }

        CancellationTokenSource cts;

        public override void Do()
        {
            callback?.Invoke();
        }
    }
}