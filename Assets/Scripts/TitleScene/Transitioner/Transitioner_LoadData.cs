using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using VContainer;

namespace TransitionerInTitleScene
{
    public class Transitioner_LoadData : IPhaseTransitionerInTitleScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInTitleScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IMusicDataListLoader> musicDataListLoader;

        readonly PhaseStatusInTitleScene status = PhaseStatusInTitleScene.LoadData;

        bool IPhaseTransitionerInTitleScene.ConditionChecker(PhaseStatusInTitleScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTitleScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadData\"");

            bool[] isCompletedTask = new bool[1];

            // 楽曲データリストの読み込みとセット
            if (!musicDataListLoader.Value.CheckLoadedMusicDatas())
            {
                musicDataListLoader.Value.LoadMusicDataList(() => {
                    isCompletedTask[0] = true;
                    CheckAndTransition(isCompletedTask);
                });
            }

        }

        private void CheckAndTransition(bool[] isCompletedTask)
        {
            if(!isCompletedTask.All(x => x)) { return; }
            TransitionNextPhase();
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInTitleScene.WaitingForPlayer);
        }
    }
}
