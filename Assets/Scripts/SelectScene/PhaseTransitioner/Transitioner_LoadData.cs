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
        [SerializeField] SerializeInterface<IMusicDataListLoader> musicDataListLoader;
        [SerializeField] OperationDictionary operationDictionary;
        [SerializeField] MusicDataListController musicDataListController;

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
                    isCompletedTask[0] = true;
                    CheckAndTransition(isCompletedTask);
                });
            }
            else
            {
                phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.FadeIn);
            }
        }

        private void CheckAndTransition(bool[] isCompletedTask)
        {
            if(!isCompletedTask.All(x => x)) { return; }
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.FadeIn);
        }

        private void RegisterOperation()
        {
            // 楽曲選択
            operationDictionary.RegisterOperation(OperationTag.Select_SelectMusic, () => { phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.DetailSelect); });
            operationDictionary.RegisterOperation(OperationTag.Select_MoveLeft, () => { musicDataListController?.MoveMusicTopic(-1); });
            operationDictionary.RegisterOperation(OperationTag.Select_MoveRight, () => { musicDataListController?.MoveMusicTopic(+1); });
            operationDictionary.RegisterOperation(OperationTag.Select_UpDifficulty, () => { musicDataListController?.ChangeDifficulty(+1); });
            operationDictionary.RegisterOperation(OperationTag.Select_DownDifficulty, () => { musicDataListController?.ChangeDifficulty(-1); });

            // 詳細確認
            operationDictionary.RegisterOperation(OperationTag.Select_Detail_BackSelectMusic, () => { phaseTransitionable?.Value.TransitionPhase(PhaseStatusInSelectScene.MusicSelect); });
            operationDictionary.RegisterOperation(OperationTag.Select_Detail_StartMusic, () => { });

            // オプション
        }
    }
}
