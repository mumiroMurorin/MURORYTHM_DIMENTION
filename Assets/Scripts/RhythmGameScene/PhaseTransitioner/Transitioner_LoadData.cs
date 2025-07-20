using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace TransitionerInRhythmGameScene
{
    public class Transitioner_LoadData : IPhaseTransitionerInRhythmGameScene
    {
        [SerializeField] SerializeInterface<IChartLoader> chartLoader;
        [SerializeField] SerializeInterface<IMusicPlayerInRhythmGameScene> musicPlayer;
        [SerializeField] SerializeInterface<IScoreResetter> scoreResetter;
        [SerializeField] SerializeInterface<ITimeController> timeController;
        [SerializeField] SerializeInterface<IPhaseTransitionableInRhythmGameScene> phaseTransitionable;
        [SerializeField] BodyTrackingSettingsLoader trackingSettingsLoader;
        
        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.LoadData;

        bool isLoadedMusic;
        bool isLoadedChart;

        bool IPhaseTransitionerInRhythmGameScene.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRhythmGameScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadData\"");

            // トラッキングセッティング読み込み
            trackingSettingsLoader?.LoadBodyTrackingSettings();

            // スコアリセット
            scoreResetter?.Value.ResetScore();

            // 時間リセット
            timeController?.Value.ResetTimer();

            // 楽曲の読み込み
            musicPlayer?.Value.LoadMusic(() => { 
                isLoadedMusic = true;
                if (CheckTransitionable()) { TransitionNextPhase(); }
            });

            // 譜面の読み込み
            chartLoader?.Value.LoadChart(() => {
                isLoadedChart = true;
                if (CheckTransitionable()) { TransitionNextPhase(); }
            });
        }

        private bool CheckTransitionable()
        {
            return isLoadedMusic && isLoadedChart;
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInRhythmGame.LoadChart);
        }
    }

}
