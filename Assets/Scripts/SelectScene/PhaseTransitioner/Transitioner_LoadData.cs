using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using VContainer;

namespace TransitionerInSelectScene
{
    public class Transitioner_LoadData : IPhaseTransitionerInSelectScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInSelectScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IPhaseStatusGetterInSelectScene> phaseGetter;
        [SerializeField] SerializeInterface<IMusicDataListLoader> musicDataListLoader;
        [SerializeField] OperationDictionary operationDictionary;
        [SerializeField] MusicDataListController musicDataListController;
        [SerializeField] OptionDataListController optionDataListController;
        [SerializeField] MusicDataSetter musicDataSetter;
        [SerializeField] OptionDataSetter optionDataSetter;
        [SerializeField] InteractNoteEffectControllerInOption interactNoteEffectControllerInOption;
        [SerializeField] TextBoxController firstPlayOptionGuideTextBox;

        readonly PhaseStatusInSelectScene status = PhaseStatusInSelectScene.LoadData;

        bool IPhaseTransitionerInSelectScene.ConditionChecker(PhaseStatusInSelectScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInSelectScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadData\"");

            RegisterOperation();

            bool[] isCompletedTask = new bool[1];

            // 楽曲データリストの読み込みとセット
            if (!musicDataListLoader.Value.CheckLoadedMusicDatas()) 
            {
                musicDataListLoader.Value.LoadMusicDataList(() => {
                    InitializeInteractNoteEffectControllerInOption();
                    isCompletedTask[0] = true;
                    CheckAndTransition(isCompletedTask);
                });
            }
            else
            {
                InitializeInteractNoteEffectControllerInOption();
                phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.FadeIn);
            }
        }

        private void InitializeInteractNoteEffectControllerInOption()
        {
            interactNoteEffectControllerInOption?.InitializeAfterLoadData();
        }

        private void CheckAndTransition(bool[] isCompletedTask)
        {
            if(!isCompletedTask.All(x => x)) { return; }
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.FadeIn);
        }

        private void RegisterOperation()
        {
            // 楽曲選択
            operationDictionary.RegisterOperation(OperationTag.Select_SelectMusic, () => { TransitionDetailSelect(); });
            operationDictionary.RegisterOperation(OperationTag.Select_FirstPlayOptionGuide_Confirm, () => { ConfirmFirstPlayOptionGuide(); });
            operationDictionary.RegisterOperation(OperationTag.Select_FirstPlayOptionGuide_DummyOption, () => { });
            operationDictionary.RegisterOperation(OperationTag.Select_MoveLeft, () => { musicDataListController?.MoveMusicTopic(-1); });
            operationDictionary.RegisterOperation(OperationTag.Select_MoveRight, () => { musicDataListController?.MoveMusicTopic(+1); });
            operationDictionary.RegisterOperation(OperationTag.Select_UpDifficulty, () => { ChangeDifficulty(+1); });
            operationDictionary.RegisterOperation(OperationTag.Select_DownDifficulty, () => { ChangeDifficulty(-1); });

            // 詳細確認
            operationDictionary.RegisterOperation(OperationTag.Select_Detail_BackSelectMusic, () => { phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.MusicSelect); });
            operationDictionary.RegisterOperation(OperationTag.Select_Detail_StartMusic, () => { StartMusic(); });
            operationDictionary.RegisterOperation(OperationTag.Select_Detail_OpenOption, () => { phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.MusicOption); });

            // オプション
            operationDictionary.RegisterOperation(OperationTag.Select_Option_BackMusicDetail, () => { TransitionDetailSelect(); });
            operationDictionary.RegisterOperation(OperationTag.Select_Option_PlusValue, () => { optionDataListController?.ChangeTopicValue(+1); });
            operationDictionary.RegisterOperation(OperationTag.Select_Option_MinusValue, () => { optionDataListController?.ChangeTopicValue(-1); });
            operationDictionary.RegisterOperation(OperationTag.Select_Option_MoveRight, () => { optionDataListController?.MoveOptionTopic(+1); });
            operationDictionary.RegisterOperation(OperationTag.Select_Option_MoveLeft, () => { optionDataListController?.MoveOptionTopic(-1); });
        }

        private void TransitionDetailSelect()
        {
            if (IsFirstPlayOptionGuideRequired())
            {
                phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.FirstPlayOptionGuide);
                return;
            }

            if (musicDataListController.IsPlayableMusicOnCurrentSelecting()) { phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.DetailSelect); }
            else { phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.DetailSelect_UnStartable); }
        }


        private void ConfirmFirstPlayOptionGuide()
        {
            optionDataSetter?.SetFirstPlayGuideRequired(false);

            // 初回ガイドのTextBoxを閉じてから、本来の楽曲詳細フェーズへ戻す
            if (firstPlayOptionGuideTextBox != null)
            {
                firstPlayOptionGuideTextBox.Close(TransitionDetailSelect);
                return;
            }

            TransitionDetailSelect();
        }

        private bool IsFirstPlayOptionGuideRequired()
        {
            return optionDataSetter != null && optionDataSetter.IsFirstPlayGuideRequired();
        }

        /// <summary>
        /// 難易度変更
        /// </summary>
        /// <param name="delta"></param>
        private void ChangeDifficulty(int delta)
        {
            musicDataListController?.ChangeDifficulty(delta);

            bool isChangePhase = false;

            if (phaseGetter.Value.PhaseStatus.Value == PhaseStatusInSelectScene.DetailSelect && !musicDataListController.IsPlayableMusicOnCurrentSelecting()) { isChangePhase = true; }
            if (phaseGetter.Value.PhaseStatus.Value == PhaseStatusInSelectScene.DetailSelect_UnStartable && musicDataListController.IsPlayableMusicOnCurrentSelecting()) { isChangePhase = true; }

            if (isChangePhase) { TransitionDetailSelect(); }
        }

        /// <summary>
        /// プレイ楽曲のセットとフェーズ移動
        /// </summary>
        private void StartMusic()
        {
            musicDataSetter.DataSetter.SetDifficulty(musicDataListController.Getter.Difficulty.Value);
            musicDataSetter.DataSetter.SetMusicData(musicDataListController.Getter.CurrentMusicData.Value);
            optionDataSetter?.SetFirstPlayGuideRequired(false);

            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.FadeOut);
        }
    }
}
