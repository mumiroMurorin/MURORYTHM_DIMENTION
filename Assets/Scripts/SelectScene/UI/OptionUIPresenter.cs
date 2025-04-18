using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using VContainer;
using TransitionerInSelectScene;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace UIInSelectScene
{
    public class OptionUIPresenter : MonoBehaviour
    {
        [SerializeField] OptionTopicController optionTopicController_view;
        [SerializeField] SerializeInterface<IOperationGetter> operationGetter_model;
        [SerializeField] SerializeInterface<IPhaseStatusGetterInSelectScene> phaseStatusGetter_model;

        ISelectSceneDataGetter selectSceneDataGetter_model;

        [Inject]
        public void Construct(ISelectSceneDataGetter selectSceneDataGetter)
        {
            selectSceneDataGetter_model = selectSceneDataGetter;
        }

        void Start()
        {
            BindOptionTopic();
        }

        private void BindOptionTopic()
        {
            // オプションの選択
            phaseStatusGetter_model?.Value.PhaseStatus
                .Where(status => status == PhaseStatusInSelectScene.MusicOption)
                .Subscribe(_ => {
                    // オプショントピックのセット
                    int index = selectSceneDataGetter_model.CurrentOptionIndex.Value;
                    optionTopicController_view.SetOptionDatas(index, selectSceneDataGetter_model);
                    optionTopicController_view.OnSelectOption();
                })
                .AddTo(this.gameObject);

            // オプションから戻る
            phaseStatusGetter_model?.Value.PhaseStatus
                .Pairwise()
                .Where(pair => pair.Previous == PhaseStatusInSelectScene.MusicOption && pair.Current == PhaseStatusInSelectScene.DetailSelect)
                .Subscribe(_ => {
                    optionTopicController_view.OnBackDetailSelectPhase();
                })
                .AddTo(this.gameObject);

            // トピックの移動
            selectSceneDataGetter_model?.CurrentOptionIndex
                .Pairwise()
                .Subscribe(pair => {
                    // トピックの更新
                    _ = optionTopicController_view.OnChangeSelectedOption(pair.Current, pair.Previous, selectSceneDataGetter_model);
                })
                .AddTo(this.gameObject);
        }
    }

    public interface IOptionTopicPresenter
    {
        public void Bind(IOptionGetter optionGetter);

        public void SetEvent(IOptionGetter optionGetter);
    }
}

