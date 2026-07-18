using UnityEngine;
using System.Threading;

namespace TransitionerInTutorialScene
{
    public class Transitioner_SelectTutorialGuideCharacter : IPhaseTransitionerInTutorialScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInTutorialScene> phaseTransitionable;
        [SerializeField] TutorialGuideCharacterSelector selector;
        [SerializeField] float delay = 0.5f;
        [SerializeField] TextBoxController selectTutorialGuideCharacterTextBox;
        [SerializeField] bool showSelectorPanel = false;
        [SerializeField] bool createDefaultSelectorWhenNull = true;

        readonly PhaseStatusInTutorialScene status = PhaseStatusInTutorialScene.SelectTutorialGuideCharacter;
        CancellationTokenSource cts;
        bool isSelecting;
        bool hasTransitioned;

        bool IPhaseTransitionerInTutorialScene.ConditionChecker(PhaseStatusInTutorialScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTutorialScene.Transition()
        {
            Debug.Log("[Transition] Transition to \"SelectTutorialGuideCharacter\"");
            isSelecting = false;
            hasTransitioned = false;

            if (selector == null && createDefaultSelectorWhenNull)
            {
                selector = TutorialGuideCharacterSelector.CreateDefault();
            }

            if (selector == null)
            {
                TransitionNextPhase();
                return;
            }

            selector.BeginSelect(OnCharacterConfirmed, showSelectorPanel);

            OpenSelectGuideCharacterWindow();
        }

        public void SelectCreationGuide()
        {
            SelectGuideCharacter(TutorialGuideCharacterType.Creation);
        }

        public void SelectShikibooGuide()
        {
            SelectGuideCharacter(TutorialGuideCharacterType.Shikiboo);
        }

        public void SelectDestructionGuide()
        {
            SelectGuideCharacter(TutorialGuideCharacterType.Destruction);
        }

        public void SelectGuideCharacter(TutorialGuideCharacterType characterType)
        {
            if (!isSelecting || hasTransitioned) { return; }

            if (selector == null)
            {
                OnCharacterConfirmed();
                return;
            }

            selector.SelectAndConfirm(characterType);
        }

        void OpenSelectGuideCharacterWindow()
        {
            cts?.CancelAndDispose();
            cts = DelayUtility.Run(delay, () =>
            {
                if (selectTutorialGuideCharacterTextBox == null)
                {
                    isSelecting = true;
                    return;
                }

                selectTutorialGuideCharacterTextBox.Open(() => { isSelecting = true; });
            });

            phaseTransitionable?.Value?.RegisterCts(cts);
        }

        void OnCharacterConfirmed()
        {
            if (hasTransitioned) { return; }

            hasTransitioned = true;
            isSelecting = false;

            if (selectTutorialGuideCharacterTextBox != null)
            {
                selectTutorialGuideCharacterTextBox.Close(TransitionNextPhase);
                return;
            }

            TransitionNextPhase();
        }

        void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInTutorialScene.LoadBody);
        }
    }
}
