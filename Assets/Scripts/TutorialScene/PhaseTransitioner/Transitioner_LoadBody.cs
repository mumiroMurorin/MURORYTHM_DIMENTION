using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace TransitionerInTutorialScene
{
    public class Transitioner_LoadBody : IPhaseTransitionerInTutorialScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInTutorialScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IBodyLoader> bodyLoader;
        [SerializeField] GameObject loadingBodyUIObj;
        [SerializeField] SerializeInterface<ITimelinePlayer> openLodingBodyUI; 
        [SerializeField] SerializeInterface<ITimelinePlayer> closeLodingBodyUI; 

        readonly PhaseStatusInTutorialScene status = PhaseStatusInTutorialScene.LoadBody;

        bool IPhaseTransitionerInTutorialScene.ConditionChecker(PhaseStatusInTutorialScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTutorialScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadBody\"");

            loadingBodyUIObj.SetActive(true);
            openLodingBodyUI?.Value.PlayAnimation(() => {
                bodyLoader.Value.WaitForLoadBody(CloseUI);
            });
        }

        /// <summary>
        /// 体の認識UIを閉じる
        /// </summary>
        private void CloseUI()
        {
            closeLodingBodyUI?.Value.PlayAnimation(TransitionNextPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInTutorialScene.Play);
        }
    }

}
