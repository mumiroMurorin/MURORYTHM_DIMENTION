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
        [SerializeField] SerializeInterface<IPhaseTransitionableInRhythmGameScene> phaseTransitionable;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.LoadChart;

        bool IPhaseTransitionerInRhythmGameScene.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRhythmGameScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadChart\"");

            chartEnder.Value.BindOnEndChart();
            chartGenerator.Value.Generate(() => 
            { 
                scoreCalculaterSetter.Value.SetScoreCalculater();
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
