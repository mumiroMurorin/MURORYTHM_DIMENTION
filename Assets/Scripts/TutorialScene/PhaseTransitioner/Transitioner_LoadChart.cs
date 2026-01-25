using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace TransitionerInTutorialScene
{
    public class Transitioner_LoadChart : IPhaseTransitionerInTutorialScene
    {
        [SerializeField] SerializeInterface<IChartGenerator> chartGenerator;
        [SerializeField] SerializeInterface<IScoreCalculaterSetter> scoreCalculaterSetter;
        [SerializeField] GroundController groundController;
        [SerializeField] TutorialController tutorialController;
        [SerializeField] SerializeInterface<IPhaseTransitionableInTutorialScene> phaseTransitionable;

        readonly PhaseStatusInTutorialScene status = PhaseStatusInTutorialScene.LoadChart;

        bool IPhaseTransitionerInTutorialScene.ConditionChecker(PhaseStatusInTutorialScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTutorialScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadChart\"");

            // チュートリアル終了で次のシーンへ
            tutorialController.OnFinishTutorialListener += () => { phaseTransitionable.Value.TransitionPhase(PhaseStatusInTutorialScene.FadeOut); };
            
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
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInTutorialScene.FadeIn);
        }
    }

}
