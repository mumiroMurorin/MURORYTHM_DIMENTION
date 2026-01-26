using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;

public class TextBoxController : MonoBehaviour
{
    [SerializeField] bool disableOnStart = true;
    [SerializeField] SE_Type openSe;
    [SerializeField] SE_Type closeSe;
    [SerializeField] Animator anim;

    const string OPEN_ANIM_NAME = "Open";
    const string CLOSE_ANIM_NAME = "Close";

    bool isOpening;
    CancellationTokenSource cts;

    private void Start()
    {
        if (disableOnStart) { this.gameObject.SetActive(false); }
    }

    public void Open(Action callback = null)
    {
        cts?.CancelAndDispose();
        cts = new CancellationTokenSource();

        WaitForOpenAsync(callback, cts.Token).Forget();
    }

    public void Close(Action callback = null)
    {
        cts?.CancelAndDispose();
        cts = new CancellationTokenSource();

        WaitForCloseAsync(callback, cts.Token).Forget();
    }

    private async UniTask WaitForOpenAsync(Action callback, CancellationToken token)
    {
        this.gameObject.SetActive(true);
        SoundManager.Instance.PlaySE(openSe);
        isOpening = false;

        anim.SetTrigger(OPEN_ANIM_NAME);
        await UniTask.WaitUntil(() => isOpening, cancellationToken: token);

        callback?.Invoke();
    }

    private async UniTask WaitForCloseAsync(Action callback, CancellationToken token)
    {
        isOpening = true;
        
        SoundManager.Instance.PlaySE(closeSe);
        anim.SetTrigger(CLOSE_ANIM_NAME);
        await UniTask.WaitUntil(() => !isOpening, cancellationToken: token);

        this.gameObject.SetActive(false);
        callback?.Invoke();
    }

    /// <summary>
    /// オープンアニメーションが終了したときアニメーションから呼ばれる
    /// </summary>
    public void OnFinishOpen()
    {
        isOpening = true;
    }

    /// <summary>
    /// クローズアニメーションが終了したときアニメーションから呼ばれる
    /// </summary>
    public void OnFinishClose()
    {
        isOpening = false;
    }

    private void OnDestroy()
    {
        cts?.CancelAndDispose();
    }
}
