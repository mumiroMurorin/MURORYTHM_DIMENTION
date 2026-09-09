using UnityEngine;

namespace TransitionerInSelectScene
{
    public class Transitioner_FirstPlayOptionGuide_Operation : IPhaseTransitionerInSelectScene
    {
        readonly PhaseStatusInSelectScene status = PhaseStatusInSelectScene.FirstPlayOptionGuide_Operation;

        bool IPhaseTransitionerInSelectScene.ConditionChecker(PhaseStatusInSelectScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInSelectScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"FirstPlayOptionGuide_Operation\"");
        }
    }
}
