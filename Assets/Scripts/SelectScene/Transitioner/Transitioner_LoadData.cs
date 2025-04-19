using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace TransitionerInSelectScene
{
    public class Transitioner_LoadData : IPhaseTransitionerInSelectScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IMusicDataListLoader> musicDataListLoader;

        readonly PhaseStatusInSelectScene status = PhaseStatusInSelectScene.LoadData;

        bool IPhaseTransitionerInSelectScene.ConditionChecker(PhaseStatusInSelectScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInSelectScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadData\"");

            // 楽曲データリストの読み込みとセット
            musicDataListLoader.Value.LoadMusicDataList();

            // 楽曲サンプル音源の読み込み
            musicDataListLoader.Value.LoadAudioDatas(TransitionNextPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.FadeIn);
        }
    }
}
