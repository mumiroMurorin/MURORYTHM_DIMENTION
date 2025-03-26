using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace Refactoring
{
    public class Operation_DetailSelect : MonoBehaviour
    {
        [Header("各項目に対応するスライダーUIの表示色")]
        [SerializeField] Color musicStartColor;
        [SerializeField] Color backSelectColor;
        [SerializeField] Color difficultyUpColor;
        [SerializeField] Color difficultyDownColor;

        [SerializeField] SerializeInterface<IOperationSetter> operationSetter;
        [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IPhaseStatusGetterInSelectScene> phaseStatusGetter;
        [SerializeField] float delaySeconds = 0.5f;

        private int[] OPTION_NEXT_INDICES = new int[] { 12, 13 };
        private int[] OPTION_BACK_INDICES = new int[] { 2, 3 };
        private int[] BACK_SELECT_INDICES = new int[] { 0, 15 };
        private int[] MUSIC_START_INDICES = new int[] { 5, 6, 7, 8, 9, 10 };
        private int[] DIFF_UP_INDICES = new int[] { 11, 12, 13 };
        private int[] DIFF_DOWN_INDICES = new int[] { 2, 3, 4 };

        ISelectSceneDataGetter selectSceneDataGetter;
        ISelectSceneDataSetter selectSceneDataSetter;
        IMusicDataSetter musicDataSetter;

        [Inject]
        public void Construct(IMusicDataSetter musicDataSetter, ISelectSceneDataGetter selectSceneDataGetter, ISelectSceneDataSetter selectSceneDataSetter)
        {
            this.musicDataSetter = musicDataSetter;
            this.selectSceneDataGetter = selectSceneDataGetter;
            this.selectSceneDataSetter = selectSceneDataSetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            phaseStatusGetter?.Value.PhaseStatus
                .Where(value => value == PhaseStatusInSelectScene.DetailSelect)
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
            operationSetter.Value.SetOperate(new SliderTouchData(MUSIC_START_INDICES, TransitionRhythmGamePhase, musicStartColor));
            operationSetter.Value.SetOperate(new SliderTouchData(BACK_SELECT_INDICES, TransitionMusicSelectPhase, backSelectColor));
            operationSetter.Value.SetOperate(new SliderTouchData(DIFF_UP_INDICES, () => ChangeDifficulty(+1), difficultyUpColor));
            operationSetter.Value.SetOperate(new SliderTouchData(DIFF_DOWN_INDICES, () => ChangeDifficulty(-1), difficultyDownColor));
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
        /// 楽曲選択に戻る
        /// </summary>
        private void TransitionMusicSelectPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.MusicSelect);
        }

        /// <summary>
        /// 楽曲決定
        /// </summary>
        private void TransitionRhythmGamePhase()
        {
            // 難易度の確定
            musicDataSetter.SetDifficulty(selectSceneDataGetter.Difficulty.Value);

            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.FadeOut);
        }
    }

}