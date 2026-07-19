using System.Threading;
using UnityEngine;

namespace TransitionerInLobbyScene
{
    public class Transitioner_SelectTutorialGuideCharacter : IPhaseTransitionerInLobbyScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInLobbyScene> phaseTransitionable;
        [SerializeField] float delay = 0.5f;
        [SerializeField] TextBoxController previousTopicTextBox;
        [SerializeField] TextBoxController selectTutorialGuideCharacterTextBox;

        readonly PhaseStatusInLobbyScene status = PhaseStatusInLobbyScene.SelectTutorialGuideCharacter;
        CancellationTokenSource cts;

        bool IPhaseTransitionerInLobbyScene.ConditionChecker(PhaseStatusInLobbyScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInLobbyScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"SelectTutorialGuideCharacter\"");

            previousTopicTextBox?.Close(OpenSelectTutorialGuideCharacterWindow);
        }

        private void OpenSelectTutorialGuideCharacterWindow()
        {
            cts?.CancelAndDispose();
            cts = DelayUtility.Run(delay, () =>
            {
                selectTutorialGuideCharacterTextBox?.Open(TransitionNextPhase);
            });
            phaseTransitionable?.Value?.RegisterCts(cts);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInLobbyScene.SelectTutorialGuideCharacter_Operation);
        }
    }
}