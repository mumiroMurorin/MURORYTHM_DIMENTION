using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;
using NaughtyAttributes;

namespace TransitionerInResultScene
{
    public class Transitioner_TransitionSelectScene : IPhaseTransitionerInResultScene
    {
        [Scene]
        [SerializeField] string nextSceneName;

        [SerializeField] SerializeInterface<IPhaseTransitionableInResultScene> phaseTransitionable;
        [SerializeField] float waitTime = 0.5f;

        readonly PhaseStatusInResultScene status = PhaseStatusInResultScene.TransitionSelectScene;
        CancellationTokenSource cts = new CancellationTokenSource();

        bool IPhaseTransitionerInResultScene.ConditionChecker(PhaseStatusInResultScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInResultScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"TransitionSelectScene\"");
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
