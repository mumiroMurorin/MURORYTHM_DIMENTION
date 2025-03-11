using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring.TransitionerInSelectScene
{
    public class Transitioner_DetailSelect : IPhaseTransitionerInSelectScene
    {
        private int[] OPTION_NEXT_INDEXES = new int[] { 12, 13 };
        private int[] OPTION_BACK_INDEXES = new int[] { 2, 3 };
        private int[] OPTION_PLUS_INDEXES = new int[] { 8, 9, 10 };
        private int[] OPTION_MINUS_INDEXES = new int[] { 5, 6, 7 };
        private int[] BACK_SELECT_INDEXES = new int[] { 0, 15 };
        private int[] MUSIC_START_INDEXES = new int[] { 5, 6, 7, 8, 9, 10 };

        [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IInputHandler> inputHandler;

        readonly PhaseStatusInSelectScene status = PhaseStatusInSelectScene.DetailSelect;

        bool IPhaseTransitionerInSelectScene.ConditionChecker(PhaseStatusInSelectScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInSelectScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"DetailSelect\"");

            inputHandler?.Value.Dispose();

            inputHandler?.Value.OnTouchSlider(MUSIC_START_INDEXES, TransitionRhythmGamePhase);
            inputHandler?.Value.OnTouchSlider(BACK_SELECT_INDEXES, TransitionMusicSelectPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionRhythmGamePhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.FadeOut);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionMusicSelectPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.MusicSelect);
        }
    }
}
