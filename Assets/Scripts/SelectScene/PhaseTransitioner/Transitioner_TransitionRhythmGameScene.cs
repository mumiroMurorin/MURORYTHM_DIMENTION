using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;
using NaughtyAttributes;

namespace TransitionerInSelectScene
{
    public class Transitioner_TransitionRhythmGameScene : IPhaseTransitionerInSelectScene
    {
        [SerializeField] [Scene] string nextSceneName;
        [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitionable;
        [SerializeField] float waitTime = 0.5f;

        readonly PhaseStatusInSelectScene status = PhaseStatusInSelectScene.TransitionRhythmGameScene;
        CancellationTokenSource cts;

        bool IPhaseTransitionerInSelectScene.ConditionChecker(PhaseStatusInSelectScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInSelectScene.Transition()
        {
            // シーン遷移の二重実行を防ぐ (別の遷移が始まっていれば何もしない)
            if (!SceneTransitionGate.TryBegin(nextSceneName)) { return; }

            Debug.Log("【Transition】Transition to \"TransitionRhythmGameScene\"");

            cts?.CancelAndDispose();
            cts = new CancellationTokenSource();

            phaseTransitionable?.Value.RegisterCts(cts);
            LoadSceneAsync(cts.Token).Forget();
        }

        private async UniTaskVoid LoadSceneAsync(CancellationToken token)
        {
            var async = SceneManager.LoadSceneAsync(nextSceneName);
            if (async == null) { SceneTransitionGate.Abort(); return; }

            async.allowSceneActivation = false;
            await UniTask.WaitForSeconds(waitTime, cancellationToken: token);
            async.allowSceneActivation = true;
        }
    }

}
