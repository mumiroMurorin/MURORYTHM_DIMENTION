using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;
using NaughtyAttributes;

namespace TransitionerInGameOverScene
{
    public class Transitioner_TransitionSelectScene : IPhaseTransitionerInGameOverScene
    {
        [SerializeField] [Scene] string nextSceneName;
        [SerializeField] SerializeInterface<IPhaseTransitionableInGameOverScene> phaseTransitionable;
        [SerializeField] float waitTime = 0.5f;

        readonly PhaseStatusInGameOverScene status = PhaseStatusInGameOverScene.TransitionSelectScene;
        CancellationTokenSource cts;

        bool IPhaseTransitionerInGameOverScene.ConditionChecker(PhaseStatusInGameOverScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInGameOverScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"TransitionSelectScene\"");

            cts?.CancelAndDispose();
            cts = new CancellationTokenSource();
            phaseTransitionable?.Value.RegisterCts(cts);

            LoadSceneAsync(cts.Token).Forget();
        }

        private async UniTaskVoid LoadSceneAsync(CancellationToken token)
        {
            var async = SceneManager.LoadSceneAsync(nextSceneName);

            async.allowSceneActivation = false;
            await UniTask.WaitForSeconds(waitTime, cancellationToken: token);
            async.allowSceneActivation = true;
        }
    }

}
