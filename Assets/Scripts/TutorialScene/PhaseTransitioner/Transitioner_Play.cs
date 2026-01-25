using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace TransitionerInTutorialScene
{
    public class Transitioner_Play : IPhaseTransitionerInTutorialScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInTutorialScene> phaseTransitionable;
        [SerializeField] SerializeInterface<ITimeController> timer;

        [Tooltip("フェーズ遷移までの時間")]
        [SerializeField] float waitDuration = 0.5f;

        readonly PhaseStatusInTutorialScene status = PhaseStatusInTutorialScene.Play;

        bool IPhaseTransitionerInTutorialScene.ConditionChecker(PhaseStatusInTutorialScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTutorialScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"Play\"");

            _ = DelayedExecutor.ExecuteAfterDelay(waitDuration, StartTutorial);
        }

        /// <summary>
        /// 音ゲーの開始
        /// </summary>
        private void StartTutorial()
        {
            // 時を進める
            timer?.Value.StartTimer();
        }
    }

}
