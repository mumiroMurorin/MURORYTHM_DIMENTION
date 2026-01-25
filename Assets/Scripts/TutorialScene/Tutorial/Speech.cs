using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Tutorial
{
    /// <summary>
    /// セリフを出す
    /// </summary>
    [System.Serializable]
    public class Speech : TutorialActionNode
    {
        [SerializeField] SpeechBubbleTutorial speechBubble;
        [SerializeField,TextArea(0,5)] string text;
        [SerializeField] float waitSeconds = 2.5f;
        [SerializeField] SpeechBubbleConfig config;
        [SerializeField] SerializeInterface<IDisposer> disposableObject;

        CancellationTokenSource textAnimationCts;
        CancellationTokenSource waitingPlayerCts;

        public override void Do()
        {
            speechBubble?.Speak(text, config);

            textAnimationCts = new CancellationTokenSource();
            disposableObject?.Value?.SetCts(textAnimationCts);

            // アニメーションが終った場合アクション待ちをキャンセル
            speechBubble.OnFinishAnimationListner += OnFinishTextAnimation;
        }

        private void OnFinishTextAnimation()
        {
            textAnimationCts?.CancelAndDispose();
            AfterTextAnimation();

            speechBubble.OnFinishAnimationListner -= OnFinishTextAnimation;
        }

        private void AfterTextAnimation()
        {
            waitingPlayerCts = new CancellationTokenSource();
            disposableObject?.Value?.SetCts(waitingPlayerCts);

            // 文字アニメーション後ちょい待ち
            WaitForSecondsAsync(() => {
                speechBubble.ShutUp();
                next?.Do();
            }, waitingPlayerCts.Token).Forget();
        }

        private async UniTask WaitForSecondsAsync(System.Action callback, CancellationToken token)
        {
            await UniTask.WaitForSeconds(waitSeconds, cancellationToken: token);
            callback?.Invoke();
        }
    }
}