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
        [FormerlySerializedAs("text")]
        [SerializeField] string textKey;
        [SerializeField] float waitSeconds = 2.5f;
        [SerializeField] SpeechBubbleConfig config;

        private const string TutorialTextTableName = "TutorialText";

        private TutorialRuntimeContext context;
        private CancellationTokenSource textAnimationCts;
        private CancellationTokenSource waitingPlayerCts;

        public override void Initialize(TutorialRuntimeContext context)
        {
            this.context = context;
        }

        public override void Do()
        {
            SpeechBubbleTutorial currentSpeechBubble = GetSpeechBubble();
            if (currentSpeechBubble == null)
            {
                next?.Do();
                return;
            }

            textAnimationCts = new CancellationTokenSource();
            RegisterCts(textAnimationCts);

            currentSpeechBubble.OnFinishAnimationListner -= OnFinishTextAnimation;
            currentSpeechBubble.OnFinishAnimationListner += OnFinishTextAnimation;

            currentSpeechBubble.Speak(ResolveText(), config);
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

            SpeechBubbleTutorial currentSpeechBubble = GetSpeechBubble();
            if (currentSpeechBubble != null)
            {
                currentSpeechBubble.OnFinishAnimationListner -= OnFinishTextAnimation;
            }
        }

        private void AfterTextAnimation()
        {
            waitingPlayerCts = new CancellationTokenSource();
            RegisterCts(waitingPlayerCts);

            WaitForSecondsAsync(() =>
            {
                GetSpeechBubble()?.ShutUp();
                next?.Do();
            }, waitingPlayerCts.Token).Forget();
        }

        private async UniTask WaitForSecondsAsync(System.Action callback, CancellationToken token)
        {
            await UniTask.WaitForSeconds(waitSeconds, cancellationToken: token);
            callback?.Invoke();
        }

        private SpeechBubbleTutorial GetSpeechBubble()
        {
            return context?.SpeechBubble;
        }

        private void RegisterCts(CancellationTokenSource cts)
        {
            context?.Disposer?.SetCts(cts);
        }
    }
}