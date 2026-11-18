using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;

namespace TransitionerInResultScene
{
    public class Transitioner_Retry : IPhaseTransitionerInResultScene
    {
        const string NEXT_SCENE_NAME = "RhythmGameScene";

        [SerializeField] float waitTime = 0.5f;

        readonly PhaseStatusInResultScene status = PhaseStatusInResultScene.Retry;
        CancellationTokenSource cts = new CancellationTokenSource();

        bool IPhaseTransitionerInResultScene.ConditionChecker(PhaseStatusInResultScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInResultScene.Transition()
        {
            // シーン遷移の二重実行を防ぐ (別の遷移が始まっていれば何もしない)
            if (!SceneTransitionGate.TryBegin(NEXT_SCENE_NAME)) { return; }

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
