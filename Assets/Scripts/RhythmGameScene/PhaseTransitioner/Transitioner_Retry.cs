using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;

namespace TransitionerInRhythmGameScene
{
    public class Transitioner_Retry : IPhaseTransitionerInRhythmGameScene
    {
        const string NEXT_SCENE_NAME = "RhythmGameScene";

        [SerializeField] float waitTime = 0f;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.Retry;
        CancellationTokenSource cts = new CancellationTokenSource();

        bool IPhaseTransitionerInRhythmGameScene.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRhythmGameScene.Transition()
        {
            // シーン遷移の二重実行を防ぐ (別の遷移が始まっていれば何もしない)
            if (!SceneTransitionGate.TryBegin(NEXT_SCENE_NAME)) { return; }

            // 再生中の曲を止めてから読み直す。
            // 止めないと、読み直した後も同じ曲が途中から鳴り続け (同じ曲の再生要求は無視されるため)、
            // 譜面の時間がその再生位置まで飛んでしまう。
            SoundManager.Instance.StopBGM(false);

            Debug.Log("【Transition】Transition to \"Retry\"");
            LoadSceneAsync(cts.Token).Forget();
        }

        private async UniTaskVoid LoadSceneAsync(CancellationToken token)
        {
            var async = SceneManager.LoadSceneAsync(NEXT_SCENE_NAME);
            if (async == null) { SceneTransitionGate.Abort(); return; }

            async.allowSceneActivation = false;
            await UniTask.WaitForSeconds(waitTime, cancellationToken: token);
            async.allowSceneActivation = true;
        }
    }

}
