using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using Refactoring.TransitionerInSelectScene;

namespace Refactoring
{
    public class Operation_MusicSelect : MonoBehaviour
    {
        [Header("各項目に対応するスライダーUIの表示色")]
        [SerializeField] Color musicSelectColor;
        [SerializeField] Color rightMoveColor;
        [SerializeField] Color leftMoveColor;
        [SerializeField] Color difficultyUpColor;
        [SerializeField] Color difficultyDownColor;

        [SerializeField] SerializeInterface<IOperationSetter> operationSetter;
        [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IPhaseStatusGetterInSelectScene> phaseStatusGetter;
        [SerializeField] float delaySeconds = 0.5f;

        private int[] RIGHT_MOVE_INDICES = new int[] { 14, 15 };
        private int[] LEFT_MOVE_INDICES = new int[] { 0, 1 };
        private int[] DIFF_UP_INDICES = new int[] { 11, 12, 13 };
        private int[] DIFF_DOWN_INDICES = new int[] { 2, 3, 4 };
        private int[] MUSIC_SELECT_INDICES = new int[] { 5, 6, 7, 8, 9, 10 };

        ISelectSceneDataSetter selectSceneDataSetter;
        ISelectSceneDataGetter selectSceneDataGetter;

        [Inject]
        public void Construct(ISelectSceneDataSetter selectSceneDataSetter, ISelectSceneDataGetter selectSceneDataGetter)
        {
            this.selectSceneDataSetter = selectSceneDataSetter;
            this.selectSceneDataGetter = selectSceneDataGetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            phaseStatusGetter?.Value.PhaseStatus
                .Where(value => value == PhaseStatusInSelectScene.MusicSelect)
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
            operationSetter.Value.SetOperate(new SliderTouchData(MUSIC_SELECT_INDICES, TransitionNextPhase, musicSelectColor));
            operationSetter.Value.SetOperate(new SliderTouchData(RIGHT_MOVE_INDICES, () => MoveMusicTopic(+1), rightMoveColor));
            operationSetter.Value.SetOperate(new SliderTouchData(LEFT_MOVE_INDICES, () => MoveMusicTopic(-1), leftMoveColor));
            operationSetter.Value.SetOperate(new SliderTouchData(DIFF_UP_INDICES, () => ChangeDifficulty(+1), difficultyUpColor));
            operationSetter.Value.SetOperate(new SliderTouchData(DIFF_DOWN_INDICES, () => ChangeDifficulty(-1), difficultyDownColor));
        }

        /// <summary>
        /// MusicTopicの移動
        /// </summary>
        /// <param name="index"></param>
        private void MoveMusicTopic(int delta)
        {
            selectSceneDataSetter.SetSelectIndex(selectSceneDataGetter.CurrentSelectIndex.Value + delta);
        }

        /// <summary>
        /// 難易度の変更
        /// </summary>
        /// <param name="diff"></param>
        private void ChangeDifficulty(int delta)
        {
            selectSceneDataSetter.SetDifficulty(selectSceneDataGetter.Difficulty.Value + delta);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.DetailSelect);
        }
    }

}