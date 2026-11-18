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
            // シーン遷移の二重実行を防ぐ (別の遷移が始まっていれば何もしない)
            if (!SceneTransitionGate.TryBegin(nextSceneName)) { return; }

            Debug.Log("【Transition】Transition to \"TransitionRootScene\"");
            var async = SceneManager.LoadSceneAsync(nextSceneName);
            if (async == null) { SceneTransitionGate.Abort(); return; }
        }
    }

}
