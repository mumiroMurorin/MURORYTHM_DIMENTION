using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace TransitionerInTutorialScene
{
    public class Transitioner_LoadData : IPhaseTransitionerInTutorialScene
    {
        [SerializeField] TextAsset chartFile;
        [SerializeField] SerializeInterface<IChartLoader> chartLoader;
        [SerializeField] SerializeInterface<IMusicPlayerInRhythmGameScene> musicPlayer;
        [SerializeField] SerializeInterface<IScoreResetter> scoreResetter;
        [SerializeField] SerializeInterface<ITimeController> timeController;
        [SerializeField] SerializeInterface<IPhaseTransitionableInTutorialScene> phaseTransitionable;
        [SerializeField] BodyTrackingSettingsLoader trackingSettingsLoader;
        
        readonly PhaseStatusInTutorialScene status = PhaseStatusInTutorialScene.LoadData;

        bool isLoadedMusic;
        bool isLoadedChart;

        bool IPhaseTransitionerInTutorialScene.ConditionChecker(PhaseStatusInTutorialScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTutorialScene.Transition()
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
            chartLoader?.Value.LoadChart(chartFile, () =>
            {
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
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInTutorialScene.LoadChart);
        }
    }

}
