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
        [SerializeField] MusicTopicsControllerView musicTopicsController_view;
        [SerializeField] SliderUnitsControllerView sliderUnitsController_view;
        [SerializeField] SliderTopicTextsControllerView topicTextsController_view;
        [SerializeField] BackGroundControllerView backGroundController_view;
        [SerializeField] SerializeInterface<IOperationGetter> operationGetter_model;
        [SerializeField] SerializeInterface<IPhaseStatusGetterInSelectScene> phaseStatusGetter_model;

        IMusicDataListGetter musicDataListGetter_model;

        [Inject]
        public void Construct(IMusicDataListGetter musicListGetter)
        {
            musicDataListGetter_model = musicListGetter;
        }

        private void Start()
        {
            BindMusicTopic();
            BindSliderUI();
            BindOther();

            SetEvent();
        }

        private void BindMusicTopic()
        {
            // 楽曲リストが既にあった時、楽曲リストを更新する
            if(musicDataListGetter_model?.MusicDatasSorted.Count > 0)
            {
                int index = musicDataListGetter_model.CurrentMusicIndex.Value;
                musicTopicsController_view.SetMusicDatas(index, musicDataListGetter_model);
            }

            // 楽曲リストの更新
            musicDataListGetter_model?.MusicDatasSorted.ObserveCountChanged()
                .Subscribe(_ => {
                    // トピックの更新
                    int index = musicDataListGetter_model.CurrentMusicIndex.Value;
                    musicTopicsController_view.SetMusicDatas(index, musicDataListGetter_model);
                })
                .AddTo(this.gameObject);

            // トピックの移動
            musicDataListGetter_model?.CurrentMusicIndex
                .Pairwise()
                .Subscribe(pair => { 
                    // トピックの更新
                    _ = musicTopicsController_view.OnChangeSelectedMusic(pair.Current, pair.Previous, musicDataListGetter_model);
                })
                .AddTo(this.gameObject);

            // 楽曲の選択(決定)
            phaseStatusGetter_model?.Value.PhaseStatus
                .Pairwise()
                .Where(pair => (pair.Current == PhaseStatusInSelectScene.DetailSelect || pair.Current == PhaseStatusInSelectScene.DetailSelect_UnStartable) && pair.Previous == PhaseStatusInSelectScene.MusicSelect)
                .Subscribe(_ => musicTopicsController_view.OnSelectMusic())
                .AddTo(this.gameObject);

            // 楽曲選択に戻る
            phaseStatusGetter_model?.Value.PhaseStatus
                .Where(status => status == PhaseStatusInSelectScene.MusicSelect)
                .Subscribe(_ => musicTopicsController_view.OnBackSelectPhase())
                .AddTo(this.gameObject);

            // 難易度の変更
            musicDataListGetter_model?.Difficulty
                .Subscribe(musicTopicsController_view.OnChangeDifficulty)
                .AddTo(this.gameObject);

            // オプションの選択
            phaseStatusGetter_model?.Value.PhaseStatus
                .Where(status => status == PhaseStatusInSelectScene.MusicOption)
                .Subscribe(_ => { 
                    musicTopicsController_view.OnSelectOption(); 
                })
                .AddTo(this.gameObject);

            // オプションから戻る
            phaseStatusGetter_model?.Value.PhaseStatus
                .Pairwise()
                .Where(pair => pair.Previous == PhaseStatusInSelectScene.MusicOption && (pair.Current == PhaseStatusInSelectScene.DetailSelect || pair.Current == PhaseStatusInSelectScene.DetailSelect_UnStartable))
                .Subscribe(_ => { musicTopicsController_view.OnBackDetailSelectPhase(); })
                .AddTo(this.gameObject);
        }


        private void BindSliderUI()
        {
            // タッチされた時のUI挙動
            operationGetter_model?.Value.OnTouchSliderListner
                .Subscribe(sliderUnitsController_view.OnTouchSlider)
                .AddTo(this.gameObject);

            // 操作の追加
            operationGetter_model?.Value.SliderTouchDatas
                .ObserveAdd()
                .Subscribe(value =>
                {
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
            musicDataListGetter_model?.CurrentMusicData
                .Subscribe(value => {
                    // 背景の更新
                    backGroundController_view.OnChangeMusicTopic(musicDataListGetter_model.CurrentMusicData.Value);
                })
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {

        }
    }
}