using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading;

public class LogItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI tmp;
    [SerializeField] Image backGround;
    [SerializeField] float displayDuration = 3f;

    [Header("背景色")]
    [Tooltip("通常時")]
    [SerializeField] Color generalColor;
    [Tooltip("警告時")]
    [SerializeField] Color warningColor;
    [Tooltip("エラー時")]
    [SerializeField] Color errorColor;

    CancellationTokenSource cts;

    private void Awake()
    {
        cts = new CancellationTokenSource();
        _ = DelayedExecutor.ExecuteAfterDelay(displayDuration, Destroy, cts.Token);
    }

    /// <summary>
    /// データセット
    /// </summary>
    /// <param name="logData"></param>
    public void SetLogData(LogData logData)
    {
        tmp.text = logData.Logtext;

        // 背景色の設定
        switch (logData.LogType)
        {
            case LogType.Log:
                backGround.color = generalColor;
                break;
            case LogType.Warning:
                backGround.color = warningColor;
                break;
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                backGround.color = errorColor;
                if (cts != null)
                {
                    cts.Cancel();
                    cts.Dispose();
                    cts = null;
                }
                break;
        }
    }

    /// <summary>
    /// 表示文の差し替え
    /// </summary>
    public void SetText(string text)
    {
        tmp.text = text;
    }

    /// <summary>
    /// 時間経過で消えないようにする (消すときは呼び出し側で GameObject を破棄する)
    /// </summary>
    public void KeepDisplaying()
    {
        if (cts == null) { return; }

        cts.Cancel();
        cts.Dispose();
        cts = null;
    }

    private void Destroy()
    {
        Destroy(this.gameObject);
    }

    private void OnDestroy()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }
}
