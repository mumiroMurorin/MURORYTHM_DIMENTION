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
        [SerializeField] OptionAsset initializingOption;
        [SerializeField] OptionDataSetter optionDataSetter;
        [SerializeField] float waitTime = 0f;

        readonly PhaseStatusInTutorialScene status = PhaseStatusInTutorialScene.TransitionSelectScene;
        CancellationTokenSource cts = new CancellationTokenSource();

        bool IPhaseTransitionerInTutorialScene.ConditionChecker(PhaseStatusInTutorialScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTutorialScene.Transition()
        {
            // シーン遷移の二重実行を防ぐ (別の遷移が始まっていれば何もしない)
            if (!SceneTransitionGate.TryBegin(nextSceneName)) { return; }

            Debug.Log("【Transition】Transition to \"TransitionSelectScene\"");

            optionDataSetter?.SetOption(initializingOption);

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
