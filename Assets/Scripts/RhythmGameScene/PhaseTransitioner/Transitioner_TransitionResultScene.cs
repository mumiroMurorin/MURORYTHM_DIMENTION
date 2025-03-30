using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;

namespace TransitionerInRhythmGameScene
{
    public class Transitioner_TransitionResultScene : IPhaseTransitionerInRhythmGameScene
    {
        const string NEXT_SCENE_NAME = "ResultScene";

        [SerializeField] SerializeInterface<IPhaseTransitionableInRhythmGameScene> phaseTransitionable;
        [SerializeField] float waitTime = 0.5f;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.TransitionResultScene;
        CancellationTokenSource cts = new CancellationTokenSource();

        bool IPhaseTransitionerInRhythmGameScene.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRhythmGameScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"TransitionResultScene\"");
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
