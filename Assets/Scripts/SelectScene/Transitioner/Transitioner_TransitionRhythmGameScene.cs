using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;

namespace TransitionerInSelectScene
{
    public class Transitioner_TransitionRhythmGameScene : IPhaseTransitionerInSelectScene
    {
        const string NEXT_SCENE_NAME = "RhythmGameScene";

        [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitionable;
        [SerializeField] float waitTime = 0.5f;

        readonly PhaseStatusInSelectScene status = PhaseStatusInSelectScene.TransitionRhythmGameScene;
        CancellationTokenSource cts = new CancellationTokenSource();

        bool IPhaseTransitionerInSelectScene.ConditionChecker(PhaseStatusInSelectScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInSelectScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"TransitionRhythmGameScene\"");
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
