using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using VContainer;

namespace TransitionerInTitleScene
{
    public class Transitioner_LoadData : IPhaseTransitionerInTitleScene
    {
        [SerializeField] OptionAsset initializingOption;
        [SerializeField] SerializeInterface<IPhaseTransitionableInTitleScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IMusicDataListLoader> musicDataListLoader;
        [SerializeField] OptionDataSetter optionDataSetter;
        [SerializeField] OperationDictionary operationDictionary;
        [SerializeField] VideoLoader videoLoader;

        readonly PhaseStatusInTitleScene status = PhaseStatusInTitleScene.LoadData;

        bool IPhaseTransitionerInTitleScene.ConditionChecker(PhaseStatusInTitleScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTitleScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadData\"");

            bool[] isCompletedTask = new bool[2];

            optionDataSetter?.SetOption(initializingOption);
            optionDataSetter?.ResetTutorialGuideCharacterType();

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
                isCompletedTask[0] = true;
            }

            // タイトル動画の読み込み
            videoLoader.LoadVideo(() =>
            {
                isCompletedTask[1] = true;
                CheckAndTransition(isCompletedTask);
            });

            RegisterOperation();
        }

        private void CheckAndTransition(bool[] isCompletedTask)
        {
            if(!isCompletedTask.All(x => x)) { return; }
            TransitionNextPhase();
        }

        private void RegisterOperation()
        {
            operationDictionary.RegisterOperation(OperationTag.Title_WaitingForPlayerInput, () => { TransitionGameStartPhase(); });
        }

        /// <summary>
        /// GameStartフェーズへの移動
        /// </summary>
        private void TransitionGameStartPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInTitleScene.GameStart);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInTitleScene.FadeIn);
        }
    }
}
