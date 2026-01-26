using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading;

namespace TransitionerInGameOverScene
{
    public class Transitioner_LoadData : IPhaseTransitionerInGameOverScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInGameOverScene> phaseTransitionable;
        [SerializeField] OperationDictionary operationDictionary;
        [SerializeField] GameOverSceneDataController dataController;

        readonly PhaseStatusInGameOverScene status = PhaseStatusInGameOverScene.LoadData;

        bool IPhaseTransitionerInGameOverScene.ConditionChecker(PhaseStatusInGameOverScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInGameOverScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadData\"");

            SoundManager.Instance.PlayBGM(BGM_Type.GameOver);

            RegisterOperation();
            TransitionNextPhase();
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInGameOverScene.FadeIn);
        }

        private void RegisterOperation() 
        {
            operationDictionary.RegisterOperation(OperationTag.GameOver_Continue, () => { 
                TransitionFadeOutPhase(true);
            });

            operationDictionary.RegisterOperation(OperationTag.GameOver_FinishGame, () => { 
                TransitionFadeOutPhase(false);
            });
        }

        private void TransitionFadeOutPhase(bool isContinue)
        {
            dataController?.DataSetter?.SetContinue(isContinue);
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInGameOverScene.Animation);
        }
    }
}
