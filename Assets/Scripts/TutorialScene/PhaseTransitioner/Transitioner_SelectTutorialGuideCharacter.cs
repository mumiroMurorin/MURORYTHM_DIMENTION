using UnityEngine;

namespace TransitionerInTutorialScene
{
    public class Transitioner_SelectTutorialGuideCharacter : IPhaseTransitionerInTutorialScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInTutorialScene> phaseTransitionable;
        [SerializeField] TutorialGuideCharacterSelector selector;
        [SerializeField] bool createDefaultSelectorWhenNull = true;

        readonly PhaseStatusInTutorialScene status = PhaseStatusInTutorialScene.SelectTutorialGuideCharacter;

        bool IPhaseTransitionerInTutorialScene.ConditionChecker(PhaseStatusInTutorialScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTutorialScene.Transition()
        {
            Debug.Log("[Transition] Transition to \"SelectTutorialGuideCharacter\"");

            if (selector == null && createDefaultSelectorWhenNull)
            {
                selector = TutorialGuideCharacterSelector.CreateDefault();
            }

            if (selector == null)
            {
                TransitionNextPhase();
                return;
            }

            selector.BeginSelect(TransitionNextPhase);
        }

        void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInTutorialScene.LoadBody);
        }
    }
}
