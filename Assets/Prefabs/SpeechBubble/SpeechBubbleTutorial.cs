using UnityEngine;
using TMPro;
using System.Threading;

public class SpeechBubbleTutorial : SpeechBubbleAnimation
{
    public System.Action OnFinishAnimationListner;

    protected override void SetText(string text, SpeechBubbleConfig config)
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

                // 最後の文字表示
                if(i == text.Length)
                {
                    ctsArray[i] = DelayUtility.Run(delaySeconds, () => {
                        tmp.maxVisibleCharacters = visibleNum;
                        tmp.ForceMeshUpdate();
                        OnFinishAnimationListner?.Invoke();
                    }, true);
                }
                // それ以外
                else
                {
                    ctsArray[i] = DelayUtility.Run(delaySeconds, () => {
                        tmp.maxVisibleCharacters = visibleNum;
                        tmp.ForceMeshUpdate();
                    }, true);
                }

                delaySeconds += 1f / config.CharacterRevealSpeed;
            }
        }
    }

    /// <summary>
    /// テキスト送りをキャンセルして全文表示
    /// </summary>
    public void CancelAnimation()
    {
        ResetTextCts();
        tmp.maxVisibleCharacters = int.MaxValue;
    }
}
