using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

public static class CTSUtil
{
    public static void CancelAndDispose(this CancellationTokenSource cts)
    {
        if(cts == null) { return; }
        cts.Cancel();
        cts.Dispose();
        cts = null;
    }
} 