using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring
{
    public class Transitioner_Play : IPhaseTransitioner
    {
        [SerializeField] SerializeInterface<IPhaseTransitionable> phaseTransitionable;
        [SerializeField] SerializeInterface<IGroundController> groundController;
        [SerializeField] SerializeInterface<ITimeController> timer;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.Play;

        bool IPhaseTransitioner.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitioner.Transition()
        {
            Debug.Log("【Transition】Transition to \"Play\"");
            StartRhythmGame();
        }

        /// <summary>
        /// 音ゲーの開始
        /// </summary>
        private void StartRhythmGame()
        {
            // グラウンドを走らせる
            groundController?.Value.StartGroundMove();
            // 時を進める
            timer?.Value.StartTimer();
        }
    }

}
