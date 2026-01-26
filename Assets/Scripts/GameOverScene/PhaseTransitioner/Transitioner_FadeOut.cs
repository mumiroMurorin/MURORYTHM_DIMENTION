using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TransitionerInGameOverScene
{
    public class Transitioner_FadeOut : IPhaseTransitionerInGameOverScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInGameOverScene> phaseTransitionable;
        [SerializeField] FadeController fadeController;
        [SerializeField] MusicDataGetter musicDataGetter;
        [SerializeField] GameOverSceneDataController dataController;

        readonly PhaseStatusInGameOverScene status = PhaseStatusInGameOverScene.FadeOut;

        bool IPhaseTransitionerInGameOverScene.ConditionChecker(PhaseStatusInGameOverScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInGameOverScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"FadeOut\"");

            // アニメーションの再生
            // コンティニュー時、背景をシンフォニータイプ仕様に
            if (dataController.DataGetter.IsContinue.Value)
            {
                fadeController?.FadeOut(musicDataGetter?.DataGetter, 
                    () => { TransitionNextPhase(PhaseStatusInGameOverScene.TransitionSelectScene); });
            }
            // ゲーム終了時、デフォルト背景でフェードアウト
            else
            {
                fadeController?.FadeOut(() => { TransitionNextPhase(PhaseStatusInGameOverScene.TransitionTitleScene); });
            }
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase(PhaseStatusInGameOverScene phase)
        {
            phaseTransitionable.Value.TransitionPhase(phase);
        }
    }

}
