using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;
using NaughtyAttributes;

namespace TransitionerInTutorialScene
{
    public class Transitioner_TransitionSelectScene : IPhaseTransitionerInTutorialScene
    {
        [Scene]
        [SerializeField] string nextSceneName;
        [SerializeField] float waitTime = 0f;

        readonly PhaseStatusInTutorialScene status = PhaseStatusInTutorialScene.TransitionSelectScene;
        CancellationTokenSource cts = new CancellationTokenSource();

        bool IPhaseTransitionerInTutorialScene.ConditionChecker(PhaseStatusInTutorialScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTutorialScene.Transition()
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
