using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading;

public class SpeechBubbleTutorial : SpeechBubbleAnimation
{
    [SerializeField] Image faceImage;
    [SerializeField] EmotionAsset emotionAsset;

    public System.Action OnFinishAnimationListner;

    protected override void SetText(string text, SpeechBubbleConfig config)
    {
        tmp.text = text;
        var emotion = config != null ? config.Emotion : FaceEmotion.Normal;
        emotionAsset?.ApplySprite(emotion, faceImage);

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

                if (i == text.Length)
                {
                    ctsArray[i] = DelayUtility.Run(delaySeconds, () =>
                    {
                        tmp.maxVisibleCharacters = visibleNum;
                        tmp.ForceMeshUpdate();
                        OnFinishAnimationListner?.Invoke();
                    }, true);
                }
                else
                {
                    ctsArray[i] = DelayUtility.Run(delaySeconds, () =>
                    {
                        tmp.maxVisibleCharacters = visibleNum;
                        tmp.ForceMeshUpdate();
                    }, true);
                }

                delaySeconds += revealInterval;
            }
        }
        else
        {
            OnFinishAnimationListner?.Invoke();
        }
    }

    public void CancelAnimation()
    {
        ResetTextCts();
        tmp.maxVisibleCharacters = int.MaxValue;
    }
}
