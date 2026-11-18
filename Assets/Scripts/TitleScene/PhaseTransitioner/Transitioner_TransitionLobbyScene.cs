using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;
using NaughtyAttributes;

namespace TransitionerInTitleScene
{
    public class Transitioner_TransitionLobbyScene : IPhaseTransitionerInTitleScene
    {
        [SerializeField] [Scene] string nextSceneName;
        [SerializeField] SerializeInterface<IPhaseTransitionableInTitleScene> phaseTransitionable;
        [SerializeField] float waitTime = 0.5f;

        readonly PhaseStatusInTitleScene status = PhaseStatusInTitleScene.TransitionLobbyScene;
        CancellationTokenSource cts;

        bool IPhaseTransitionerInTitleScene.ConditionChecker(PhaseStatusInTitleScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTitleScene.Transition()
        {
            // シーン遷移の二重実行を防ぐ (別の遷移が始まっていれば何もしない)
            if (!SceneTransitionGate.TryBegin(nextSceneName)) { return; }

            Debug.Log("【Transition】Transition to \"TransitionLobbyScene\"");

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
