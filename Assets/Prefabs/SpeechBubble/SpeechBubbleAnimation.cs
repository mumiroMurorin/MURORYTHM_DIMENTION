using UnityEngine;
using TMPro;
using System.Threading;

public abstract class SpeechBubbleAnimation : SpeechBubble
{
    [SerializeField] SpeechBubbleConfig defaultConfig;

    protected CancellationTokenSource[] ctsArray;

    protected override void OnSpeak(string text, SpeechBubbleConfig config)
    {
        if (config != null)
        {
            this.tmp.fontSize = config.FontSize < 0f ? defaultConfig.FontSize : config.FontSize;
            this.tmp.color = config.FontColor;
        }

        SetText(text, config);
        this.gameObject.SetActive(true);
    }

    protected virtual void SetText(string text, SpeechBubbleConfig config)
    {
        tmp.text = text;

        var duration = GetSpeechDuration(config);
        if (Application.isPlaying && duration > 0f)
        {
            tmp.maxVisibleCharacters = 0;
            float delaySeconds = 0f;

            ResetTextCts();
            ctsArray = new CancellationTokenSource[text.Length + 1];

            var revealInterval = text.Length > 0 ? duration / text.Length : 0f;

            for (int i = 0; i < text.Length + 1; i++)
            {
                int visibleNum = i;
                ctsArray[i] = DelayUtility.Run(delaySeconds, () => { tmp.maxVisibleCharacters = visibleNum; });

                delaySeconds += revealInterval;
            }
        }
    }

    protected float GetSpeechDuration(SpeechBubbleConfig config)
    {
        if (config != null)
        {
            return config.SpeechDuration;
        }

        return defaultConfig != null ? defaultConfig.SpeechDuration : 0f;
    }

    protected override void OnShutUp()
    {
        if (this == null) { return; }
        if (this.gameObject == null) { return; }

        ResetTextCts();

        this?.gameObject?.SetActive(false);
    }

    protected void ResetTextCts()
    {
        if (!Application.isPlaying || ctsArray == null) { return; }

        foreach (var cts in ctsArray)
        {
            cts.CancelAndDispose();
        }

        ctsArray = null;
    }

    private void OnDestroy()
    {
        ResetTextCts();
    }
}
