using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Tutorial
{
    /// <summary>
    /// ƒQ[ƒ€‚ğˆê’â~‚©‚çÄ¶‚·‚é
    /// </summary>
    [System.Serializable]
    public class Wait : TutorialActionNode
    {
        [SerializeField] float seconds;
        [SerializeField] SerializeInterface<IDisposer> disposableObject;

        CancellationTokenSource cts;

        public override void Do()
        {
            cts?.CancelAndDispose();
            cts = DelayUtility.Run(seconds, () => { next?.Do(); }, true);

            disposableObject?.Value?.SetCts(cts);
        }
    }
}