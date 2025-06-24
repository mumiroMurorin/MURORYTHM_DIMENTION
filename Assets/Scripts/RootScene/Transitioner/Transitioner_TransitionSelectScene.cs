using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;

namespace TransitionerInRootScene
{
    public class Transitioner_TransitionSelectScene : IPhaseTransitionerInRootScene
    {
        const string NEXT_SCENE_NAME = "MusicSelectScene";

        [SerializeField] float waitTime = 0f;

        readonly PhaseStatusInRootScene status = PhaseStatusInRootScene.TransitionSelectScene;
        CancellationTokenSource cts = new CancellationTokenSource();

        bool IPhaseTransitionerInRootScene.ConditionChecker(PhaseStatusInRootScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRootScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"TransitionSelectScene\"");
            LoadSceneAsync(cts.Token).Forget();
        }

        private async UniTaskVoid LoadSceneAsync(CancellationToken token)
        {
            var async = SceneManager.LoadSceneAsync(NEXT_SCENE_NAME);

            async.allowSceneActivation = false;
            await UniTask.WaitForSeconds(waitTime, cancellationToken: token);
            async.allowSceneActivation = true;
        }
    }

}
