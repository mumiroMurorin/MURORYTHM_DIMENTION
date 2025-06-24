using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;

namespace TransitionerInSelectScene
{
    public class Transitioner_TransitionRootScene : IPhaseTransitionerInSelectScene
    {
        const string NEXT_SCENE_NAME = "RootScene";

        readonly PhaseStatusInSelectScene status = PhaseStatusInSelectScene.TransitionRootScene;
        CancellationTokenSource cts = new CancellationTokenSource();

        bool IPhaseTransitionerInSelectScene.ConditionChecker(PhaseStatusInSelectScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInSelectScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"TransitionRootScene\"");
            var async = SceneManager.LoadSceneAsync(NEXT_SCENE_NAME);
        }
    }

}
