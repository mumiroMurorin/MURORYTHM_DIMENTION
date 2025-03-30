using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace TransitionerInRhythmGameScene
{
    public class Transitioner_Play : IPhaseTransitionerInRhythmGameScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInRhythmGameScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IGroundController> groundController;
        [SerializeField] SerializeInterface<ITimeController> timer;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.Play;

        bool IPhaseTransitionerInRhythmGameScene.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRhythmGameScene.Transition()
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
            // 楽曲を流す

            // 時を進める
            timer?.Value.StartTimer();
        }
    }

}
