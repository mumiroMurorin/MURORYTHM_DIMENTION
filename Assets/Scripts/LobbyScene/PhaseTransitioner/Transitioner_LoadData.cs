using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using VContainer;

namespace TransitionerInLobbyScene
{
    public class Transitioner_LoadData : IPhaseTransitionerInLobbyScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInLobbyScene> phaseTransitionable;
        [SerializeField] OperationDictionary operationDictionary;
        [SerializeField] LobbySceneDataController dataController;

        readonly PhaseStatusInLobbyScene status = PhaseStatusInLobbyScene.LoadData;

        bool IPhaseTransitionerInLobbyScene.ConditionChecker(PhaseStatusInLobbyScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInLobbyScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadData\"");

            RegisterOperation();

            TransitionNextPhase();
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInLobbyScene.FadeIn);
        }

        private void RegisterOperation() 
        {
            operationDictionary.RegisterOperation(OperationTag.Lobby_PlayTutorial, () => { TransitionFadeOutPhase(true); });
            operationDictionary.RegisterOperation(OperationTag.Lobby_SkipTutorial, () => { TransitionFadeOutPhase(false); });
        }

        private void TransitionFadeOutPhase(bool isPlayTutorial)
        {
            dataController?.DataSetter?.SetPlayTutorial(isPlayTutorial);
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInLobbyScene.FadeOut);
        }
    }
}
