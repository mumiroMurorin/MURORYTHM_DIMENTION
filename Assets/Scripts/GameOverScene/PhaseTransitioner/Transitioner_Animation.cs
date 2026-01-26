using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TransitionerInGameOverScene
{
    public class Transitioner_Animation : IPhaseTransitionerInGameOverScene
    {
        [SerializeField] float afterContinueDelaySeconds = 1.5f;
        [SerializeField] float afterFinishDelaySeconds = 3f;
        [SerializeField] SerializeInterface<IPhaseTransitionableInGameOverScene> phaseTransitionable;
        [SerializeField] GameOverSceneDataController dataController;
        [SerializeField] TextBoxController textBoxController;
        [SerializeField] SikibooController sikibooController;

        readonly PhaseStatusInGameOverScene status = PhaseStatusInGameOverScene.Animation;

        CancellationTokenSource cts;

        bool IPhaseTransitionerInGameOverScene.ConditionChecker(PhaseStatusInGameOverScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInGameOverScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"Animation\"");

            phaseTransitionable?.Value?.RegisterCts(cts);
            textBoxController?.Close();

            // コンティニュー
            if (dataController.DataGetter.IsContinue.Value) 
            {
                sikibooController?.OnContinueSelected();

                cts?.CancelAndDispose();
                cts = DelayUtility.Run(afterContinueDelaySeconds, () => phaseTransitionable.Value.TransitionPhase(PhaseStatusInGameOverScene.FadeOut));
            }
            // ゲーム終了
            else
            {
                sikibooController?.OnFinishSelected();

                cts?.CancelAndDispose();
                cts = DelayUtility.Run(afterFinishDelaySeconds, () => phaseTransitionable.Value.TransitionPhase(PhaseStatusInGameOverScene.FadeOut));
            }
        }
    }

}
