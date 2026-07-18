using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Tutorial
{
    /// <summary>
    /// ゲームを一時停止から再生する
    /// </summary>
    [System.Serializable]
    public class Wait : TutorialActionNode
    {
        [SerializeField] float seconds;

        CancellationTokenSource cts;
        TutorialRuntimeContext context;

        public override void Initialize(TutorialRuntimeContext context)
        {
            this.context = context;
        }

        public override void Do()
        {
            cts?.CancelAndDispose();
            cts = DelayUtility.Run(seconds, () => { next?.Do(); }, true);
            context?.Disposer?.SetCts(cts);
        }
    }
}