using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace TransitionerInRhythmGameScene
{
    public class Transitioner_LoadChart : IPhaseTransitionerInRhythmGameScene
    {
        [SerializeField] SerializeInterface<IChartGenerator> chartGenerator;
        [SerializeField] SerializeInterface<IChartEnder> chartEnder;
        [SerializeField] SerializeInterface<IScoreCalculaterSetter> scoreCalculaterSetter;
        [SerializeField] GroundController groundController;
        [SerializeField] SerializeInterface<IPhaseTransitionableInRhythmGameScene> phaseTransitionable;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.LoadChart;

        bool IPhaseTransitionerInRhythmGameScene.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRhythmGameScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadChart\"");

            chartEnder.Value.BindOnEndChart(() => phaseTransitionable.Value.TransitionPhase(PhaseStatusInRhythmGame.EndAnimation));
            chartGenerator.Value.Generate(() => 
            { 
                scoreCalculaterSetter.Value.SetScoreCalculater();
                groundController.Initialize();
                TransitionNextPhase(); 
            });
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInRhythmGame.FadeIn);
        }
    }

}
