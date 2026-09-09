using UnityEngine;

namespace TransitionerInSelectScene
{
    public class Transitioner_DetailSelect : IPhaseTransitionerInSelectScene
    {
        readonly PhaseStatusInSelectScene status = PhaseStatusInSelectScene.DetailSelect;

        bool IPhaseTransitionerInSelectScene.ConditionChecker(PhaseStatusInSelectScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInSelectScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"DetailSelect\"");
        }
    }
}
