using UnityEngine;
using TMPro;
using System.Threading;

public class SpeechBubbleAnimation : SpeechBubble
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

        // プレイ中、順番に表示
        // (TimeLine状では一気に表示)
        if (Application.isPlaying && config.CharacterRevealSpeed > 0f)
        {
            // 初期化
            tmp.maxVisibleCharacters = 0;
            float delaySeconds = 0f;

            ResetTextCts();
            ctsArray = new CancellationTokenSource[text.Length + 1];

            for (int i = 0; i < text.Length + 1; i++)
            {
                int visibleNum = i;
                ctsArray[i] = DelayUtility.Run(delaySeconds, () => { tmp.maxVisibleCharacters = visibleNum; });

                delaySeconds += 1f / config.CharacterRevealSpeed;
            }
        }
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
