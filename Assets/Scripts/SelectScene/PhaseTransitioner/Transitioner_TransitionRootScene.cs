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
    public class Transitioner_TransitionRootScene : IPhaseTransitionerInSelectScene
    {
        [SerializeField] [Scene] string nextSceneName;

        readonly PhaseStatusInSelectScene status = PhaseStatusInSelectScene.TransitionRootScene;

        bool IPhaseTransitionerInSelectScene.ConditionChecker(PhaseStatusInSelectScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInSelectScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"TransitionRootScene\"");
            var async = SceneManager.LoadSceneAsync(nextSceneName);
        }
    }

}
