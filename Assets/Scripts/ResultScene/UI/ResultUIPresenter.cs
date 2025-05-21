using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace UIInResultScene
{
    public class ResultUIPresenter : MonoBehaviour
    {
        [SerializeField] MusicDataUIControllerView musicDataUIController_view;
        [SerializeField] ScoreDataUIControllerView scoreDataUIController_view;
        [SerializeField] SliderUnitsControllerView sliderUnitsController_view;
        [SerializeField] SliderTopicTextsControllerView topicTextsController_view;

        [SerializeField] SerializeInterface<IOperationGetter> operationGetter_model;

        IScoreGetter scoreGetter_model;
        IMusicDataGetter musicDataGetter_model;

        [Inject] 
        public void Construct(IScoreGetter scoreGetter, IMusicDataGetter musicDataGetter)
        {
            scoreGetter_model = scoreGetter;
            musicDataGetter_model = musicDataGetter;
        }

        private void Start()
        {
            Bind();
            SetEvent();
        }

        private void Bind()
        {
            // 楽曲データ
            if (musicDataGetter_model.Music != null) { musicDataUIController_view.SetMusicData(musicDataGetter_model.Music.Value); }
            musicDataUIController_view.SetDifficulty(musicDataGetter_model.Difficulty.Value);

            // スコアデータ
            scoreDataUIController_view.SetScoreData(scoreGetter_model);

            // スライダーUI
            // 操作の追加
            operationGetter_model?.Value.SliderTouchDatas
                .ObserveAdd()
                .Subscribe(value => {
                    SetInteractionSliderEvent(value.Value);
                    sliderUnitsController_view.OnChangeSliderData(value.Value);
                    topicTextsController_view?.OnChangeSliderData(value.Value);
                })
                .AddTo(this.gameObject);

            // 操作の一新
            operationGetter_model?.Value.SliderTouchDatas
                .ObserveReset()
                .Subscribe(_ => { 
                    sliderUnitsController_view.OnClearSliderData();
                    topicTextsController_view?.OnClearSliderData();
                })
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {

        }

        private void SetInteractionSliderEvent(SliderTouchData sliderTouchData)
        {
            sliderTouchData.AddCallback(() => sliderUnitsController_view.OnTouchSlider(sliderTouchData));
        }
    }
}
