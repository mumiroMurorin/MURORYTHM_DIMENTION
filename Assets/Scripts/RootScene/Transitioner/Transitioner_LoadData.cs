using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace TransitionerInRootScene
{
    public class Transitioner_LoadData : IPhaseTransitionerInRootScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInRootScene> phaseTransitionable;
        [SerializeField] SerializeInterface<ITimeController> timeController;
        [SerializeField] SerializeInterface<ISpaceInputHandler> spaceInputHandler;
        
        readonly PhaseStatusInRootScene status = PhaseStatusInRootScene.LoadData;

        bool isLoadedMusic;
        bool isLoadedChart;

        bool IPhaseTransitionerInRootScene.ConditionChecker(PhaseStatusInRootScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRootScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadData\"");

            // 時間リセット
            timeController?.Value.ResetTimer();
            timeController?.Value.StartTimer();

            // トラッキングの開始
            spaceInputHandler?.Value.InitializeBodyTracking();
            spaceInputHandler?.Value.StartTracking();

            TransitionNextPhase();
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInRootScene.SettingOption);
        }
    }

}
