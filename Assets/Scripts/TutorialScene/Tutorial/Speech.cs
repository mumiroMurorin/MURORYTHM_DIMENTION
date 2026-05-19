using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.Localization.Settings;
using UnityEngine.Serialization;

namespace Tutorial
{
    [System.Serializable]
    public class Speech : TutorialActionNode
    {
        [SerializeField] SpeechBubbleTutorial speechBubble;
        [FormerlySerializedAs("text")]
        [SerializeField] string textKey;
        [SerializeField] float waitSeconds = 2.5f;
        [SerializeField] SpeechBubbleConfig config;
        [SerializeField] SerializeInterface<IDisposer> disposableObject;

        private const string TutorialTextTableName = "TutorialText";

        private CancellationTokenSource textAnimationCts;
        private CancellationTokenSource waitingPlayerCts;

        public override void Do()
        {
            textAnimationCts = new CancellationTokenSource();
            disposableObject?.Value?.SetCts(textAnimationCts);

            speechBubble.OnFinishAnimationListner -= OnFinishTextAnimation;
            speechBubble.OnFinishAnimationListner += OnFinishTextAnimation;

            speechBubble?.Speak(ResolveText(), config);
        }

        private string ResolveText()
        {
            if (string.IsNullOrWhiteSpace(textKey))
            {
                return string.Empty;
            }

            var localizedText = LocalizationSettings.StringDatabase.GetLocalizedString(TutorialTextTableName, textKey);
            return string.IsNullOrEmpty(localizedText) ? textKey : localizedText;
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

            WaitForSecondsAsync(() =>
            {
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
