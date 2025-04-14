using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using System.Threading;

namespace TransitionerInRhythmGameScene
{
    public class Transitioner_Play : IPhaseTransitionerInRhythmGameScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInRhythmGameScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IMusicPlayerInRhythmGameScene> musicPlayer;
        [SerializeField] SerializeInterface<ITimeController> timer;

        [Tooltip("フェーズ遷移までの時間")]
        [SerializeField] float waitDuration = 0.5f;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.Play;

        bool IPhaseTransitionerInRhythmGameScene.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRhythmGameScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"Play\"");

            _ = DelayedExecutor.ExecuteAfterDelay(waitDuration, StartRhythmGame);
        }

        /// <summary>
        /// 音ゲーの開始
        /// </summary>
        private void StartRhythmGame()
        {
            // 楽曲を流す
            musicPlayer?.Value.PlayMusic();
            // 時を進める
            timer?.Value.StartTimer();
        }
    }

}
