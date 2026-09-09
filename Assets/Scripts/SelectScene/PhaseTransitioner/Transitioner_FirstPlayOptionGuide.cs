using System.Threading;
using UnityEngine;

namespace TransitionerInSelectScene
{
    public class Transitioner_FirstPlayOptionGuide : IPhaseTransitionerInSelectScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitionable;
        [SerializeField] float delay = 0.5f;
        [SerializeField] TextBoxController previousTopicTextBox;
        [SerializeField] TextBoxController firstPlayOptionGuideTextBox;

        readonly PhaseStatusInSelectScene status = PhaseStatusInSelectScene.FirstPlayOptionGuide;
        CancellationTokenSource cts;

        bool IPhaseTransitionerInSelectScene.ConditionChecker(PhaseStatusInSelectScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInSelectScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"FirstPlayOptionGuide\"");

            if (previousTopicTextBox != null)
            {
                previousTopicTextBox.Close(OpenFirstPlayOptionGuideWindow);
                return;
            }

            OpenFirstPlayOptionGuideWindow();
        }

        private void OpenFirstPlayOptionGuideWindow()
        {
            cts?.CancelAndDispose();
            cts = DelayUtility.Run(delay, () =>
            {
                if (firstPlayOptionGuideTextBox != null)
                {
                    firstPlayOptionGuideTextBox.Open(TransitionNextPhase);
                    return;
                }

                TransitionNextPhase();
            });
            phaseTransitionable?.Value?.RegisterCts(cts);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.FirstPlayOptionGuide_Operation);
        }
    }
}
