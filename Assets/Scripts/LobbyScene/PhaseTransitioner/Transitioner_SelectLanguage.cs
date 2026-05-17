using UnityEngine;

namespace TransitionerInLobbyScene
{
    public class Transitioner_SelectLanguage : IPhaseTransitionerInLobbyScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInLobbyScene> phaseTransitionable;
        [SerializeField] TextBoxController textBoxController;

        readonly PhaseStatusInLobbyScene status = PhaseStatusInLobbyScene.SelectLanguage;

        bool IPhaseTransitionerInLobbyScene.ConditionChecker(PhaseStatusInLobbyScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInLobbyScene.Transition()
        {
            Debug.Log("【Transition】 Transition to SelectLanguage");

            textBoxController?.Open(TransitionNextPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInLobbyScene.SelectLanguage_Operation);
        }
    }
}
