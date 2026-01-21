using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using TransitionerInLobbyScene;

namespace OperationInLobbyScene
{
    public class Operation_ConfirmTutorial : MonoBehaviour
    {
        [Header("各項目に対応するスライダーUIの表示色")]
        [SerializeField] Color playTutorialColor;
        [SerializeField] string playTutorialText = "プレイする";

        [SerializeField] Color skipTutorialColor;
        [SerializeField] string skipTutorialText = "プレイしない";

        [SerializeField] SerializeInterface<IOperationSetter> operationSetter;
        [SerializeField] SerializeInterface<IPhaseTransitionableInLobbyScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IPhaseStatusGetterInLobbyScene> phaseStatusGetter;
        [SerializeField] LobbySceneDataController dataController;
        [SerializeField] float delaySeconds = 0.5f;

        private int[] PLAY_TUTORIAL_INDICES = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };
        private int[] SKIP_TUTORIAL_INDICES = new int[] { 8, 9, 10, 11, 12, 13, 14, 15 };

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            phaseStatusGetter?.Value.PhaseStatus
                .Where(value => value == PhaseStatusInLobbyScene.ConfirmTutorial)
                .Subscribe(_ => UpdateOperation())
                .AddTo(this.gameObject);
        }

        private void UpdateOperation()
        {
            operationSetter.Value.Dispose();

            // 少し入力許可を遅らせる
            _ = DelayedExecutor.ExecuteAfterDelay(delaySeconds, () => SetOperation());
        }

        private void SetOperation()
        {
            operationSetter.Value.SetOperate(new SliderTouchData(PLAY_TUTORIAL_INDICES, () => { TransitionTutorialFade(true); }, playTutorialColor, playTutorialText));
            operationSetter.Value.SetOperate(new SliderTouchData(SKIP_TUTORIAL_INDICES, () => { TransitionTutorialFade(false); }, skipTutorialColor, skipTutorialText));
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionTutorialFade(bool isPlayTutorial)
        {
            dataController?.DataSetter.SetPlayTutorial(isPlayTutorial);
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInLobbyScene.FadeOut);
        }


    }

}