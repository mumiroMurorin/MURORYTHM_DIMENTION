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
        [SerializeField] BodyTrackingSettingsSaver trackingSettingsSaver;

        readonly PhaseStatusInRootScene status = PhaseStatusInRootScene.TransitionSelectScene;
        CancellationTokenSource cts = new CancellationTokenSource();

        bool IPhaseTransitionerInRootScene.ConditionChecker(PhaseStatusInRootScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRootScene.Transition()
        {
            // シーン遷移の二重実行を防ぐ (別の遷移が始まっていれば何もしない)
            if (!SceneTransitionGate.TryBegin(NEXT_SCENE_NAME)) { return; }

            Debug.Log("【Transition】Transition to \"TransitionSelectScene\"");

            // データ保存
            trackingSettingsSaver?.SaveBodyTrackingSettings();

            LoadSceneAsync(cts.Token).Forget();
        }

        private async UniTaskVoid LoadSceneAsync(CancellationToken token)
        {
            var async = SceneManager.LoadSceneAsync(NEXT_SCENE_NAME);
            if (async == null) { SceneTransitionGate.Abort(); return; }

            async.allowSceneActivation = false;
            await UniTask.WaitForSeconds(waitTime, cancellationToken: token);
            async.allowSceneActivation = true;
        }
    }

}
