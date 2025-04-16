using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using VContainer;
using TransitionerInSelectScene;

namespace UIInSelectScene
{
    public class SelectUIPresenter : MonoBehaviour
    {
        [SerializeField] MusicTopicControllerView musicTopicController_view;
        [SerializeField] OptionTopicControllerView optionTopicController_view;
        [SerializeField] SliderUnitsControllerView sliderUnitsController_view;
        [SerializeField] SliderTopicTextsControllerView topicTextsController_view;
        [SerializeField] BackGroundControllerView backGroundController_view;
        [SerializeField] SerializeInterface<IOperationGetter> operationGetter_model;
        [SerializeField] SerializeInterface<IPhaseStatusGetterInSelectScene> phaseStatusGetter_model;

        ISelectSceneDataGetter selectSceneDataGetter_model;

        [Inject]
        public void Construct(ISelectSceneDataGetter selectSceneDataGetter)
        {
            selectSceneDataGetter_model = selectSceneDataGetter;
        }

        private void Start()
        {
            BindMusicTopic();
            BindOptionTopic();
            BindSliderUI();
            BindOther();

            SetEvent();
        }

        private void BindMusicTopic()
        {
            // 楽曲リストの更新
            selectSceneDataGetter_model?.MusicDatasSorted.ObserveCountChanged()
                .Subscribe(_ => {
                    int index = selectSceneDataGetter_model.CurrentMusicIndex.Value;
                    // トピックの更新
                    musicTopicController_view.SetMusicDatas(index, selectSceneDataGetter_model);
                })
                .AddTo(this.gameObject);

            // トピックの移動
            selectSceneDataGetter_model?.CurrentMusicIndex
                .Pairwise()
                .Subscribe(pair => { 
                    // トピックの更新
                    _ = musicTopicController_view.OnChangeSelectedMusic(pair.Current, pair.Previous, selectSceneDataGetter_model);
                })
                .AddTo(this.gameObject);

            // 楽曲の選択(決定)
            phaseStatusGetter_model?.Value.PhaseStatus
                .Pairwise()
                .Where(pair => pair.Current == PhaseStatusInSelectScene.DetailSelect && pair.Previous == PhaseStatusInSelectScene.MusicSelect)
                .Subscribe(_ => musicTopicController_view.OnSelectMusic())
                .AddTo(this.gameObject);

            // 楽曲選択に戻る
            phaseStatusGetter_model?.Value.PhaseStatus
                .Where(status => status == PhaseStatusInSelectScene.MusicSelect)
                .Subscribe(_ => musicTopicController_view.OnBackSelectPhase())
                .AddTo(this.gameObject);

            // 難易度の変更
            selectSceneDataGetter_model?.Difficulty
                .Subscribe(musicTopicController_view.OnChangeDifficulty)
                .AddTo(this.gameObject);

            // オプションの選択
            phaseStatusGetter_model?.Value.PhaseStatus
                .Where(status => status == PhaseStatusInSelectScene.MusicOption)
                .Subscribe(_ => { 
                    musicTopicController_view.OnSelectOption(); 
                })
                .AddTo(this.gameObject);

            // オプションから戻る
            phaseStatusGetter_model?.Value.PhaseStatus
                .Pairwise()
                .Where(pair => pair.Previous == PhaseStatusInSelectScene.MusicOption && pair.Current == PhaseStatusInSelectScene.DetailSelect)
                .Subscribe(_ => { 
                    musicTopicController_view.OnBackDetailSelectPhase(); 
                })
                .AddTo(this.gameObject);
        }

        private void BindOptionTopic()
        {
            // オプションの選択
            phaseStatusGetter_model?.Value.PhaseStatus
                .Where(status => status == PhaseStatusInSelectScene.MusicOption)
                .Subscribe(_ => {
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
        }

        private void BindSliderUI()
        {
            // 操作の追加
            operationGetter_model?.Value.SliderTouchDatas
                .ObserveAdd()
                .Subscribe(value =>
                {
                    SetInteractionSliderEvent(value.Value);
                    sliderUnitsController_view?.OnChangeSliderData(value.Value);
                    topicTextsController_view?.OnChangeSliderData(value.Value);
                })
                .AddTo(this.gameObject);

            // 操作の一新
            operationGetter_model?.Value.SliderTouchDatas
                .ObserveReset()
                .Subscribe(_ =>
                {
                    sliderUnitsController_view?.OnClearSliderData();
                    topicTextsController_view?.OnClearSliderData();
                })
                .AddTo(this.gameObject);
        }

        private void BindOther()
        {
            // 選択楽曲の変更
            selectSceneDataGetter_model?.CurrentMusicData
                .Subscribe(value => {
                    // 背景の更新
                    backGroundController_view.OnChangeMusicTopic(selectSceneDataGetter_model.CurrentMusicData.Value);
                })
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {

        }

        private void SetInteractionSliderEvent(SliderTouchData sliderTouchData)
        {
            sliderTouchData.Callback += () => sliderUnitsController_view.OnTouchSlider(sliderTouchData);
        }
    }
}