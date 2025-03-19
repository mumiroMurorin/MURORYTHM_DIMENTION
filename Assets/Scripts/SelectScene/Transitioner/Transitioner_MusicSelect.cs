using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring.TransitionerInSelectScene
{
    public class Transitioner_MusicSelect : IPhaseTransitionerInSelectScene
    {
        private int[] RIGHT_MOVE_INDEXES = new int[] { 11, 12, 13 };
        private int[] LEFT_MOVE_INDEXES = new int[] { 2, 3, 4 };
        private int[] DIFF_UP_INDEXES = new int[] { 14, 15 };
        private int[] DIFF_DOWN_INDEXES = new int[] { 0,1 };
        private int[] MUSIC_SELECT_INDEXES = new int[] { 5, 6, 7, 8, 9, 10 };

        [SerializeField] float inputProhibitDuration = 0.5f;
        [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IInputHandler> inputHandler;

        readonly PhaseStatusInSelectScene status = PhaseStatusInSelectScene.MusicSelect;

        bool IPhaseTransitionerInSelectScene.ConditionChecker(PhaseStatusInSelectScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInSelectScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"MusicSelect\"");

            inputHandler?.Value.Dispose();

            // ちょっと遅れて実行
            // 待たないと次のフェーズまで行っちゃう
            _ = DelayedExecutor.ExecuteAfterDelay(inputProhibitDuration, SetEvent);
        }

        private void SetEvent()
        {
            inputHandler?.Value.OnTouchSlider(MUSIC_SELECT_INDEXES, TransitionNextPhase);
            inputHandler?.Value.OnTouchSlider(RIGHT_MOVE_INDEXES, () => MoveMusicTopic(+1));
            inputHandler?.Value.OnTouchSlider(LEFT_MOVE_INDEXES, () => MoveMusicTopic(-1));
            inputHandler?.Value.OnTouchSlider(DIFF_UP_INDEXES, () => ChangeDifficulty(+1));
            inputHandler?.Value.OnTouchSlider(DIFF_DOWN_INDEXES, () => ChangeDifficulty(-1));
        }

        /// <summary>
        /// MusicTopicの移動
        /// </summary>
        /// <param name="index"></param>
        private void MoveMusicTopic(int index)
        {

        }

        /// <summary>
        /// 難易度の変更
        /// </summary>
        /// <param name="diff"></param>
        private void ChangeDifficulty(int diff)
        {

        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.DetailSelect);
        }
    }
}
